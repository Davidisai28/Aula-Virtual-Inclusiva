using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Solo imágenes: no crea VideoPlayer ni modifica la caché de videos.
public sealed class MiniaturaVideoAula : MonoBehaviour
{
    const int MaxBytes = 256 * 1024;
    static readonly HashSet<string> pendientes = new HashSet<string>();
    static int activas;
    RawImage imagen;
    TMP_Text marcador;
    VideoInfoDrive video;
    Texture2D textura;
    UnityWebRequest solicitud;
    string claveEnCurso;
    bool iniciada;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarCola() { pendientes.Clear(); activas = 0; }

    public static void PrepararFila(Transform fila)
    {
        // Ambas bibliotecas comparten la geometría 1472 x 216. No cambia botones ni eventos.
        Ajustar(fila.Find("NombreTMP"), 312, 18, 788, 116);
        Ajustar(fila.Find("TamanoTMP"), 312, 152, 210, 50);
        Ajustar(fila.Find("Insignia"), 548, 152, 552, 50);
        Ajustar(fila.Find("Insignia/TextoEstadoTMP"), 0, 0, 552, 50);
        Ajustar(fila.Find("EstadoTMP"), 312, 152, 788, 50);
        Ajustar(fila.Find("ProgresoDescarga"), 312, 204, 788, 4);
        if (fila.Find("Miniatura") != null) return;
        // Crear inactivo y con CanvasRenderer antes de activar gráficos/máscara.
        var go = new GameObject("Miniatura", typeof(RectTransform)); go.SetActive(false);
        go.transform.SetParent(fila, false); Ajustar(go.transform, 24, 32, 256, 144);
        go.AddComponent<CanvasRenderer>();
        var fondo = go.AddComponent<VidrioBibliotecaGraphic>();
        fondo.color = fondo.colorInferior = new Color(.055f, .09f, .13f, 1);
        fondo.radio = 16; fondo.grosorBorde = 0; fondo.raycastTarget = false;
        go.AddComponent<Mask>().showMaskGraphic = true;
        var raw = new GameObject("Imagen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        raw.transform.SetParent(go.transform, false); Ajustar(raw.transform, 0, 0, 256, 144);
        raw.GetComponent<RawImage>().raycastTarget = false; raw.SetActive(false);
        var label = new GameObject("SinMiniatura", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false); Ajustar(label.transform, 0, 0, 256, 144);
        var text = label.GetComponent<TMP_Text>();
        text.font = fila.Find("NombreTMP")?.GetComponent<TMP_Text>()?.font;
        text.text = "VIDEO"; text.fontSize = 26; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.66f,.74f,.8f); text.raycastTarget = false;
        go.AddComponent<MiniaturaVideoAula>(); go.SetActive(true);
    }
    static void Ajustar(Transform t, float x, float y, float w, float h)
    {
        if (!(t is RectTransform r)) return;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
    }
    public void Mostrar(VideoInfoDrive info)
    {
        Cancelar(); video = info;
        imagen = transform.Find("Imagen").GetComponent<RawImage>();
        marcador = transform.Find("SinMiniatura").GetComponent<TMP_Text>();
        if (isActiveAndEnabled) Iniciar();
    }
    void OnEnable() { if (Application.isPlaying && video != null) Iniciar(); }
    void Iniciar() { if (!iniciada && Application.isPlaying) { iniciada = true; StartCoroutine(Cargar()); } }
    static string Clave(string valor)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(valor))).Replace("-", "").ToLowerInvariant();
    }
    IEnumerator Cargar()
    {
        if (!ConfiguracionBackendAula.IntentarObtener(null, out var origen, out _)) yield break;
        string key = Clave(origen + "|" + video.id + "|" + video.modifiedTime);
        string carpeta = Path.Combine(Application.persistentDataPath, "MiniaturasAula");
        string ruta = Path.Combine(carpeta, key + ".img");
        if (Leer(ruta)) yield break;
        // Solo se aceptan rutas relativas del mismo backend y del video actual.
        string esperada = "/api/videos/" + Uri.EscapeDataString(video.id) + "/thumbnail";
        if (video.thumbnailPath != esperada) yield break; // Compatible con el servidor anterior.
        while (activas >= 2 || pendientes.Contains(key)) yield return null;
        if (Leer(ruta)) yield break;
        claveEnCurso = key; pendientes.Add(key); activas++;
        try
        {
            solicitud = UnityWebRequestTexture.GetTexture(origen + esperada, true);
            solicitud.timeout = 40; solicitud.redirectLimit = 0;
            yield return solicitud.SendWebRequest();
            if (solicitud.result != UnityWebRequest.Result.Success) yield break;
            byte[] bytes = solicitud.downloadHandler.data;
            var nueva = DownloadHandlerTexture.GetContent(solicitud);
            if (bytes == null || bytes.Length > MaxBytes || !Valida(nueva)) { if (nueva != null) Destroy(nueva); yield break; }
            Aplicar(nueva);
            Debug.Log("[MINIATURA-AULA] RED · " + origen + " · " + bytes.Length + " bytes", this);
            try
            {
                Directory.CreateDirectory(carpeta);
                File.WriteAllBytes(ruta, bytes);
                // Solo se purgan nuestras imágenes, nunca BibliotecaVideos.
                var files = new DirectoryInfo(carpeta).GetFiles("*.img");
                Array.Sort(files, (a,b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
                for (int i = 128; i < files.Length; i++) files[i].Delete();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { /* La imagen sigue disponible en memoria. */ }
        }
        finally { LiberarSolicitud(); }
    }
    bool Leer(string ruta)
    {
        try
        {
            if (!File.Exists(ruta) || new FileInfo(ruta).Length > MaxBytes) return false;
            var nueva = new Texture2D(2, 2);
            if (ImageConversion.LoadImage(nueva, File.ReadAllBytes(ruta), true) && Valida(nueva)) { Aplicar(nueva); return true; }
            Destroy(nueva);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is UnityException) { }
        return false;
    }
    static bool Valida(Texture2D t) => t != null && t.width > 0 && t.height > 0 && t.width <= 1024 && t.height <= 1024;
    void Aplicar(Texture2D nueva)
    {
        if (textura != null) Destroy(textura);
        textura = nueva; textura.filterMode = FilterMode.Bilinear; textura.wrapMode = TextureWrapMode.Clamp;
        imagen.texture = textura;
        float aspecto = (float)textura.width / textura.height, destino = 16f / 9f;
        imagen.uvRect = aspecto > destino ? new Rect((1-destino/aspecto)/2,0,destino/aspecto,1) : new Rect(0,(1-aspecto/destino)/2,1,aspecto/destino);
        imagen.gameObject.SetActive(true); marcador.gameObject.SetActive(false);
    }
    void LiberarSolicitud()
    {
        if (solicitud != null) { solicitud.Abort(); solicitud.Dispose(); solicitud = null; }
        if (claveEnCurso != null) { pendientes.Remove(claveEnCurso); activas = Mathf.Max(0,activas-1); claveEnCurso = null; }
    }
    void Cancelar()
    {
        StopAllCoroutines(); LiberarSolicitud(); iniciada = false;
        if (imagen != null) { imagen.texture = null; imagen.gameObject.SetActive(false); }
        if (marcador != null) marcador.gameObject.SetActive(true);
        if (textura != null) { Destroy(textura); textura = null; }
    }
    void OnDisable() => Cancelar();
}
