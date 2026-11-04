using System;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Aplicación explícita desde la ventana del generador. Nunca guarda la escena principal.
public static class PreparadorBibliotecaXR
{
    static T[] Componentes<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    public static void AsignarReferencias(GameObject pantalla, BibliotecaVisualAula ui)
    {
        ui.gestorCine = Componentes<GestorCineXR>(pantalla.scene).FirstOrDefault();
        var origin = Componentes<XROrigin>(pantalla.scene).FirstOrDefault(x => x.Camera != null);
        if (origin != null) ui.GetComponent<Canvas>().worldCamera = origin.Camera;
    }

    public static void Preparar(GameObject pantalla)
    {
        var systems = Componentes<EventSystem>(pantalla.scene);
        var libraries = pantalla.GetComponentsInChildren<BibliotecaVisualAula>(true)
            .Where(x => x.name == BibliotecaVisualAula.Id && x.transform.parent == pantalla.transform).ToArray();
        if (systems.Length != 1 || libraries.Length != 1 ||
            UnityEngine.Object.FindObjectsByType<EventSystem>().Length > 1)
        {
            Debug.LogError("[BIBLIOTECA-XR] Se requiere exactamente un EventSystem y una biblioteca. Genera primero la interfaz; no se ha cambiado ninguna interacción.");
            return;
        }
        var es = systems[0];
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Preparar biblioteca XR");
        // Retirar los módulos competidores ANTES de añadir XRUIInputModule.
        foreach (var module in es.GetComponents<BaseInputModule>())
            if (!(module is XRUIInputModule)) Undo.DestroyObjectImmediate(module);
        var xrModules = es.GetComponents<XRUIInputModule>();
        for (int i = 1; i < xrModules.Length; i++) Undo.DestroyObjectImmediate(xrModules[i]);
        var xr = es.GetComponent<XRUIInputModule>();
        if (xr == null) xr = Undo.AddComponent<XRUIInputModule>(es.gameObject);
        Undo.RecordObject(xr, "Configurar módulo XR");
        xr.enabled = true; xr.enableXRInput = true;
        // La prueba debe confirmar el mando simulado, no el ratón directo del módulo UI.
        xr.enableMouseInput = false;

        var ui = libraries[0];
        Undo.RecordObject(ui, "Referencias biblioteca");
        Undo.RecordObject(ui.GetComponent<Canvas>(), "Cámara biblioteca");
        AsignarReferencias(pantalla, ui);
        var raycaster = ui.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster == null) raycaster = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(ui.gameObject);
        Undo.RecordObject(raycaster, "Raycaster biblioteca");
        raycaster.enabled = true;
        raycaster.checkFor2DOcclusion = false;
        raycaster.checkFor3DOcclusion = true;

        // El collider permanece. Solo se retira la selección XR de la superficie.
        var surface = pantalla.GetComponent<XRSimpleInteractable>();
        if (surface != null)
        {
            Undo.RecordObject(surface, "Separar selección del pizarrón y cine");
            for (int i = surface.selectEntered.GetPersistentEventCount() - 1; i >= 0; i--)
                if (surface.selectEntered.GetPersistentTarget(i) is GestorCineXR &&
                    surface.selectEntered.GetPersistentMethodName(i) == "EntrarAlCine")
                    UnityEventTools.RemovePersistentListener(surface.selectEntered, i);
            surface.enabled = false;
            EditorUtility.SetDirty(surface);
        }

        foreach (var interactor in Componentes<NearFarInteractor>(pantalla.scene))
        {
            Undo.RecordObject(interactor, "Preparar UI Near Far");
            interactor.enableUIInteraction = true;
            CompletarPress(interactor, interactor.uiPressInput);
            EditorUtility.SetDirty(interactor);
        }
        // Los rayos de teletransporte traen UI desactivada: no cambiar su función.
        foreach (var interactor in Componentes<XRRayInteractor>(pantalla.scene).Where(x => x.enableUIInteraction))
        {
            Undo.RecordObject(interactor, "Revisar UI Ray");
            CompletarPress(interactor, interactor.uiPressInput);
            EditorUtility.SetDirty(interactor);
        }
        Undo.FlushUndoRecordObjects();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(pantalla.scene);
        Diagnosticar(pantalla);
        Debug.Log("[BIBLIOTECA-XR] Preparación aplicada con Undo. Escena principal SIN guardar. Cine tiene botón independiente; modos de prueba en Prueba Editor.", ui);
    }

    static void CompletarPress(XRBaseInteractor interactor, XRInputButtonReader reader)
    {
        // Respetar acciones embebidas y fuentes personalizadas existentes.
        if (reader.inputSourceMode != XRInputButtonReader.InputSourceMode.InputActionReference) return;
        var hand = interactor.handedness.ToString();
        if (hand != "Left" && hand != "Right")
        { Debug.LogWarning("[BIBLIOTECA-XR] Revisa Handedness y UI Press en " + interactor.name, interactor); return; }
        var references = AssetDatabase.LoadAllAssetsAtPath("Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/XRI Default Input Actions.inputactions")
            .OfType<InputActionReference>().ToArray();
        InputActionReference Find(string name) => references.FirstOrDefault(r => r.action != null &&
            r.action.name == name && r.action.actionMap.name == "XRI " + hand + " Interaction");
        if (reader.inputActionReferencePerformed == null) reader.inputActionReferencePerformed = Find("UI Press");
        if (reader.inputActionReferenceValue == null) reader.inputActionReferenceValue = Find("UI Press Value");
    }

    public static void Diagnosticar(GameObject pantalla)
    {
        foreach (var es in Componentes<EventSystem>(pantalla.scene))
            Debug.Log("[BIBLIOTECA-XR] EventSystem " + es.name + ": " + string.Join(", ",
                es.GetComponents<BaseInputModule>().Select(m => m.GetType().Name + " enabled=" + m.enabled)), es);
        foreach (var i in Componentes<NearFarInteractor>(pantalla.scene))
            ReportarPress(i, i.enableUIInteraction, i.uiPressInput);
        foreach (var i in Componentes<XRRayInteractor>(pantalla.scene).Where(i => i.enableUIInteraction))
            ReportarPress(i, i.enableUIInteraction, i.uiPressInput);
        foreach (var ui in pantalla.GetComponentsInChildren<BibliotecaVisualAula>(true))
        {
            var c = ui.GetComponent<Canvas>();
            var scale = ui.transform.lossyScale;
            Debug.Log($"[BIBLIOTECA-XR] Canvas {c.renderMode}, cámara={c.worldCamera}, escala mundial={scale.ToString("F6")}, raycaster={ui.GetComponent<TrackedDeviceGraphicRaycaster>() != null}", ui);
            foreach (var text in ui.GetComponentsInChildren<TMP_Text>(true).Where(t => t.gameObject.activeInHierarchy && t.isTextOverflowing))
                Debug.LogWarning("[BIBLIOTECA-XR] Texto desbordado: " + text.name, text);
        }
    }

    static void ReportarPress(XRBaseInteractor interactor, bool enabled, XRInputButtonReader reader)
    {
        var a = reader.inputActionReferencePerformed != null ? reader.inputActionReferencePerformed.action : reader.inputActionPerformed;
        Debug.Log($"[BIBLIOTECA-XR] {interactor.name} ({interactor.handedness}): UI Interaction={enabled}; fuente={reader.inputSourceMode}; UI Press={a?.actionMap?.name}/{a?.name}; acción enabled={a?.enabled}; interactor activo={interactor.isActiveAndEnabled}", interactor);
        if (reader.inputSourceMode == XRInputButtonReader.InputSourceMode.InputActionReference &&
            (reader.inputActionReferencePerformed == null || (Application.isPlaying && a != null && !a.enabled)))
            Debug.LogWarning("[BIBLIOTECA-XR] UI Press falta o no está habilitada en Play. Revisa Input Action Manager del rig y XRI Default Input Actions.", interactor);
    }
}
