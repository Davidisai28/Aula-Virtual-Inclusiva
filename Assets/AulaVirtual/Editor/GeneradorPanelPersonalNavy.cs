using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class GeneradorPanelPersonalNavy : EditorWindow
{
    PruebaAsientoXR asiento;
    ControladorVideoXR controlador;
    Material vidrio;
    TMP_FontAsset fuente;
    static Color C(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
    [MenuItem("Aula Virtual/Panel personal Navy Glass")]
    static void Abrir() => GetWindow<GeneradorPanelPersonalNavy>("Panel Navy Glass");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Barra compacta, controles y biblioteca personal comparten los servicios actuales. Se conservan tus medidas y no se guarda la escena principal.",MessageType.Info);
        asiento=(PruebaAsientoXR)EditorGUILayout.ObjectField("Gestor del asiento",asiento,typeof(PruebaAsientoXR),true);
        controlador=(ControladorVideoXR)EditorGUILayout.ObjectField("Controlador existente",controlador,typeof(ControladorVideoXR),true);
        if(GUILayout.Button("Aplicar / actualizar (con respaldo)",GUILayout.Height(36))) Aplicar();
        if(GUILayout.Button("Previsualizar desde el asiento",GUILayout.Height(28))) Previsualizar();
        if(GUILayout.Button("Validar panel y conexiones",GUILayout.Height(28)))
        { if(Resolver() && asiento.PanelPersonal!=null) Validar(asiento.PanelPersonal); }
        if(GUILayout.Button("Restaurar presentación anterior",GUILayout.Height(28))) Restaurar();
    }

    bool Resolver()
    {
        if(Application.isPlaying) { Debug.LogWarning("Detén Play para aplicar el panel."); return false; }
        if(asiento==null)
        {
            var candidates=UnityEngine.Object.FindObjectsByType<PruebaAsientoXR>();
            if(candidates.Length==1) asiento=candidates[0];
        }
        if(asiento==null || EditorUtility.IsPersistent(asiento) || !asiento.gameObject.scene.IsValid())
        { Debug.LogError("Asigna el PruebaAsientoXR de la escena."); return false; }
        return true;
    }

    bool Respaldar()
    {
        if(string.IsNullOrEmpty(asiento.gameObject.scene.path)) { Debug.LogError("Guarda la escena inicial antes de crear su respaldo."); return false; }
        const string folder="Assets/AulaVirtual/Scenes/BackupsPanelPersonal";
        Directory.CreateDirectory(folder);
        string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+asiento.gameObject.scene.name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity");
        bool dirty=asiento.gameObject.scene.isDirty;
        bool ok=EditorSceneManager.SaveScene(asiento.gameObject.scene,path,true);
        if(dirty) EditorSceneManager.MarkSceneDirty(asiento.gameObject.scene);
        if(!ok) Debug.LogError("No se pudo respaldar. Operación cancelada.");
        else Debug.Log("Respaldo de escena: "+path);
        return ok;
    }

    void BuscarControlador()
    {
        if(controlador!=null) return;
        if(asiento.PanelPersonal!=null) controlador=asiento.PanelPersonal.controlador;
        if(controlador!=null) return;
        var serialized=new SerializedObject(asiento);
        var legacy=serialized.FindProperty("panelControlesVideo").objectReferenceValue as GameObject;
        if(legacy==null) return;
        var targets=legacy.GetComponentsInChildren<XRSimpleInteractable>(true)
            .SelectMany(i=>Enumerable.Range(0,i.selectEntered.GetPersistentEventCount())
                .Select(index=>i.selectEntered.GetPersistentTarget(index))).OfType<ControladorVideoXR>().Distinct().ToArray();
        if(targets.Length==1) controlador=targets[0];
    }

    PanelPersonalNavyGlass[] Propios() => asiento.GetComponentsInChildren<PanelPersonalNavyGlass>(true)
        .Where(p=>p.transform.parent==asiento.transform && p.name==PanelPersonalNavyGlass.Id).ToArray();

    void BorrarPropios()
    {
        foreach(var panel in Propios())
        {
            foreach(var graphic in panel.GetComponentsInChildren<Graphic>(true))
            {
                if(graphic.GetComponent<CanvasRenderer>()==null) graphic.gameObject.AddComponent<CanvasRenderer>();
                VidrioBibliotecaGraphic.RestablecerRenderer(graphic);
            }
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject,"Ocultar panel anterior");
            panel.gameObject.SetActive(false);
            Undo.DestroyObjectImmediate(panel.gameObject);
        }
    }

    void Aplicar()
    {
        if(!Resolver()) return;
        BuscarControlador();
        if(controlador==null || controlador.gameObject.scene!=asiento.gameObject.scene || asiento.CamaraXR==null || asiento.VistaSentado==null)
        { Debug.LogError("Revisa el controlador multimedia existente, Camera XR y VistaSentado. No se modificó la escena."); return; }
        var servicios=asiento.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GestorCacheVideos>(true))
            .Where(g=>g.reproductor==controlador.GetComponent<UnityEngine.Video.VideoPlayer>()).ToArray();
        var catalogos=asiento.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogoVideosDrive>(true)).ToArray();
        if(servicios.Length!=1 || !catalogos.Any(c=>c.gestorCache==servicios[0]))
        { Debug.LogError("Se requiere un GestorCacheVideos existente para ese reproductor y un CatalogoVideosDrive enlazado a él. No se ha cambiado la interfaz."); return; }
        if(!Respaldar()) return;
        fuente=FuenteMediatecaEditor.Obtener(); if(fuente==null) return;
        var shader=Shader.Find("AulaVirtual/UI/LiquidGlass");
        if(shader==null) { Debug.LogError("Espera a importar el shader LiquidGlass existente."); return; }
        const string path="Assets/AulaVirtual/Materials/MAT_UI_NavyGlass.mat";
        vidrio=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(vidrio==null) { vidrio=new Material(shader){name="MAT_UI_NavyGlass"}; AssetDatabase.CreateAsset(vidrio,path); }
        var old=asiento.PanelPersonal;
        float distancia=old!=null?old.distancia:.85f, altura=old!=null?old.altura:-.28f;
        float ancho=old!=null?old.anchoMetros:.78f, lateral=old!=null?old.desplazamientoLateral:0;
        float anchoBiblioteca=old!=null?old.anchoBibliotecaMetros:1f;
        Vector3 orientacion=old!=null?old.ajusteOrientacion:Vector3.zero;
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Aplicar panel personal Navy");
        BorrarPropios();
        var root=new GameObject(PanelPersonalNavyGlass.Id,typeof(RectTransform),typeof(Canvas),typeof(PanelPersonalNavyGlass));
        root.SetActive(false); root.transform.SetParent(asiento.transform,false);
        var rect=(RectTransform)root.transform; rect.sizeDelta=new Vector2(1200,620);
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=asiento.CamaraXR;
        rect.sizeDelta=new Vector2(1200,620);
        canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2|AdditionalCanvasShaderChannels.TexCoord3;
        var scaler=root.AddComponent<CanvasScaler>(); scaler.dynamicPixelsPerUnit=2; scaler.referencePixelsPerUnit=100;
        root.AddComponent<TrackedDeviceGraphicRaycaster>().checkFor3DOcclusion=true;
        var panel=root.GetComponent<PanelPersonalNavyGlass>(); panel.asiento=asiento; panel.controlador=controlador;
        panel.gestorCache=servicios[0];
        panel.distancia=distancia; panel.altura=altura; panel.anchoMetros=ancho; panel.desplazamientoLateral=lateral; panel.ajusteOrientacion=orientacion;
        panel.anchoBibliotecaMetros=anchoBiblioteca;
        var controles=Box(root.transform,"VistaControles",0,0,1200,620);
        panel.vistaControles=controles.gameObject;
        Superficie(controles,"Sombra",8,10,1184,610,"0B1F3A","0B1F3A",.25f,40);
        var fondo=Superficie(controles,"CristalNavy",0,0,1200,610,"102B4E","0B1F3A",.99f,40);
        Texto(fondo,"TituloAula","Aula Virtual",48,28,1104,40,30,C("D7E4F2"));
        panel.tituloVideo=Texto(fondo,"TituloVideo","Sin video seleccionado",48,80,1104,104,42,C("F5F8FF"));
        panel.tituloVideo.maxVisibleLines=2; panel.tituloVideo.textWrappingMode=TextWrappingModes.Normal;
        panel.estadoVideo=Texto(fondo,"EstadoVideo","Detenido",48,192,1104,44,32,C("A9BDD3"));
        Accion(fondo,"Reproducir",48,262,252,132,controlador.Reproducir,DecoracionMediatecaGraphic.Forma.Play);
        Accion(fondo,"Pausar",332,262,252,132,controlador.Pausar,DecoracionMediatecaGraphic.Forma.Pausa);
        Accion(fondo,"Reiniciar",616,262,252,132,controlador.Reiniciar,DecoracionMediatecaGraphic.Forma.Reiniciar);
        Accion(fondo,"Detener",900,262,252,132,controlador.Detener,DecoracionMediatecaGraphic.Forma.Detener);
        Accion(fondo,"Biblioteca",48,442,352,104,panel.AbrirBiblioteca,null);
        Accion(fondo,"Minimizar",424,442,352,104,panel.Minimizar,null);
        Accion(fondo,"Levantarse",800,442,352,104,asiento.LevantarseDesdeBoton,null);
        var barra=Box(root.transform,"BarraCompacta",0,442,1200,128);
        panel.barraCompacta=barra.gameObject;
        var superficieBarra=Superficie(barra,"CristalBarra",0,0,1200,128,"102B4E","0B1F3A",.99f,40);
        Accion(superficieBarra,"Biblioteca",48,16,352,96,panel.AbrirBiblioteca,null);
        Accion(superficieBarra,"Controles",424,16,352,96,panel.AbrirControles,null);
        Accion(superficieBarra,"Levantarse",800,16,352,96,asiento.LevantarseDesdeBoton,null);
        CrearBiblioteca(panel,root.transform);
        controles.gameObject.SetActive(false);
        panel.Colocar(asiento.VistaSentado);
        Validar(panel);
        Undo.RegisterCreatedObjectUndo(root,"Crear panel personal");
        Undo.RecordObject(asiento,"Conectar panel al asiento");
        var so=new SerializedObject(asiento); so.FindProperty("panelPersonal").objectReferenceValue=panel; so.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(asiento.gameObject.scene);
        Selection.activeGameObject=root;
        Debug.Log("[PANEL-NAVY] Panel conectado, oculto hasta sentarse. Usa Previsualizar para inspeccionarlo en Scene. Escena SIN guardar.",panel);
    }

    void CrearBiblioteca(PanelPersonalNavyGlass panel,Transform parent)
    {
        var group=Box(parent,"BibliotecaCercana",0,0,1600,900);
        group.gameObject.SetActive(false);
        panel.bibliotecaCercanaReservada=group.gameObject;
        var ui=group.gameObject.AddComponent<BibliotecaVisualAula>();
        panel.bibliotecaPersonal=ui; ui.panelPersonal=panel; ui.gestorCache=panel.gestorCache;
        var sources=asiento.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogoVideosDrive>(true)).ToArray();
        ui.catalogoDrive=sources.FirstOrDefault(c=>c.gestorCache==panel.gestorCache) ?? (sources.Length==1?sources[0]:null);
        var fondo=Superficie(group,"FondoAzul",0,0,1600,900,"102B4E","0B1F3A",.995f,40);
        Texto(fondo,"Titulo","Biblioteca personal",64,48,1040,80,60,C("F5F8FF"));
        ui.actualizar=Accion(fondo,"Actualizar",1200,48,336,88,null,null);
        ui.estadoCatalogo=Texto(fondo,"EstadoCatalogo","Pulsa Actualizar para consultar",64,148,1472,50,40,C("A9BDD3"));
        var content=Box(fondo,"ListaVideos",64,230,1472,456); ui.contenido=content;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing=24; layout.childControlWidth=true; layout.childControlHeight=true;
        layout.childForceExpandHeight=false; layout.childForceExpandWidth=true;
        var row=Box(content,"PlantillaFilaVideo",0,0,1472,216);
        var le=row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight=216; le.minHeight=216;
        Superficie(row,"Superficie",0,0,1472,208,"19395F","102B4E",.995f,26);
        var title=Texto(row,"NombreTMP","Nombre del video",36,18,1040,116,48,C("F5F8FF"));
        title.maxVisibleLines=2; title.textWrappingMode=TextWrappingModes.Normal;
        Texto(row,"TamanoTMP","—",36,152,240,50,40,C("A9BDD3"));
        var badge=Box(row,"Insignia",310,152,710,50);
        Texto(badge,"TextoEstadoTMP","Disponible",0,0,710,50,40,C("D7E4F2"));
        Texto(row,"EstadoTMP","",36,152,1040,50,40,C("D7E4F2")).gameObject.SetActive(false);
        Accion(row,"AccionVideo",1136,54,300,100,null,null).GetComponentInChildren<TMP_Text>().text="Descargar";
        var progress=Superficie(row,"ProgresoDescarga",36,204,1040,4,"294B70","294B70",1,2);
        Superficie(progress,"Relleno",0,0,0,4,"80CFC7","80CFC7",1,2);
        progress.gameObject.SetActive(false);
        row.gameObject.AddComponent<FilaCatalogoVideoAula>(); row.gameObject.SetActive(false); ui.plantillaFila=row.gameObject;
        ui.totalVideos=Texto(fondo,"TotalVideos","0 videos",64,720,300,72,40,C("A9BDD3"));
        ui.totalVideos.alignment=TextAlignmentOptions.MidlineLeft;
        ui.anterior=Accion(fondo,"Anterior",400,720,216,72,null,null);
        ui.indicadorPagina=Texto(fondo,"IndicadorPagina","1 / 1",632,720,168,72,40,C("D7E4F2"));
        ui.indicadorPagina.alignment=TextAlignmentOptions.Center;
        ui.siguiente=Accion(fondo,"Siguiente",816,720,240,72,null,null);
        ui.anterior.gameObject.SetActive(false); ui.siguiente.gameObject.SetActive(false); ui.indicadorPagina.gameObject.SetActive(false);
        Accion(fondo,"Volver",64,818,360,64,panel.Minimizar,null);
        Accion(fondo,"Levantarse",1136,818,400,64,asiento.LevantarseDesdeBoton,null);
        ui.entrarCine=null; ui.estadoSinConexion=null;
    }

    static void Validar(PanelPersonalNavyGlass panel)
    {
        bool ok=true;
        var corners=new Vector3[4];
        foreach(var graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            var basis=panel.bibliotecaPersonal!=null && graphic.transform.IsChildOf(panel.bibliotecaPersonal.transform)
                ? (RectTransform)panel.bibliotecaPersonal.transform : (RectTransform)panel.transform;
            if(graphic.GetComponent<CanvasRenderer>()==null) {Debug.LogError("Falta CanvasRenderer: "+graphic.name,graphic);ok=false;}
            graphic.rectTransform.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var p=basis.InverseTransformPoint(corner);
                if(p.x<basis.rect.xMin-.5f || p.x>basis.rect.xMax+.5f || p.y<basis.rect.yMin-.5f || p.y>basis.rect.yMax+.5f)
                {Debug.LogError("Fuera del panel: "+graphic.name,graphic);ok=false;break;}
            }
        }
        string[] actions={"Reproducir","Pausar","Reiniciar","Detener","Levantarse"};
        string[] methods={"Reproducir","Pausar","Reiniciar","Detener","LevantarseDesdeBoton"};
        for(int i=0;i<actions.Length;i++)
        {
            var button=panel.transform.Find("VistaControles/CristalNavy/"+actions[i])?.GetComponent<Button>();
            UnityEngine.Object expected=i==4?(UnityEngine.Object)panel.asiento:panel.controlador;
            if(button==null || !button.interactable || button.onClick.GetPersistentEventCount()!=1 ||
                button.onClick.GetPersistentTarget(0)!=expected || button.onClick.GetPersistentMethodName(0)!=methods[i])
            {Debug.LogError("Conexión inesperada: "+actions[i],panel);ok=false;}
        }
        string[] rutas={"BarraCompacta/CristalBarra/Biblioteca","BarraCompacta/CristalBarra/Controles","BarraCompacta/CristalBarra/Levantarse",
            "VistaControles/CristalNavy/Minimizar","VistaControles/CristalNavy/Biblioteca","BibliotecaCercana/FondoAzul/Volver","BibliotecaCercana/FondoAzul/Levantarse"};
        string[] destinos={"AbrirBiblioteca","AbrirControles","LevantarseDesdeBoton","Minimizar","AbrirBiblioteca","Minimizar","LevantarseDesdeBoton"};
        for(int i=0;i<rutas.Length;i++)
        {
            var button=panel.transform.Find(rutas[i])?.GetComponent<Button>();
            UnityEngine.Object expected=destinos[i]=="LevantarseDesdeBoton"?(UnityEngine.Object)panel.asiento:panel;
            if(button==null || !button.interactable || button.onClick.GetPersistentEventCount()!=1 ||
                button.onClick.GetPersistentTarget(0)!=expected || button.onClick.GetPersistentMethodName(0)!=destinos[i])
            {Debug.LogError("Conexión personal inesperada: "+rutas[i],panel);ok=false;}
        }
        var library=panel.bibliotecaPersonal;
        if(library==null || library.panelPersonal!=panel || library.gestorCache!=panel.gestorCache ||
            library.catalogoDrive==null || library.catalogoDrive.gestorCache!=panel.gestorCache || library.entrarCine!=null)
        {Debug.LogError("Revisar referencias compartidas de biblioteca personal.",panel);ok=false;}
        if(panel.barraCompacta==null || panel.vistaControles==null || panel.bibliotecaCercanaReservada==null)
        {Debug.LogError("Faltan vistas personales.",panel);ok=false;}
        if(ok) Debug.Log("[PANEL-NAVY] Vistas, renderers, límites, controles originales y servicios compartidos verificados.",panel);
    }

    void Previsualizar()
    {
        if(!Resolver() || asiento.PanelPersonal==null || asiento.VistaSentado==null) return;
        var panel=asiento.PanelPersonal;
        Undo.RegisterFullObjectHierarchyUndo(panel.gameObject,"Previsualizar panel personal");
        panel.Colocar(asiento.VistaSentado); panel.gameObject.SetActive(true);
        Selection.activeGameObject=panel.gameObject; SceneView.lastActiveSceneView?.FrameSelected();
        EditorSceneManager.MarkSceneDirty(asiento.gameObject.scene);
    }

    void Restaurar()
    {
        if(!Resolver() || !Respaldar()) return;
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Restaurar controles del asiento");
        Undo.RecordObject(asiento,"Desconectar panel personal");
        var so=new SerializedObject(asiento); so.FindProperty("panelPersonal").objectReferenceValue=null; so.ApplyModifiedProperties();
        BorrarPropios(); Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(asiento.gameObject.scene);
        Debug.Log("[PANEL-NAVY] Presentación original restaurada para el próximo Play. Objetos y eventos originales conservados.");
    }

    static RectTransform Box(Transform p,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(p,false);
        var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
    }
    RectTransform Superficie(Transform p,string name,float x,float y,float w,float h,string top,string bottom,float alpha,float radio)
    {
        var r=Box(p,name,x,y,w,h); r.gameObject.AddComponent<CanvasRenderer>();
        var g=r.gameObject.AddComponent<VidrioBibliotecaGraphic>(); g.material=vidrio; g.vidrioAnalitico=true;
        Color a=C(top), b=C(bottom); a.a=alpha; b.a=alpha; g.color=a; g.colorInferior=b; g.radio=radio;
        g.grosorBorde=.8f; g.colorBorde=new Color(.66f,.85f,1,.17f); g.raycastTarget=false; return r;
    }
    TMP_Text Texto(Transform p,string name,string value,float x,float y,float w,float h,float size,Color color)
    {
        var r=Box(p,name,x,y,w,h); r.gameObject.AddComponent<CanvasRenderer>();
        var t=r.gameObject.AddComponent<TextMeshProUGUI>(); t.font=fuente; t.text=value; t.fontSize=size; t.color=color;
        t.enableAutoSizing=false; t.extraPadding=true; t.richText=false; t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.NoWrap; t.maxVisibleLines=1; t.overflowMode=TextOverflowModes.Ellipsis; return t;
    }
    Button Accion(Transform p,string name,float x,float y,float w,float h,UnityAction action,DecoracionMediatecaGraphic.Forma? icon)
    {
        var r=Superficie(p,name,x,y,w,h,"19395F","102B4E",.97f,32);
        var g=r.GetComponent<VidrioBibliotecaGraphic>(); g.raycastTarget=true;
        var button=r.gameObject.AddComponent<BotonVidrioAula>(); button.targetGraphic=g;
        var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.12f,1.15f,1.18f);
        colors.pressedColor=new Color(.65f,.75f,.85f); colors.disabledColor=new Color(.65f,.65f,.65f,.55f); colors.fadeDuration=.12f; button.colors=colors;
        button.navigation=new Navigation{mode=Navigation.Mode.None};
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        if(icon.HasValue)
        {
            var ir=Box(r,"Icono",(w-40)/2,20,40,40); ir.gameObject.AddComponent<CanvasRenderer>();
            var ig=ir.gameObject.AddComponent<DecoracionMediatecaGraphic>(); ig.forma=icon.Value; ig.color=C("D7E4F2"); ig.raycastTarget=false;
        }
        var label=Texto(r,"TextoTMP",name,16,icon.HasValue?76:0,w-32,icon.HasValue?44:h,40,C("F5F8FF"));
        label.alignment=TextAlignmentOptions.Center;
        return button;
    }
}
