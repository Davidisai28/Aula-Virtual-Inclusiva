using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Unity.XR.CoreUtils;

// Validación explícita batch. Abre la escena para inspeccionarla, nunca la guarda.
public static class VerificarCopiaPreparacion
{
    [Serializable] class Informe
    {
        public bool ok;
        public string escena;
        public int scriptsFaltantes, graficosSinRenderer, rigsActivos, eventSystemsActivos, reproductores;
        public string[] graficosSinRendererRutas;
        public string renderTexture;
        public bool catalogo, cache, cine, asientos;
        public string alcance = "Inspeccion estructural en Editor; no Play Mode, no validacion visual ni Quest";
    }
    public static void Ejecutar()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Esta verificacion es exclusiva del proceso batch de preparacion.");
        var escena = EditorSceneManager.OpenScene("Assets/AulaVirtual/Scenes/AulaPrincipal_GLB.unity", OpenSceneMode.Single);
        var roots = escena.GetRootGameObjects();
        var objects = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        var players = roots.SelectMany(r => r.GetComponentsInChildren<VideoPlayer>(true)).ToArray();
        // TextMeshPro 3D hereda de Graphic pero usa MeshRenderer, no CanvasRenderer.
        var invalidos = roots.SelectMany(r => r.GetComponentsInChildren<Graphic>(true))
            .Where(g => !(g is TMPro.TextMeshPro) && g.GetComponent<CanvasRenderer>() == null).ToArray();
        var report = new Informe {
            escena = escena.path,
            scriptsFaltantes = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
            graficosSinRenderer = invalidos.Length,
            graficosSinRendererRutas = invalidos.Select(g => AnimationUtility.CalculateTransformPath(g.transform, null) + " [" + g.GetType().Name + "]").ToArray(),
            rigsActivos = UnityEngine.Object.FindObjectsByType<XROrigin>().Length,
            eventSystemsActivos = UnityEngine.Object.FindObjectsByType<EventSystem>().Length,
            reproductores = players.Length,
            renderTexture = players.FirstOrDefault(p => p.targetTexture != null)?.targetTexture.name,
            catalogo = objects.Any(g => g.GetComponent<CatalogoVideosDrive>() != null),
            cache = objects.Any(g => g.GetComponent<GestorCacheVideos>() != null),
            cine = objects.Any(g => g.GetComponent<GestorCineXR>() != null),
            asientos = objects.Any(g => g.GetComponent<PruebaAsientoXR>() != null)
        };
        report.ok = report.scriptsFaltantes == 0 && report.graficosSinRenderer == 0 && report.rigsActivos == 1 &&
            report.eventSystemsActivos == 1 && report.reproductores > 0 && report.renderTexture == "RT_VideoAula" &&
            report.catalogo && report.cache && report.cine && report.asientos;
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/validacion-escena-unity.json"));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("[PREPARACION-GITHUB] " + JsonUtility.ToJson(report));
        if (!report.ok) throw new InvalidOperationException("La inspeccion estructural requiere revision. No se guardo la escena.");
    }
}
