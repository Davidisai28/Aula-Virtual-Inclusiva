using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FilaCatalogoVideoAula : MonoBehaviour
{
    VideoInfoDrive video;
    System.Action<VideoInfoDrive> seleccionar;
    Button boton;
    public void Mostrar(VideoInfoDrive video, bool cacheComprobada, System.Action<VideoInfoDrive> accion = null)
    {
        this.video = video; seleccionar = accion;
        Texto("NombreTMP", NombreVisible(video.name));
        Texto("EstadoTMP", FormatearTamano(video.size) + (cacheComprobada ? " · Caché verificada" : " · Disponibilidad sin comprobar"));
        Texto("IconoTMP", "VID");
        Texto("AccionVideo/TextoTMP", cacheComprobada ? "Reproducir" : "Descargar");
        boton = transform.Find("AccionVideo")?.GetComponent<Button>();
        if (boton != null)
        {
            boton.onClick.RemoveListener(Seleccionar);
            boton.onClick.AddListener(Seleccionar);
            boton.interactable = seleccionar != null;
        }
        var progress = transform.Find("ProgresoDescarga");
        if (progress != null) progress.gameObject.SetActive(false);
        PresentarInsignia(cacheComprobada, false);
        var waves = transform.Find("MiniaturaOndas")?.GetComponent<DecoracionMediatecaGraphic>();
        if (waves != null)
        {
            int variant = 0; foreach (char ch in video.id) variant = (variant + ch) % 7;
            waves.variante = variant; waves.SetVerticesDirty();
        }
    }

    void Seleccionar() => seleccionar?.Invoke(video);

    public void Actualizar(GestorCacheVideos gestor)
    {
        if (video == null || gestor == null) return;
        bool selected = gestor.VideoActual != null && gestor.VideoActual.id == video.id;
        bool error = selected && gestor.Estado == GestorCacheVideos.EstadoVideo.Error;
        bool downloading = selected && gestor.Estado == GestorCacheVideos.EstadoVideo.Descargando;
        bool cache = gestor.EstaEnCache(video);
        if (boton != null) boton.interactable = !gestor.Ocupado && seleccionar != null;
        Texto("AccionVideo/TextoTMP", downloading ? Mathf.FloorToInt(gestor.Progreso * 100) + " %" :
            error ? "Reintentar" : cache ? "Reproducir" : "Descargar");
        Texto("EstadoTMP", error ? "Error · Puedes reintentar" : selected && gestor.Ocupado ? gestor.Mensaje :
            FormatearTamano(video.size) + (cache ? " · En caché" : " · Disponible para descargar"));
        PresentarInsignia(cache, error || (selected && gestor.Ocupado));
        var progress = transform.Find("ProgresoDescarga");
        if (progress != null)
        {
            progress.gameObject.SetActive(downloading);
            var fill = progress.Find("Relleno") as RectTransform;
            if (fill != null) fill.sizeDelta = new Vector2(((RectTransform)progress).rect.width * gestor.Progreso, fill.sizeDelta.y);
        }
    }

    void PresentarInsignia(bool cache, bool estadoEspecial)
    {
        // Mantiene compatibilidad con filas generadas con el diseño anterior.
        var size = transform.Find("TamanoTMP");
        if (size == null) return;
        Texto("TamanoTMP", FormatearTamano(video.size));
        size.gameObject.SetActive(!estadoEspecial);
        var badge = transform.Find("Insignia");
        if (badge != null)
        {
            badge.gameObject.SetActive(!estadoEspecial);
            Texto("Insignia/TextoEstadoTMP", cache ? "En caché" : "Disponible");
            Color tint = cache ? new Color(.655f,.953f,.816f) : new Color(.682f,.733f,.788f);
            var label = badge.Find("TextoEstadoTMP")?.GetComponent<TMP_Text>();
            if (label != null) label.color = tint;
            var glass = badge.GetComponent<VidrioBibliotecaGraphic>();
            if (glass != null) { glass.colorBorde = tint * new Color(1,1,1,.65f); glass.SetVerticesDirty(); }
        }
        var status = transform.Find("EstadoTMP");
        if (status != null) status.gameObject.SetActive(estadoEspecial);
        var icon = transform.Find("AccionVideo/IconoAccion")?.GetComponent<DecoracionMediatecaGraphic>();
        if (icon != null)
        { icon.forma = cache ? DecoracionMediatecaGraphic.Forma.Play : DecoracionMediatecaGraphic.Forma.Descargar; icon.SetVerticesDirty(); }
    }

    void OnDestroy() { if (boton != null) boton.onClick.RemoveListener(Seleccionar); }

    public static string NombreVisible(string original)
    {
        if (string.IsNullOrWhiteSpace(original)) return "Video sin título";
        string visible = original.Trim().Replace('\n', ' ').Replace('\r', ' ');
        if (visible.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase))
            visible = visible.Substring(0, visible.Length - 4).TrimEnd();
        return string.IsNullOrWhiteSpace(visible) ? "Video sin título" : visible;
    }

    void Texto(string path, string value)
    {
        var text = transform.Find(path)?.GetComponent<TMP_Text>();
        if (text == null) return;
        text.richText = false; // Los nombres de Drive son datos, no etiquetas TMP.
        text.text = value;
    }

    public static string FormatearTamano(string size)
    {
        if (!long.TryParse(size, out long bytes) || bytes < 0) return "Tamaño no disponible";
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024L * 1024) return (bytes / 1024d).ToString("0.#") + " KiB";
        if (bytes < 1024L * 1024 * 1024) return (bytes / (1024d * 1024)).ToString("0.#") + " MiB";
        return (bytes / (1024d * 1024 * 1024)).ToString("0.#") + " GiB";
    }
}
