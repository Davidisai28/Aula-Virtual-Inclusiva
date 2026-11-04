using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Instrumentación solo Editor. No se añaden componentes ni se escriben transforms.
public sealed class DiagnosticoSentadoXR : EditorWindow
{
    PruebaAsientoXR asiento;
    Transform pantalla;
    bool armado, gizmos, ultimoSentado;
    float toleranciaMetros=.01f, toleranciaGrados=2;
    double inicio, siguientePrevia;
    int paso, antesRender;
    bool midiendo, iniciadaSentado;
    readonly List<Muestra> muestras=new List<Muestra>();
    Muestra previa;
    string informe="";
    string contexto="";
    string tipoPrueba;
    readonly double[] tiempos={.1,.5,1};

    sealed class Muestra
    {
        public string fase;
        public double instante;
        public Vector3 camara, camaraLocal, vista, rig, offset, offsetLocal, panel;
        public Quaternion rotCamara, rotRig=Quaternion.identity;
        public Vector3 forwardCamara, forwardVista, destinoPantalla;
        public bool tienePantalla, tienePanel, tieneRig, tieneOffset;
        public bool sentado;
        public bool tienePoseEntrada;
        public Vector3 poseEntrada;
        public string fuentePose;
    }

    [MenuItem("Aula Virtual/Diagnóstico/Sentado XR (solo lectura)")]
    static void Abrir() => GetWindow<DiagnosticoSentadoXR>("Diagnóstico sentado XR");

    void OnEnable()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=AlLog;
        Application.onBeforeRender+=AntesDeRender;
        SceneView.duringSceneGui+=Dibujar;
    }
    void OnDisable()
    {
        EditorApplication.update-=Tick;
        Application.logMessageReceived-=AlLog;
        Application.onBeforeRender-=AntesDeRender;
        SceneView.duringSceneGui-=Dibujar;
        midiendo=false; armado=false;
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Solo lectura. El código usa VistaSentado como destino exacto de los ojos; no valida por sí solo su colocación física. Captura una ráfaga al sentarse y genera un único informe, sin modificar la escena.",MessageType.Info);
        EditorGUI.BeginChangeCheck();
        var seleccionado=(PruebaAsientoXR)EditorGUILayout.ObjectField("Gestor del asiento",asiento,typeof(PruebaAsientoXR),true);
        pantalla=(Transform)EditorGUILayout.ObjectField("Pizarrón (opcional)",pantalla,typeof(Transform),true);
        if(EditorGUI.EndChangeCheck())
        { asiento=seleccionado; armado=false; midiendo=false; previa=null; }
        toleranciaMetros=Mathf.Max(.0001f,EditorGUILayout.FloatField("Umbral posición (m)",toleranciaMetros));
        toleranciaGrados=Mathf.Max(.01f,EditorGUILayout.FloatField("Umbral horizontal (°)",toleranciaGrados));
        bool nuevo=EditorGUILayout.Toggle("Referencias en Scene",gizmos);
        if(nuevo!=gizmos) { gizmos=nuevo; SceneView.RepaintAll(); }
        if(GUILayout.Button(armado?"Desarmar diagnóstico":"Armar próximo sentado",GUILayout.Height(32)))
        {
            if(armado) { armado=false; midiendo=false; }
            else if(Resolver())
            {
                armado=true; ultimoSentado=asiento.EstaSentado; previa=null;
                if(Application.isPlaying) previa=Leer("Previa al armado");
            }
        }
        using(new EditorGUI.DisabledScope(!Application.isPlaying || midiendo))
            if(GUILayout.Button("Medir ahora (sin sentar ni mover)"))
                if(Resolver()) Iniciar("Medición manual; no corresponde necesariamente al instante de sentado");
        EditorGUILayout.LabelField(midiendo?"Capturando durante 1 segundo…":armado?"Esperando transición a sentado…":"Diagnóstico desarmado");
        if(armado && asiento!=null && asiento.EstaSentado && !midiendo)
            EditorGUILayout.HelpBox("Ya está sentado. Levántate y vuelve a sentarte para capturar la transición.",MessageType.None);
        using(new EditorGUI.DisabledScope(string.IsNullOrEmpty(informe)))
            if(GUILayout.Button("Copiar último informe")) EditorGUIUtility.systemCopyBuffer=informe;
    }

    bool Resolver()
    {
        if(asiento==null)
        {
            var candidates=UnityEngine.Object.FindObjectsByType<PruebaAsientoXR>();
            if(candidates.Length==1) asiento=candidates[0];
        }
        if(asiento==null || !asiento.gameObject.scene.IsValid() || asiento.CamaraXR==null || asiento.VistaSentado==null)
        { Debug.LogWarning("[DIAG-SENTADO] Asigna el PruebaAsientoXR con Camera y VistaSentado válidos."); return false; }
        if(pantalla==null)
        {
            var go=GameObject.Find("PantallaVideo_Aula");
            if(go!=null && go.scene==asiento.gameObject.scene) pantalla=go.transform;
        }
        return true;
    }

    XROrigin Origin()
    {
        if(asiento==null) return null;
        return new SerializedObject(asiento).FindProperty("xrOrigin").objectReferenceValue as XROrigin;
    }

    void AlLog(string mensaje,string stack,LogType tipo)
    {
        // El mensaje existente ocurre después de MatchOrigin/MoveCamera y de mostrar el panel.
        if(!armado || midiendo || !Application.isPlaying || asiento==null || ultimoSentado ||
            !asiento.EstaSentado || mensaje!="Estudiante sentado") return;
        ultimoSentado=true;
        Iniciar("Después del log existente Estudiante sentado (misma llamada, antes de la siguiente actualización de tracking)");
    }

    void Tick()
    {
        if(!Application.isPlaying) { midiendo=false; ultimoSentado=false; previa=null; return; }
        if(asiento==null || asiento.CamaraXR==null || asiento.VistaSentado==null) return;
        double ahora=EditorApplication.timeSinceStartup;
        if(midiendo)
        {
            if(iniciadaSentado && !asiento.EstaSentado) { Finalizar("El estudiante se levantó antes de terminar."); return; }
            while(paso<tiempos.Length && ahora-inicio>=tiempos[paso])
            {
                muestras.Add(Leer("Editor tras "+tiempos[paso].ToString("0.0",CultureInfo.InvariantCulture)+" s (instante real en informe)")); paso++;
            }
            if(ahora-inicio>=1.1) Finalizar(null);
        }
        if(!armado) return;
        bool sentado=asiento.EstaSentado;
        if(sentado && !ultimoSentado && !midiendo) Iniciar("Transición detectada por Editor; instante inicial aproximado");
        ultimoSentado=sentado;
        if(!sentado && ahora>=siguientePrevia)
        { previa=Leer("Última muestra previa (sondeo 10 Hz)"); siguientePrevia=ahora+.1; }
    }

    void AntesDeRender()
    {
        if(!midiendo || antesRender>=2 || asiento==null || asiento.CamaraXR==null || asiento.VistaSentado==null) return;
        muestras.Add(Leer("onBeforeRender "+(++antesRender)));
    }

    void Iniciar(string tipo)
    {
        tipoPrueba=tipo; inicio=EditorApplication.timeSinceStartup;
        iniciadaSentado=asiento.EstaSentado;
        muestras.Clear(); paso=0; antesRender=0;
        if(previa!=null) muestras.Add(previa);
        muestras.Add(Leer("Inicial"));
        contexto=Auditar(); midiendo=true; Repaint();
    }

    Muestra Leer(string fase)
    {
        var camera=asiento.CamaraXR.transform; var vista=asiento.VistaSentado; var origin=Origin();
        var m=new Muestra {fase=fase,instante=EditorApplication.timeSinceStartup,sentado=asiento.EstaSentado,
            camara=camera.position,camaraLocal=camera.localPosition,rotCamara=camera.rotation,
            forwardCamara=camera.forward,vista=vista.position,forwardVista=vista.forward};
        if(origin!=null && origin.Origin!=null)
        { m.tieneRig=true; m.rig=origin.Origin.transform.position; m.rotRig=origin.Origin.transform.rotation; }
        if(origin!=null && origin.CameraFloorOffsetObject!=null)
        { m.tieneOffset=true; m.offset=origin.CameraFloorOffsetObject.transform.position; m.offsetLocal=origin.CameraFloorOffsetObject.transform.localPosition; }
        if(asiento.PanelPersonal!=null) { m.tienePanel=true; m.panel=asiento.PanelPersonal.transform.position; }
        if(pantalla!=null)
        {
            var renderer=pantalla.GetComponent<Renderer>();
            m.tienePantalla=true; m.destinoPantalla=renderer!=null?renderer.bounds.center:pantalla.position;
        }
        var driver=asiento.CamaraXR.GetComponent<TrackedPoseDriver>();
        var action=driver!=null?driver.positionInput.action:null;
        if(action!=null && action.enabled)
        {
            try
            {
                m.poseEntrada=action.ReadValue<Vector3>(); m.tienePoseEntrada=true;
                m.fuentePose=action.name+" | control="+(action.activeControl!=null?action.activeControl.path:"sin control activo")+" | update="+driver.updateType;
            }
            catch(InvalidOperationException) { m.fuentePose="La acción de posición no se puede leer como Vector3"; }
        }
        return m;
    }

    static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture,"({0:F5}, {1:F5}, {2:F5})",v.x,v.y,v.z);
    static string F(float v) => v.ToString("F5",CultureInfo.InvariantCulture);
    static string Ruta(Transform t) => t==null?"<ninguno>":t.parent==null?t.name:Ruta(t.parent)+"/"+t.name;
    static float Angulo(Vector3 a,Vector3 b)
    {
        a=Vector3.ProjectOnPlane(a,Vector3.up); b=Vector3.ProjectOnPlane(b,Vector3.up);
        return a.sqrMagnitude<.0001f || b.sqrMagnitude<.0001f?float.NaN:Vector3.SignedAngle(a,b,Vector3.up);
    }

    string Auditar()
    {
        var b=new StringBuilder(); var origin=Origin();
        b.AppendLine("Gestor: "+Ruta(asiento.transform)+" | escena: "+asiento.gameObject.scene.path);
        b.AppendLine("Cámara usada por el asiento="+Ruta(asiento.CamaraXR.transform)+" | Camera.main="+
            Ruta(Camera.main!=null?Camera.main.transform:null)+" | coinciden="+(Camera.main==asiento.CamaraXR));
        b.AppendLine("Vista: "+Ruta(asiento.VistaSentado)+" | local="+V(asiento.VistaSentado.localPosition));
        b.AppendLine("Su padre es una referencia geométrica; el código usa Vista como destino de OJOS, no como centro de la silla.");
        if(origin!=null)
            b.AppendLine("XROrigin="+Ruta(origin.transform)+" | Origin base="+Ruta(origin.Origin!=null?origin.Origin.transform:null)+
                " | Camera Offset="+Ruta(origin.CameraFloorOffsetObject!=null?origin.CameraFloorOffsetObject.transform:null)+
                " | modo solicitado="+origin.RequestedTrackingOriginMode+" | actual="+origin.CurrentTrackingOriginMode+" | CameraYOffset="+F(origin.CameraYOffset));
        for(var t=asiento.CamaraXR.transform;t!=null;t=t.parent)
        {
            b.AppendLine("Cadena cámara: "+Ruta(t)+" | mundial="+V(t.position)+" | local="+V(t.localPosition)+
                " | escala mundial="+V(t.lossyScale)+" | rotación local="+V(t.localEulerAngles));
            b.AppendLine("  componentes: "+string.Join(", ",t.GetComponents<Component>().Where(c=>c!=null)
                .Select(c=>c.GetType().FullName+(c is Behaviour behaviour?" [enabled="+behaviour.enabled+", activo="+behaviour.isActiveAndEnabled+"]":""))));
            if((t.lossyScale-Vector3.one).sqrMagnitude>.00001f) b.AppendLine("  AVISO: escala distinta de uno; revisar jerarquía antes de interpretar offsets.");
        }
        var behaviours=asiento.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Behaviour>(true)).Where(c=>c!=null);
        foreach(var behaviour in behaviours)
        {
            string name=behaviour.GetType().Name;
            if(name.Contains("Simulator") || name.Contains("Locomotion") || name.Contains("BodyTransformer") ||
                name.Contains("Mediator") || name.Contains("Provider") || name.Contains("TrackedPose"))
                b.AppendLine("Posible participante: "+name+" | "+Ruta(behaviour.transform)+" | activo="+behaviour.isActiveAndEnabled);
            if(behaviour is XRInteractionSimulator sim)
                b.AppendLine("  Simulator cameraTransform="+Ruta(sim.cameraTransform)+" | modo controlador="+sim.currentState.currentControllerInputMode);
        }
        foreach(var d in InputSystem.devices.OfType<TrackedDevice>())
            b.AppendLine("Tracking device: "+d.name+" | layout="+d.layout+" | enabled="+d.enabled+
                " | isTracked="+d.isTracked.ReadValue()+" | devicePosition="+V(d.devicePosition.ReadValue()));
        return b.ToString();
    }

    void Finalizar(string motivo)
    {
        midiendo=false;
        var b=new StringBuilder("[DIAG-SENTADO] INFORME\n");
        b.AppendLine("Prueba: "+tipoPrueba); b.AppendLine(contexto);
        b.AppendLine("Delta = cámara - Vista, coordenadas globales, metros. Ángulos horizontales firmados alrededor de Y.");
        foreach(var m in muestras)
        {
            Vector3 delta=m.camara-m.vista;
            b.AppendLine(m.fase+" | t="+(m.instante-inicio).ToString("F4",CultureInfo.InvariantCulture)+" s | sentado="+m.sentado+" | Cámara="+V(m.camara)+" | Vista="+V(m.vista));
            b.AppendLine("  ΔXYZ="+V(delta)+" | distancia="+F(delta.magnitude)+" m | horizontal="+F(new Vector2(delta.x,delta.z).magnitude)+
                " m | cámara→forward Vista="+F(Angulo(m.forwardCamara,m.forwardVista))+"°");
            if(m.tienePantalla) b.AppendLine("  cámara→centro pizarrón="+F(Angulo(m.forwardCamara,m.destinoPantalla-m.camara))+"° | Vista→centro pizarrón="+F(Angulo(m.forwardVista,m.destinoPantalla-m.vista))+"°");
            b.AppendLine("  Camera local="+V(m.camaraLocal)+" | Origin mundial="+(m.tieneRig?V(m.rig):"N/A")+
                " | Offset mundial="+(m.tieneOffset?V(m.offset):"N/A")+" | Offset local="+(m.tieneOffset?V(m.offsetLocal):"N/A")+
                " | panel mundial="+(m.tienePanel?V(m.panel):"N/A"));
            if(m.tienePoseEntrada) b.AppendLine("  Pose de entrada="+V(m.poseEntrada)+" | "+m.fuentePose);
        }
        var first=muestras.First(m=>m.fase=="Inicial"); var last=muestras.Last();
        float initialError=(first.camara-first.vista).magnitude;
        b.AppendLine("LECTURA ORIENTATIVA (no prueba causal):");
        b.AppendLine(initialError<=toleranciaMetros?"La muestra inicial coincide con el destino dentro del umbral configurado.":"La muestra inicial ya presenta diferencia: revisar cálculo y jerarquía antes de atribuirla al tracking.");
        b.AppendLine("Cambio posterior: cámara="+F(Vector3.Distance(first.camara,last.camara))+" m | Vista="+F(Vector3.Distance(first.vista,last.vista))+
            " m | Origin="+F(Vector3.Distance(first.rig,last.rig))+" m | rotación Origin="+F(Quaternion.Angle(first.rotRig,last.rotRig))+
            "° | cámara local="+F(Vector3.Distance(first.camaraLocal,last.camaraLocal))+" m | rotación cámara="+F(Quaternion.Angle(first.rotCamara,last.rotCamara))+"°");
        if(Vector3.Distance(first.camaraLocal,last.camaraLocal)>toleranciaMetros)
            b.AppendLine("Cambió la pose local de cámara: compatible con actualización de tracking/TrackedPoseDriver/simulador; no demuestra cuál lo causó.");
        if(first.tienePoseEntrada && last.tienePoseEntrada)
            b.AppendLine("Cambio en la acción de pose del driver="+V(last.poseEntrada-first.poseEntrada)+
                "; comparar con Δ cámara local. El control activo registrado identifica la fuente de esa acción.");
        if(first.tieneOffset && last.tieneOffset && Vector3.Distance(first.offsetLocal,last.offsetLocal)>toleranciaMetros)
            b.AppendLine("Cambió Camera Offset local: revisar actualización del modo de tracking/altura, sin compensar todavía el asiento.");
        if(first.tieneRig && (Vector3.Distance(first.rig,last.rig)>toleranciaMetros || Quaternion.Angle(first.rotRig,last.rotRig)>toleranciaGrados))
            b.AppendLine("Cambió el Origin después de sentarse: revisar locomoción, transformadores y scripts activos listados.");
        if(Vector3.Distance(first.vista,last.vista)>toleranciaMetros) b.AppendLine("El destino de asiento también cambió de posición.");
        if(Mathf.Abs(Angulo(last.forwardCamara,last.forwardVista))>toleranciaGrados) b.AppendLine("Orientación final distinta de Vista. Revisar si el usuario giró la cabeza durante la captura.");
        b.AppendLine("Un Camera Offset no nulo es normal: MoveCameraToWorldLocation debe compensar la pose completa en ese instante.");
        b.AppendLine("No se modificó ningún Transform, modo de tracking, simulador ni estado de sentado.");
        if(motivo!=null) b.AppendLine(motivo);
        informe=b.ToString(); Debug.Log(informe,asiento); Repaint();
    }

    void Dibujar(SceneView view)
    {
        if(!gizmos || !view.drawGizmos || asiento==null || asiento.VistaSentado==null || asiento.CamaraXR==null) return;
        var old=Handles.color; var z=Handles.zTest; Handles.zTest=CompareFunction.Always;
        Punto(asiento.VistaSentado.position,asiento.VistaSentado.forward,Color.yellow,"Vista: destino ojos según código");
        Punto(asiento.CamaraXR.transform.position,asiento.CamaraXR.transform.forward,Color.cyan,"Main Camera: ojos actuales");
        Handles.color=Color.white; Handles.DrawDottedLine(asiento.CamaraXR.transform.position,asiento.VistaSentado.position,4);
        var punto=asiento.VistaSentado.parent;
        if(punto!=null) { Handles.color=Color.gray; Handles.DrawWireCube(punto.position,Vector3.one*.08f); Handles.Label(punto.position,punto.name); }
        Handles.color=old; Handles.zTest=z;
    }
    static void Punto(Vector3 p,Vector3 forward,Color color,string label)
    {
        Handles.color=color; Handles.SphereHandleCap(0,p,Quaternion.identity,.035f,EventType.Repaint);
        Handles.Label(p+Vector3.up*.045f,label);
        var flat=Vector3.ProjectOnPlane(forward,Vector3.up);
        if(flat.sqrMagnitude>.0001f) Handles.ArrowHandleCap(0,p,Quaternion.LookRotation(flat),.3f,EventType.Repaint);
    }
}
