using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Prueba explícita de la fila real del generador, en una escena temporal separada.
public static class VerificarMiniaturasAula
{
    [MenuItem("Aula Virtual/Biblioteca visual/Validar miniaturas sin cambiar escena")]
    public static void Ejecutar()
    {
        if (Application.isPlaying) { Debug.LogWarning("Detén Play antes de validar miniaturas."); return; }
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("PruebaMiniaturas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
            var factory = typeof(GeneradorBibliotecaVideoXR).GetMethod("Fila", BindingFlags.Static | BindingFlags.NonPublic);
            var row = (RectTransform)factory.Invoke(null, new object[] { root.transform, "FilaPrueba",
                "Un título largo y legible para comprobar la biblioteca", "Disponible", "Reproducir", true, false, TMP_Settings.defaultFontAsset });
            int total = row.GetComponentsInChildren<Transform>(true).Length;
            MiniaturaVideoAula.PrepararFila(row);
            Exigir(total == row.GetComponentsInChildren<Transform>(true).Length, "PrepararFila duplicó objetos");
            foreach (var graphic in row.GetComponentsInChildren<Graphic>(true))
                Exigir(graphic.GetComponent<CanvasRenderer>() != null, "Falta CanvasRenderer: " + graphic.name);
            var image = row.Find("Miniatura/Imagen").GetComponent<RawImage>();
            Exigir(!image.raycastTarget, "La miniatura bloquea XR");
            var title = row.Find("NombreTMP").GetComponent<TMP_Text>();
            Exigir(title.fontSize >= 48 && title.maxVisibleLines == 2, "Título pequeño o sin límite de líneas");
            var mini = (RectTransform)row.Find("Miniatura");
            var button = (RectTransform)row.Find("AccionVideo");
            Exigir(mini.anchoredPosition.x + mini.rect.width < title.rectTransform.anchoredPosition.x, "Miniatura sobre título");
            Exigir(title.rectTransform.anchoredPosition.x + title.rectTransform.rect.width < button.anchoredPosition.x, "Título sobre botón");
            Exigir(button.rect.width >= 300 && button.rect.height >= 100, "Se redujo el botón XR");
            Exigir(PaginacionBiblioteca.PorPagina == 2 && PaginacionBiblioteca.TotalPaginas(5) == 3, "Paginación incorrecta");
            foreach (var rect in row.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == row) continue;
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = row.InverseTransformPoint(corner);
                    Exigir(local.x >= -.1f && local.x <= 1472.1f && local.y <= .1f && local.y >= -216.1f,
                        "Elemento fuera de fila: " + rect.name);
                }
            }
            Debug.Log("[MINIATURAS-TEST] OK: fila real, CanvasRenderer completos, sin duplicados ni solapamiento de regiones; botón 300x100, título 48 y dos videos por página. No es una prueba visual ni de mandos físicos.");
        }
        finally
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.DestroyImmediate(root); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    static void Exigir(bool cumple, string mensaje) { if (!cumple) throw new InvalidOperationException(mensaje); }
}
