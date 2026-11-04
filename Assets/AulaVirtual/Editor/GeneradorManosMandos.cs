using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class GeneradorManosMandos : EditorWindow
{
    XROrigin rig;
    Transform izquierda,derecha,visualIzquierdo,visualDerecho;
    Material material;
    Color tonoPiel=new Color(.84f,.65f,.53f);
    const string Base="Assets/AulaVirtual/Models/ControllerHands/";
    [MenuItem("Aula Virtual/Manos virtuales con mandos (etapa 1)")]
    static void Abrir()=>GetWindow<GeneradorManosMandos>("Manos con mandos");
    void OnGUI()
    {
        EditorGUILayout.HelpBox("Solo representación visual. No instala paquetes ni cambia acciones, rig, interactores o rayos. Las manos se muestran en Play al detectar mandos seguidos. Capsense SDK y contacto directo quedan fuera de esta etapa.",MessageType.Info);
        rig=(XROrigin)EditorGUILayout.ObjectField("XR Origin existente",rig,typeof(XROrigin),true);
        izquierda=(Transform)EditorGUILayout.ObjectField("Left Controller",izquierda,typeof(Transform),true);
        derecha=(Transform)EditorGUILayout.ObjectField("Right Controller",derecha,typeof(Transform),true);
        visualIzquierdo=(Transform)EditorGUILayout.ObjectField("Modelo mando izquierdo",visualIzquierdo,typeof(Transform),true);
        visualDerecho=(Transform)EditorGUILayout.ObjectField("Modelo mando derecho",visualDerecho,typeof(Transform),true);
        material=(Material)EditorGUILayout.ObjectField("Material manos (opcional)",material,typeof(Material),false);
        tonoPiel=EditorGUILayout.ColorField("Tono de piel",tonoPiel);
        if(GUILayout.Button("Aplicar tono al material (sin regenerar)",GUILayout.Height(28))) AplicarTono();
        if(GUILayout.Button("Aplicar / actualizar manos",GUILayout.Height(34))) Aplicar();
        if(GUILayout.Button("Validar manos aplicadas",GUILayout.Height(28)))
            if(Resolver()) { Validar(izquierda);Validar(derecha); }
        if(GUILayout.Button("Quitar manos y restaurar mandos",GUILayout.Height(28))) Quitar();
    }
    void AplicarTono()
    {
        if(Application.isPlaying) {Debug.LogWarning("Detén Play para guardar el tono de piel.");return;}
        var target=material!=null?material:AssetDatabase.LoadAssetAtPath<Material>("Assets/AulaVirtual/Materials/MAT_ManosMandos.mat");
        if(target==null || !target.HasProperty("_BaseColor")) {Debug.LogError("Asigna el material URP de las manos.");return;}
        string path=AssetDatabase.GetAssetPath(target);
        if(!path.StartsWith("Assets/")) {Debug.LogError("Selecciona un material propio del proyecto.");return;}
        string folder="Backups/MaterialManos";Directory.CreateDirectory(folder);
        File.Copy(path,folder+"/"+target.name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".mat",false);
        Undo.RecordObject(target,"Cambiar tono de piel");
        target.SetColor("_BaseColor",tonoPiel);
        if(target.HasProperty("_Color")) target.SetColor("_Color",tonoPiel);
        target.SetFloat("_Smoothness",.25f);
        EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
        Debug.Log("[MANOS-MANDO] Tono de piel guardado; sin regenerar manos ni cambiar su posición.",target);
    }
    bool Resolver()
    {
        if(Application.isPlaying) {Debug.LogWarning("Detén Play para aplicar o quitar las manos.");return false;}
        if(rig==null)
        {
            var all=UnityEngine.Object.FindObjectsByType<XROrigin>(); if(all.Length==1)rig=all[0];
        }
        if(rig==null || !rig.gameObject.scene.IsValid() || EditorUtility.IsPersistent(rig) || rig.CameraFloorOffsetObject==null)
        {Debug.LogError("Asigna el XR Origin de la escena, con Camera Floor Offset válido.");return false;}
        if(izquierda==null) izquierda=rig.CameraFloorOffsetObject.transform.Find("Left Controller");
        if(derecha==null) derecha=rig.CameraFloorOffsetObject.transform.Find("Right Controller");
        if(izquierda==null || derecha==null || izquierda==derecha || !izquierda.IsChildOf(rig.transform) || !derecha.IsChildOf(rig.transform))
        {Debug.LogError("Revisa los dos nodos de seguimiento de mandos del rig existente.");return false;}
        if(visualIzquierdo==null) visualIzquierdo=izquierda.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Left Controller Visual");
        if(visualDerecho==null) visualDerecho=derecha.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Right Controller Visual");
        return true;
    }
    static Renderer[] Renderers(Transform visual)=>visual==null?Array.Empty<Renderer>():visual.GetComponentsInChildren<Renderer>(true)
        .Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
    bool Respaldar()
    {
        if(string.IsNullOrEmpty(rig.gameObject.scene.path)){Debug.LogError("Guarda la escena inicial antes de respaldarla.");return false;}
        const string folder="Assets/AulaVirtual/Scenes/BackupsManosMandos";Directory.CreateDirectory(folder);
        string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+rig.gameObject.scene.name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity");
        bool dirty=rig.gameObject.scene.isDirty;
        bool ok=EditorSceneManager.SaveScene(rig.gameObject.scene,path,true);
        if(dirty)EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        if(!ok)Debug.LogError("Respaldo fallido. Operación cancelada.");else Debug.Log("Respaldo previo: "+path);
        return ok;
    }
    static ManoVirtualConMando[] Propios(Transform node)=>node.GetComponentsInChildren<ManoVirtualConMando>(true)
        .Where(h=>h.transform.parent==node && h.name==ManoVirtualConMando.Id).ToArray();
    static void Borrar(Transform node)
    {foreach(var hand in Propios(node))Undo.DestroyObjectImmediate(hand.gameObject);}
    void Quitar()
    {
        if(!Resolver() || !Respaldar())return;
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Quitar manos visuales");
        Borrar(izquierda);Borrar(derecha);Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Debug.Log("Manos quitadas. Los modelos e interacciones originales permanecen intactos. Escena sin guardar.");
    }
    void Aplicar()
    {
        if(!Resolver())return;
        if(visualIzquierdo==null || visualDerecho==null || visualIzquierdo==izquierda || visualDerecho==derecha ||
            !visualIzquierdo.IsChildOf(izquierda) || !visualDerecho.IsChildOf(derecha) ||
            visualIzquierdo.GetComponentsInChildren<XRBaseInteractor>(true).Length>0 || visualDerecho.GetComponentsInChildren<XRBaseInteractor>(true).Length>0 ||
            Renderers(visualIzquierdo).Length==0 || Renderers(visualDerecho).Length==0)
        {Debug.LogError("Asigna las raíces visuales de los mandos originales. No se ocultarán renderers del rig completo.");return;}
        if(!Respaldar())return;
        // Solo importación de estos modelos locales; no se importa la escena Hands Demo ni sus scripts.
        AssetDatabase.ImportAsset(Base+"LeftHand.fbx",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(Base+"RightHand.fbx",ImportAssetOptions.ForceUpdate);
        var l=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"LeftHand.fbx");
        var r=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"RightHand.fbx");
        if(!Valido(l,"L_") || !Valido(r,"R_")){Debug.LogError("Los modelos no tienen el esqueleto esperado; se conservan las manos anteriores.");return;}
        if(material==null)
        {
            const string path="Assets/AulaVirtual/Materials/MAT_ManosMandos.mat";
            material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Lit");
                if(shader==null){Debug.LogError("No se encontró URP/Lit.");return;}
                material=new Material(shader){name="MAT_ManosMandos"};
                material.SetColor("_BaseColor",tonoPiel);material.SetFloat("_Smoothness",.25f);
                AssetDatabase.CreateAsset(material,path);
            }
        }
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Aplicar manos con mandos");
        Crear(izquierda,visualIzquierdo,l,true);Crear(derecha,visualDerecho,r,false);
        Validar(izquierda);Validar(derecha);
        Undo.CollapseUndoOperations(group);EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Debug.Log("[MANOS-MANDO] Dos manos visuales preparadas. En Editor siguen visibles los mandos; en Play las manos los sustituyen si hay tracking. Calibra offsets visuales antes de validar en Quest. Escena SIN guardar.");
    }
    static void Validar(Transform node)
    {
        var own=Propios(node);
        if(own.Length!=1){Debug.LogError("Se esperaba una sola mano visual en "+node.name);return;}
        var h=own[0];
        bool valid=h.articulaciones!=null && h.articulaciones.Length==15 && h.articulaciones.All(j=>j.hueso!=null) &&
            h.modelosMano!=null && h.modelosMano.Any(r=>r is SkinnedMeshRenderer) && h.modelosMando!=null && h.modelosMando.Length>0 &&
            h.modelosMando.All(r=>r!=null && !(r is LineRenderer) && !r.transform.IsChildOf(h.transform)) &&
            h.GetComponentsInChildren<XRBaseInteractor>(true).Length==0 &&
            h.GetComponentsInChildren<UnityEngine.InputSystem.XR.TrackedPoseDriver>(true).Length==0;
        if(!valid) Debug.LogError("[MANOS-MANDO] Revisa huesos, modelos y componentes de "+node.name,h);
        else Debug.Log("[MANOS-MANDO] "+node.name+": una mano, 15 articulaciones visuales; ningún interactor o driver añadido.",h);
    }
    static string[] Nombres(string prefix)=>new[]{"ThumbMetacarpal","ThumbProximal","ThumbDistal","IndexProximal","IndexIntermediate","IndexDistal",
        "MiddleProximal","MiddleIntermediate","MiddleDistal","RingProximal","RingIntermediate","RingDistal","LittleProximal","LittleIntermediate","LittleDistal"}.Select(n=>prefix+n).ToArray();
    static bool Valido(GameObject model,string prefix)
    {
        if(model==null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length==0)return false;
        var names=model.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToHashSet();
        return names.Contains(prefix+"Wrist") && Nombres(prefix).All(n=>names.Contains(n));
    }
    void Crear(Transform node,Transform original,GameObject prefab,bool left)
    {
        var old=Propios(node).FirstOrDefault();
        Vector3 offset=old!=null?old.desplazamiento:new Vector3(0,-.015f,-.055f), rotation=old!=null?old.orientacion:Vector3.zero;
        float scale=old!=null?old.escala:1, smooth=old!=null?old.suavizado:14;
        Borrar(node);
        var root=new GameObject(ManoVirtualConMando.Id);root.SetActive(false);root.transform.SetParent(node,false);
        root.transform.localPosition=offset;root.transform.localRotation=Quaternion.Euler(rotation);root.transform.localScale=Vector3.one*scale;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
        var bones=visual.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        foreach(var animator in visual.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
        string prefix=left?"L_":"R_";var wrist=bones[prefix+"Wrist"];
        // Alinear el origen del esqueleto al anclaje visual del mando, sin mover su nodo de tracking.
        visual.transform.rotation=root.transform.rotation*Quaternion.Inverse(wrist.rotation)*visual.transform.rotation;
        visual.transform.position+=root.transform.position-wrist.position;
        var hand=root.AddComponent<ManoVirtualConMando>();hand.izquierda=left;hand.modelosMando=Renderers(original);
        hand.modelosMano=visual.GetComponentsInChildren<Renderer>(true);
        foreach(var renderer in hand.modelosMano)
        {
            renderer.sharedMaterials=Enumerable.Repeat(material,Mathf.Max(1,renderer.sharedMaterials.Length)).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.forceRenderingOff=true;
        }
        var joints=new List<ManoVirtualConMando.Articulacion>();int i=0;
        foreach(string name in Nombres(prefix))
        {
            var bone=bones[name];bool thumb=i<3,index=i>=3&&i<6;
            float[] degrees=thumb?new[]{15f,25f,20f}:index?new[]{55f,65f,35f}:new[]{45f,50f,30f};
            joints.Add(new ManoVirtualConMando.Articulacion{hueso=bone,reposo=bone.localRotation,eje=Vector3.right,
                grados=degrees[i%3],grupo=thumb?ManoVirtualConMando.Grupo.Pulgar:index?ManoVirtualConMando.Grupo.Indice:ManoVirtualConMando.Grupo.Agarre});i++;
        }
        hand.articulaciones=joints.ToArray();hand.desplazamiento=offset;hand.orientacion=rotation;hand.escala=scale;hand.suavizado=smooth;
        Undo.RegisterCreatedObjectUndo(root,"Crear mano visual");Undo.RecordObject(root,"Activar adaptador visual");root.SetActive(true);Undo.FlushUndoRecordObjects();
    }
}
