using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public sealed class IntegradorManosRealesAula : EditorWindow
{
    const string Hands="com.unity.xr.hands", Version="1.8.1", XRI="com.unity.xr.interaction.toolkit";
    const string ControlesOriginales="Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Hand Controls.inputactions";
    const string ControlesAula="Assets/AulaVirtual/Settings/ManosSimuladasAula.inputactions";
    XROrigin rig;
    AddRequest peticion;
    string estado="";
    [MenuItem("Aula Virtual/Manos reales y simuladas")]
    static void Abrir()=>GetWindow<IntegradorManosRealesAula>("Manos reales / simuladas");
    void OnEnable()
    {
        EditorApplication.update-=Esperar;
        EditorApplication.update+=Esperar;
    }
    void OnDisable()=>EditorApplication.update-=Esperar;
    static UnityEditor.PackageManager.PackageInfo Paquete(string name)=>UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p=>p.name==name);
    void OnGUI()
    {
        var installed=Paquete(Hands);
        EditorGUILayout.HelpBox("Sin visor se prueban poses simuladas, pinza y Poke. La calidad del tracking físico queda pendiente. No se sustituye el rig ni se guarda la escena principal.",MessageType.Info);
        EditorGUILayout.LabelField("XR Hands",installed!=null?installed.version:"Pendiente de resolver/importar en Unity");
        rig=(XROrigin)EditorGUILayout.ObjectField("XR Origin existente",rig,typeof(XROrigin),true);
        using(new EditorGUI.DisabledScope(Application.isPlaying || EditorApplication.isCompiling || peticion!=null))
        {
            if(GUILayout.Button("1. Resolver XR Hands 1.8.1")) {peticion=Client.Add(Hands+"@"+Version);estado="Resolviendo paquete oficial…";}
            if(GUILayout.Button("2. Importar samples oficiales (sin sobrescribir)")) Importar();
            if(GUILayout.Button("3. Integrar manos en el rig (con respaldo)",GUILayout.Height(34))) Integrar();
            if(GUILayout.Button("Reparar referencias existentes (sin regenerar)")) RepararReferencias();
            if(GUILayout.Button("Preparar OpenXR para Quest / Link (con respaldo)")) ConfigurarOpenXR();
            if(GUILayout.Button("Retirar únicamente las manos integradas")) Retirar();
        }
        if(GUILayout.Button("Diagnosticar configuración")) Diagnosticar();
        EditorGUILayout.HelpBox("Play: usa el modo Mando XR existente; en el simulador cambia a Hands. F8 = Poke, M = Pinch, F9 = Open. Se evita el conflicto de N/O con Levantarse/Volver al aula.",MessageType.None);
        if(!string.IsNullOrEmpty(estado)) EditorGUILayout.LabelField(estado,EditorStyles.wordWrappedLabel);
    }
    void Esperar()
    {
        var solicitud=peticion;
        if(solicitud==null || !solicitud.IsCompleted)return;
        var resultado=solicitud.Status;
        // No interpretar estados transitorios como fallos: Error puede ser null.
        if(resultado!=StatusCode.Success && resultado!=StatusCode.Failure)return;
        // Consumir antes de informar para que un fallo no se repita en cada update.
        peticion=null;
        if(resultado==StatusCode.Success)
            estado="Paquete resuelto; espera a la compilación antes del paso 2.";
        else
        {
            var error=solicitud.Error;
            estado=string.IsNullOrEmpty(error?.message)
                ? "Package Manager no pudo resolver XR Hands y no devolvió detalles. Revisa Window > Package Manager."
                : "No se pudo resolver XR Hands: "+error.message;
            Debug.LogError("[MANOS-REALES] "+estado);
        }
        Repaint();
    }
    bool Listo()
    {
        var p=Paquete(Hands);
        if(p==null || !System.Version.TryParse(p.version.Split('-')[0],out var v) || v<new System.Version(1,8,0))
        {Debug.LogError("XR Hands 1.8+ debe estar resuelto y compilado antes de integrar.");return false;}
        return true;
    }
    void Importar()
    {
        if(!Listo())return;
        ImportarSample(Hands,Paquete(Hands).version,"HandVisualizer");
        ImportarSample(XRI,Paquete(XRI).version,"Hands Interaction Demo");
        estado="Samples importados. Espera a que Unity termine de compilar antes del paso 3.";
    }
    static void ImportarSample(string paquete,string version,string nombre)
    {
        var options=Sample.FindByPackage(paquete,version).Where(s=>s.displayName==nombre).ToArray();
        if(options.Length!=1)throw new InvalidOperationException("No se encontró el sample "+nombre);
        if(!options[0].isImported && !options[0].Import(Sample.ImportOptions.None))throw new InvalidOperationException("No se pudo importar "+nombre);
    }
    bool Resolver()
    {
        if(Application.isPlaying){Debug.LogWarning("Detén Play antes de integrar o retirar.");return false;}
        if(rig==null){var all=UnityEngine.Object.FindObjectsByType<XROrigin>();if(all.Length==1)rig=all[0];}
        if(rig==null || !rig.gameObject.scene.IsValid() || EditorUtility.IsPersistent(rig) || rig.CameraFloorOffsetObject==null || rig.Camera==null)
        {Debug.LogError("Asigna el rig de la escena, con cámara y Camera Offset válidos.");return false;}
        return true;
    }
    bool Respaldar(string etiqueta)
    {
        if(string.IsNullOrEmpty(rig.gameObject.scene.path))return false;
        string folder="Assets/AulaVirtual/Scenes/BackupsManosReales";Directory.CreateDirectory(folder);
        string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+rig.gameObject.scene.name+"_"+etiqueta+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity");
        bool dirty=rig.gameObject.scene.isDirty,ok=EditorSceneManager.SaveScene(rig.gameObject.scene,path,true);
        if(dirty)EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        if(!ok)Debug.LogError("No se pudo respaldar la escena.");
        return ok;
    }
    static string SamplePath(string package,string nombre)
    {
        var p=Paquete(package);if(p==null)return null;
        var samples=Sample.FindByPackage(package,p.version).Where(s=>s.displayName==nombre).ToArray();
        if(samples.Length!=1 || !samples[0].isImported)return null;
        string path=samples[0].importPath;
        if(string.IsNullOrEmpty(path))return null;
        // Package Manager puede devolver importPath absoluto; AssetDatabase exige Assets/...
        path=path.Replace('\\','/');
        if(Path.IsPathRooted(path))path=FileUtil.GetProjectRelativePath(path);
        return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/",StringComparison.Ordinal)
            ? path.TrimEnd('/') : null;
    }
    void Integrar()
    {
        if(!Resolver() || !Listo())return;
        var manager=rig.GetComponent<XRInputModalityManager>();
        var systems=UnityEngine.Object.FindObjectsByType<EventSystem>();
        if(manager==null || manager.leftController==null || manager.rightController==null || systems.Length!=1 ||
            systems[0].GetComponents<BaseInputModule>().Length!=1 || systems[0].GetComponent<XRUIInputModule>()==null)
        {Debug.LogError("Se requieren el gestor de modalidad y el único EventSystem/XRUIInputModule existentes. No se crean sustitutos.");return;}
        string samplePath=SamplePath(XRI,"Hands Interaction Demo");
        if(samplePath==null){Debug.LogError("[MANOS-REALES] El sample Hands Interaction Demo no está importado en Assets. Ejecuta el paso 2 y espera a la compilación.");return;}
        string prefab=samplePath+"/Prefabs/XR Origin Hands (XR Rig).prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefab)==null){Debug.LogError("[MANOS-REALES] No se pudo cargar el prefab en "+prefab+". Si el archivo existe, espera a que Unity termine de importarlo; si falta, revisa el sample en Package Manager.");return;}
        if(!Respaldar("Integracion"))return;
        GameObject contents=null,holder=null,candidate=null;
        int group=-1;
        try
        {
            // El rig de ejemplo se carga SOLO en la escena aislada de edición de prefab.
            contents=PrefabUtility.LoadPrefabContents(prefab);
            // Desempaquetar solo la copia temporal: permite extraer ramas de un variant.
            if(PrefabUtility.IsPartOfPrefabInstance(contents))
                PrefabUtility.UnpackPrefabInstance(contents,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            if(contents.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))
                throw new InvalidOperationException("El sample tiene scripts sin resolver. Espera a compilar y revisa Project Validation.");
            var names=new[]{"Left Hand","Right Hand","Hand Visualizer"};
            var roots=names.Select(n=>contents.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n)).ToArray();
            holder=new GameObject(ConfiguracionManosRealesAula.Id);holder.SetActive(false);
            SceneManager.MoveGameObjectToScene(holder,contents.scene);
            foreach(var root in roots)root.SetParent(holder.transform,false);
            Remapear(holder,contents.transform,rig.transform);
            var originals=rig.GetComponentsInChildren<ConfiguracionManosRealesAula>(true).Where(c=>c.transform.parent==rig.CameraFloorOffsetObject.transform).ToArray();
            var old=originals.FirstOrDefault();
            var prevLeft=old!=null?old.izquierdaAnterior:manager.leftHand;
            var prevRight=old!=null?old.derechaAnterior:manager.rightHand;
            // Primero construir/validar la copia nueva; después retirar raíces propias anteriores.
            candidate=Instantiate(holder,rig.CameraFloorOffsetObject.transform,false);candidate.name=ConfiguracionManosRealesAula.Id;
            candidate.transform.localPosition=Vector3.zero;candidate.transform.localRotation=Quaternion.identity;candidate.transform.localScale=Vector3.one;
            if(candidate.GetComponentsInChildren<XROrigin>(true).Length!=0 || candidate.GetComponentsInChildren<Camera>(true).Length!=0 || candidate.GetComponentsInChildren<EventSystem>(true).Length!=0)
                throw new InvalidOperationException("La copia incluye componentes de rig prohibidos.");
            var marker=candidate.AddComponent<ConfiguracionManosRealesAula>();marker.gestorModalidad=manager;
            marker.manoIzquierda=candidate.transform.Find("Left Hand").gameObject;marker.manoDerecha=candidate.transform.Find("Right Hand").gameObject;
            marker.izquierdaAnterior=prevLeft;marker.derechaAnterior=prevRight;
            Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Integrar manos reales XRI");
            Undo.RegisterCreatedObjectUndo(candidate,"Crear ramas de manos");
            Undo.RecordObject(manager,"Alternancia manos y mandos");manager.leftHand=marker.manoIzquierda;manager.rightHand=marker.manoDerecha;
            RegistrarOverrides(manager);
            foreach(var original in originals)Undo.DestroyObjectImmediate(original.gameObject);
            ConfigurarTeclasSimuladas(false);
            Undo.RecordObject(candidate,"Activar ramas de manos");candidate.SetActive(true);Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
            Selection.activeGameObject=candidate;candidate=null;
            Debug.Log("[MANOS-REALES] Integradas solo las ramas de manos, Poke, pinza y visualizador en el rig existente. Mandos/locomoción intactos. Escena SIN guardar.");
            Diagnosticar();
        }
        catch(Exception e)
        {
            if(group>=0)Undo.RevertAllDownToGroup(group);
            if(candidate!=null)DestroyImmediate(candidate);
            Debug.LogError("Integración cancelada: "+e.Message);
        }
        finally
        {
            if(holder!=null)DestroyImmediate(holder);
            if(contents!=null)PrefabUtility.UnloadPrefabContents(contents);
        }
    }
    static void Remapear(GameObject holder,Transform fuente,Transform destino)
    {
        foreach(var c in holder.GetComponentsInChildren<Component>(true))
        {
            if(c==null || c is Transform)continue;
            var so=new SerializedObject(c);var p=so.GetIterator();
            while(p.Next(true))
            {
                if(p.propertyType!=SerializedPropertyType.ObjectReference)continue;
                var reference=p.objectReferenceValue;
                Transform t=reference is GameObject go?go.transform:reference is Component co?co.transform:null;
                if(t==null || EditorUtility.IsPersistent(reference) || t.IsChildOf(holder.transform))continue;
                if(t!=fuente && !t.IsChildOf(fuente))throw new InvalidOperationException("Referencia externa no reconocida: "+c.name+"."+p.propertyPath);
                string path=AnimationUtility.CalculateTransformPath(t,fuente);
                Transform match=string.IsNullOrEmpty(path)?destino:destino.Find(path);
                UnityEngine.Object replacement=match==null?null:reference is GameObject?(UnityEngine.Object)match.gameObject:match.GetComponent(reference.GetType());
                if(replacement==null)throw new InvalidOperationException("No se puede remapear "+path+" en "+c.name+"."+p.propertyPath);
                p.objectReferenceValue=replacement;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    void ConfigurarTeclasSimuladas(bool restaurar)
    {
        if(!restaurar && !File.Exists(ControlesAula))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControlesAula));
            string json=File.ReadAllText(ControlesOriginales).Replace("<Keyboard>/n","<Keyboard>/f8").Replace("<Keyboard>/o","<Keyboard>/f9");
            File.WriteAllText(ControlesAula,json);AssetDatabase.ImportAsset(ControlesAula,ImportAssetOptions.ForceSynchronousImport);
        }
        string from=restaurar?ControlesAula:ControlesOriginales,to=restaurar?ControlesOriginales:ControlesAula;
        var targetAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>(to);
        if(targetAsset==null)throw new InvalidOperationException("No se importaron las acciones de manos: "+to);
        // El importador puede incluir varias InputActionReference para una misma acción.
        // Son alias, no acciones distintas: conservar una referencia por ID.
        var target=new Dictionary<Guid,InputActionReference>();
        foreach(var reference in AssetDatabase.LoadAllAssetsAtPath(to).OfType<InputActionReference>())
        {
            var action=reference.action;
            if(action==null || action.actionMap==null || action.actionMap.asset!=targetAsset)continue;
            if(target.TryGetValue(action.id,out var existing))
            {
                if(existing.action.actionMap.id!=action.actionMap.id)
                    throw new InvalidOperationException("ID de acción ambiguo entre mapas en "+to+": "+action.id);
                continue;
            }
            target.Add(action.id,reference);
        }
        if(target.Count==0)throw new InvalidOperationException("No se importaron referencias de acciones de manos: "+to);
        foreach(var sim in UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include))
        {
            if(sim.gameObject.scene!=rig.gameObject.scene)continue;
            foreach(var c in sim.GetComponentsInChildren<Component>(true))
            {
                if(c==null || c is Transform)continue;
                var so=new SerializedObject(c);var p=so.GetIterator();bool changed=false;
                while(p.Next(true))
                {
                    if(p.propertyType!=SerializedPropertyType.ObjectReference || p.objectReferenceValue==null || AssetDatabase.GetAssetPath(p.objectReferenceValue)!=from)continue;
                    UnityEngine.Object replacement=null;
                    if(p.objectReferenceValue is InputActionReference r && r.action!=null && target.TryGetValue(r.action.id,out var actionReference))replacement=actionReference;
                    else if(p.objectReferenceValue is InputActionAsset)replacement=targetAsset;
                    if(replacement==null)continue;
                    if(!changed)Undo.RecordObject(c,"Teclas seguras del simulador");p.objectReferenceValue=replacement;changed=true;
                }
                if(changed){so.ApplyModifiedProperties();RegistrarOverrides(c);}
            }
        }
    }
    void ConfigurarOpenXR()
    {
        if(!Resolver() || !Listo() || !Respaldar("OpenXR"))return;
        foreach(var platform in new[]{BuildTargetGroup.Android,BuildTargetGroup.Standalone})
        {
            var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(platform);
            if(settings==null){Debug.LogWarning("Abre Project Settings > XR Plug-in Management > OpenXR para "+platform);continue;}
            var features=settings.GetFeatures<OpenXRFeature>();
            foreach(string typeName in new[]{"HandTracking","MetaHandTrackingAim"})
            {
                var matches=features.Where(f=>f!=null && f.GetType().Name==typeName && f.GetType().Namespace=="UnityEngine.XR.Hands.OpenXR").ToArray();
                if(matches.Length!=1){Debug.LogWarning("No se encontró "+typeName+" en "+platform+". Espera a importar XR Hands y abre la configuración OpenXR.");continue;}
                var f=matches[0];string path=AssetDatabase.GetAssetPath(f);
                Directory.CreateDirectory("Backups/OpenXRManos");
                File.Copy(path,"Backups/OpenXRManos/"+platform+"_"+typeName+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".asset",false);
                Undo.RecordObject(f,"Activar feature manos");f.enabled=true;EditorUtility.SetDirty(f);AssetDatabase.SaveAssetIfDirty(f);
            }
        }
        Debug.Log("Features de manos revisadas. No se sustituyeron perfiles de mandos ni loaders. Los assets de configuración se guardan; la escena principal NO.");
    }
    static void RegistrarOverrides(UnityEngine.Object objeto)
    {
        EditorUtility.SetDirty(objeto);
        if(PrefabUtility.IsPartOfPrefabInstance(objeto))
            PrefabUtility.RecordPrefabInstancePropertyModifications(objeto);
    }
    void RepararReferencias()
    {
        if(!Resolver())return;
        var grupos=rig.GetComponentsInChildren<ConfiguracionManosRealesAula>(true)
            .Where(c=>c.transform.parent==rig.CameraFloorOffsetObject.transform).ToArray();
        var manager=rig.GetComponent<XRInputModalityManager>();
        if(grupos.Length!=1 || manager==null)
        {Debug.LogError("[MANOS-REALES] Se requiere exactamente un grupo integrado y el gestor del rig existente.");return;}
        var marker=grupos[0];
        if(marker.manoIzquierda==null || marker.manoDerecha==null ||
            !marker.manoIzquierda.transform.IsChildOf(marker.transform) || !marker.manoDerecha.transform.IsChildOf(marker.transform))
        {Debug.LogError("[MANOS-REALES] Las referencias del grupo integrado no son válidas. No se hicieron cambios.");return;}
        if(!Respaldar("ReparacionReferencias"))return;
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Reparar referencias de manos");
        try
        {
            Undo.RecordObject(manager,"Referencias persistentes de manos");
            manager.leftHand=marker.manoIzquierda;manager.rightHand=marker.manoDerecha;
            RegistrarOverrides(manager);
            Undo.RecordObject(marker,"Gestor existente");marker.gestorModalidad=manager;
            RegistrarOverrides(marker);
            ConfigurarTeclasSimuladas(false);
            Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
            Debug.Log("[MANOS-REALES] Referencias reparadas y registradas como overrides del prefab. No se regeneraron objetos ni se guardó la escena principal.");
            Diagnosticar();
        }
        catch(Exception e){Undo.RevertAllDownToGroup(group);Debug.LogError("[MANOS-REALES] Reparación revertida: "+e.Message);}
    }
    void Retirar()
    {
        if(!Resolver() || !Respaldar("Retirada"))return;
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Retirar ramas de manos");
        foreach(var c in rig.GetComponentsInChildren<ConfiguracionManosRealesAula>(true))
        {
            if(c.transform.parent!=rig.CameraFloorOffsetObject.transform)continue;
            Undo.RecordObject(c.gestorModalidad,"Restaurar referencias anteriores");
            c.gestorModalidad.leftHand=c.izquierdaAnterior;c.gestorModalidad.rightHand=c.derechaAnterior;
            RegistrarOverrides(c.gestorModalidad);
            Undo.DestroyObjectImmediate(c.gameObject);
        }
        if(File.Exists(ControlesAula))ConfigurarTeclasSimuladas(true);
        Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Debug.Log("Ramas de manos retiradas y mandos anteriores conservados. No se borran paquetes/samples ni se desactivan features compartidas.");
    }
    void Diagnosticar()
    {
        DiagnosticarSimulador();
        if(rig==null){var all=UnityEngine.Object.FindObjectsByType<XROrigin>();if(all.Length==1)rig=all[0];}
        if(rig==null)return;
        var m=rig.GetComponent<XRInputModalityManager>();
        Debug.Log("[MANOS-REALES] XR Hands="+(Paquete(Hands)?.version??"pendiente")+" | rigs activos="+UnityEngine.Object.FindObjectsByType<XROrigin>().Length+
            " | EventSystems="+UnityEngine.Object.FindObjectsByType<EventSystem>().Length+" | Left Hand="+m?.leftHand+" | Right Hand="+m?.rightHand+
            " | grupos de manos integrados="+rig.GetComponentsInChildren<ConfiguracionManosRealesAula>(true).Length);
        foreach(var c in rig.GetComponentsInChildren<ConfiguracionManosRealesAula>(true))
            Debug.Log("  Mano izq activa="+c.manoIzquierda.activeInHierarchy+", derecha="+c.manoDerecha.activeInHierarchy+
                " | Poke="+c.GetComponentsInChildren<XRPokeInteractor>(true).Length+" | Ray="+c.GetComponentsInChildren<XRRayInteractor>(true).Length+" | NearFar="+c.GetComponentsInChildren<NearFarInteractor>(true).Length,c);
    }
    static void DiagnosticarSimulador()
    {
        var informe=new System.Text.StringBuilder("[DIAG-ENTRADA-MANOS] Play="+Application.isPlaying+" | pausa="+EditorApplication.isPaused+" | teclado="+(Keyboard.current!=null));
        var simuladores=UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include);
        informe.AppendLine(" | simuladores="+simuladores.Length);
        foreach(var sim in simuladores)
        {
            informe.AppendLine("Simulador "+sim.name+": activo="+sim.isActiveAndEnabled+
                " | modo="+(sim.deviceLifecycleManager!=null?sim.deviceLifecycleManager.deviceMode.ToString():"SIN gestor")+
                " | manos detectadas izq="+sim.leftHandIsTracked+", der="+sim.rightHandIsTracked);
            InformeAccion(informe,"Alternar izquierda",sim.toggleManipulateLeftInput.inputActionReferencePerformed?.action);
            InformeAccion(informe,"Alternar derecha",sim.toggleManipulateRightInput.inputActionReferencePerformed?.action);
            var playback=sim.handPlaybackManager;
            if(playback==null){informe.AppendLine("FALTA Hand Playback Manager.");continue;}
            informe.AppendLine("Playback activo="+playback.isActiveAndEnabled);
            foreach(var pose in playback.simulatedHandExpressions)
            {
                if(pose==null)continue;
                InformeAccion(informe,"Pose "+pose.name,pose.toggleInput?.inputActionReferencePerformed?.action);
            }
            // Inspección serializada para evitar dependencias directas de tipos XR Hands.
            var lista=new SerializedObject(playback).FindProperty("m_SimulatedHandExpressions");
            if(lista!=null)for(int i=0;i<lista.arraySize;i++)
            {
                var item=lista.GetArrayElementAtIndex(i);
                var secuencia=item.FindPropertyRelative("m_CaptureSequence");
                informe.AppendLine("Grabación "+i+"="+(secuencia?.objectReferenceValue!=null?secuencia.objectReferenceValue.name:"FALTA"));
            }
        }
        informe.AppendLine("Si modo=Controller, las poses de manos se ignoran. Si habilitada=False, la acción no está recibiendo entrada. Este informe se emite solo al solicitarlo.");
        Debug.Log(informe.ToString());
    }
    static void InformeAccion(System.Text.StringBuilder informe,string etiqueta,InputAction accion)
    {
        if(accion==null){informe.AppendLine(etiqueta+": SIN referencia de acción");return;}
        string teclas=string.Join(", ",accion.bindings.Select(b=>b.effectivePath));
        informe.AppendLine(etiqueta+": "+accion.name+" | habilitada="+accion.enabled+
            " | controles resueltos="+accion.controls.Count+" | teclas="+teclas+
            " | asset="+AssetDatabase.GetAssetPath(accion.actionMap?.asset));
    }
}
