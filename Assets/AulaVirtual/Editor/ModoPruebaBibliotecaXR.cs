using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.UI;

// Solo Editor. No añade módulos, no modifica Input Actions ni forma parte del build Quest.
[InitializeOnLoad]
public static class ModoPruebaBibliotecaXR
{
    const string Key = "AulaVirtual.Biblioteca.MouseEditor";
    const string MouseMenu = "Aula Virtual/Biblioteca visual/Prueba Editor/Mouse";
    const string XRMenu = "Aula Virtual/Biblioteca visual/Prueba Editor/Mando XR (T)";
    static ModoPruebaBibliotecaXR()
    {
        EditorApplication.playModeStateChanged += state =>
        { if (state == PlayModeStateChange.EnteredPlayMode) Aplicar(); };
    }
    [MenuItem(MouseMenu)] static void Mouse() { EditorPrefs.SetBool(Key, true); Aplicar(); }
    [MenuItem(XRMenu)] static void XR() { EditorPrefs.SetBool(Key, false); Aplicar(); }
    [MenuItem(MouseMenu, true)] static bool MarcarMouse() { Menu.SetChecked(MouseMenu, EditorPrefs.GetBool(Key)); return true; }
    [MenuItem(XRMenu, true)] static bool MarcarXR() { Menu.SetChecked(XRMenu, !EditorPrefs.GetBool(Key)); return true; }

    static void Aplicar()
    {
        bool mouse = EditorPrefs.GetBool(Key);
        if (!Application.isPlaying)
        { Debug.Log("[BIBLIOTECA-ENTRADA] Modo " + (mouse ? "Mouse" : "XR") + " seleccionado para el próximo Play."); return; }
        var systems = Object.FindObjectsByType<EventSystem>();
        if (systems.Length != 1 || systems[0].GetComponents<BaseInputModule>().Length != 1 ||
            !(systems[0].GetComponent<BaseInputModule>() is XRUIInputModule module))
        { Debug.LogWarning("[BIBLIOTECA-ENTRADA] Se requiere el único XRUIInputModule ya preparado. No se crean componentes."); return; }
        // Exclusión de fuentes: un clic no llega por ratón directo y por gatillo simulado.
        module.enableMouseInput = mouse;
        module.enableXRInput = !mouse;
        module.enableBuiltinActionsAsFallback = true;
        foreach (var simulator in Object.FindObjectsByType<XRInteractionSimulator>()) simulator.enabled = !mouse;
        Debug.Log("[BIBLIOTECA-ENTRADA] " + (mouse ? "Mouse directo: clic izquierdo en Game." : "Mando simulado: apunta y pulsa/suelta T.") + " Cambios solo en Play.");
    }
}
