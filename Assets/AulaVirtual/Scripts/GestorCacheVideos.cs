using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class GestorCacheVideos : MonoBehaviour
{
    public enum EstadoVideo { Disponible, Descargando, Preparando, Reproduciendo, Error }
    public VideoPlayer reproductor;
    public string servidor = "http://127.0.0.1:3000";
    public event Action EstadoCambiado;
    public EstadoVideo Estado { get; private set; }
    public VideoInfoDrive VideoActual { get; private set; }
    public float Progreso { get; private set; }
    public string Mensaje { get; private set; }
    public bool Ocupado => Estado == EstadoVideo.Descargando || Estado == EstadoVideo.Preparando;
    UnityWebRequest solicitud;
    string temporal;
    string urlPreparada;
    int operacionActual;
    string Carpeta => Path.Combine(Application.persistentDataPath, "BibliotecaVideos");

    void Awake()
    {
        if (reproductor == null) return;
        reproductor.prepareCompleted += Preparado;
        reproductor.errorReceived += ErrorVideo;
    }

    static bool Valido(VideoInfoDrive v) => v != null && !string.IsNullOrWhiteSpace(v.id) &&
        v.id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !v.id.Contains("/") && !v.id.Contains("\\") &&
        long.TryParse(v.size, out long bytes) && bytes > 0;

    string RutaVersion(VideoInfoDrive v)
    {
        using (var sha = SHA256.Create())
        {
            string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(v.size + "|" + v.modifiedTime))).Replace("-", "").Substring(0, 20);
            return Path.Combine(Carpeta, v.id + "_" + hash + ".mp4");
        }
    }

    string BuscarCache(VideoInfoDrive v)
    {
        if (!Valido(v)) return null;
        try
        {
            long bytes = long.Parse(v.size);
            string version = RutaVersion(v);
            if (File.Exists(version) && new FileInfo(version).Length == bytes) return version;
            // Compatibilidad con descargas de la versión anterior. Tras guardar una
            // revisión conocida, no volver a aceptar la copia legacy de fecha desconocida.
            string legacy = Path.Combine(Carpeta, v.id + ".mp4");
            if (!File.Exists(Path.Combine(Carpeta, v.id + ".versionada")) &&
                File.Exists(legacy) && new FileInfo(legacy).Length == bytes) return legacy;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
        return null;
    }

    public bool EstaEnCache(VideoInfoDrive video) => BuscarCache(video) != null;

    void EstadoNuevo(EstadoVideo estado, string mensaje)
    {
        Estado = estado; Mensaje = mensaje; EstadoCambiado?.Invoke();
    }

    public void ReproducirVideoDelCatalogo(VideoInfoDrive video)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Ocupado) return;
        if (!Valido(video) || reproductor == null)
        { EstadoNuevo(EstadoVideo.Error, "Video inválido o reproductor sin asignar"); return; }
        // Copia inmutable para la operación: un refresco de catálogo no cambia sus datos.
        VideoActual = new VideoInfoDrive { id = video.id, name = video.name, size = video.size,
            mimeType = video.mimeType, modifiedTime = video.modifiedTime };
        urlPreparada = null;
        Progreso = 0;
        EstadoNuevo(EstadoVideo.Preparando, "Comprobando caché…");
        StartCoroutine(DescargarYReproducir(VideoActual, ++operacionActual));
    }

    IEnumerator DescargarYReproducir(VideoInfoDrive video, int operacion)
    {
        string ruta = BuscarCache(video);
        if (ruta == null)
        {
            if (!ConfiguracionBackendAula.IntentarObtener(servidor, out string origen, out string errorConfiguracion))
            { Fallar(errorConfiguracion); yield break; }
            string error = null;
            try
            {
                Directory.CreateDirectory(Carpeta);
                temporal = Path.Combine(Carpeta, video.id + "_" + Guid.NewGuid().ToString("N") + ".part");
                solicitud = new UnityWebRequest(origen + "/api/videos/" + Uri.EscapeDataString(video.id) + "/download", "GET");
                solicitud.downloadHandler = new DownloadHandlerFile(temporal) { removeFileOnAbort = true };
                solicitud.timeout = 600;
                solicitud.SendWebRequest();
            }
            catch (Exception e) { error = e.Message; }
            if (error != null) { Fallar(error); LiberarDescarga(); yield break; }
            EstadoNuevo(EstadoVideo.Descargando, "Descargando…");
            float siguiente = 0;
            while (!solicitud.isDone)
            {
                if (Time.unscaledTime >= siguiente)
                {
                    Progreso = Mathf.Clamp01((float)((double)solicitud.downloadedBytes / long.Parse(video.size)));
                    EstadoCambiado?.Invoke(); siguiente = Time.unscaledTime + .1f;
                }
                yield return null;
            }
            if (solicitud.result != UnityWebRequest.Result.Success)
            { error = solicitud.error; LiberarDescarga(); Fallar("Descarga fallida: " + error); yield break; }
            solicitud.Dispose(); solicitud = null;
            try
            {
                if (!File.Exists(temporal) || new FileInfo(temporal).Length != long.Parse(video.size))
                    throw new IOException("El archivo recibido no coincide con el tamaño del catálogo. Actualiza e intenta de nuevo.");
                ruta = RutaVersion(video);
                // Nunca borrar archivos finales previos, incluso si quedaron incompletos.
                if (File.Exists(ruta)) File.Move(ruta, ruta + ".preservado_" + Guid.NewGuid().ToString("N"));
                File.Move(temporal, ruta); temporal = null;
                File.WriteAllText(Path.Combine(Carpeta, video.id + ".versionada"), video.modifiedTime ?? "");
                Debug.Log("[CACHE-VIDEO] Descarga guardada: " + video.name, this);
            }
            catch (Exception e) { error = e.Message; }
            if (error != null) { LiberarDescarga(); Fallar(error); yield break; }
        }
        else Debug.Log("[CACHE-VIDEO] Usando caché, sin descargar: " + video.name, this);

        Progreso = 1;
        EstadoNuevo(EstadoVideo.Preparando, "Preparando video…");
        string fallo = null;
        try
        {
            reproductor.Stop();
            reproductor.source = VideoSource.Url;
            urlPreparada = new Uri(ruta).AbsoluteUri;
            reproductor.url = urlPreparada;
            reproductor.Prepare();
        }
        catch (Exception e) { fallo = e.Message; }
        if (fallo != null) { Fallar(fallo); yield break; }
        float limite = Time.realtimeSinceStartup + 45;
        while (operacion == operacionActual && Estado == EstadoVideo.Preparando && Time.realtimeSinceStartup < limite) yield return null;
        if (operacion == operacionActual && Estado == EstadoVideo.Preparando)
        { reproductor.Stop(); Fallar("El video tardó demasiado en prepararse. Reintenta."); }
    }

    void Preparado(VideoPlayer player)
    {
        if (Estado != EstadoVideo.Preparando || player.url != urlPreparada) return;
        player.Play();
        EstadoNuevo(EstadoVideo.Reproduciendo, "Reproduciendo " + VideoActual.name);
    }
    void ErrorVideo(VideoPlayer player, string mensaje)
    {
        if (VideoActual != null && !string.IsNullOrEmpty(urlPreparada) && player.url == urlPreparada &&
            (Estado == EstadoVideo.Preparando || Estado == EstadoVideo.Reproduciendo))
            Fallar("Error del reproductor: " + mensaje);
    }
    void Fallar(string mensaje)
    {
        EstadoNuevo(EstadoVideo.Error, mensaje);
        Debug.LogWarning("[CACHE-VIDEO] " + mensaje, this);
    }
    void LiberarDescarga()
    {
        solicitud?.Abort(); solicitud?.Dispose(); solicitud = null;
        if (!string.IsNullOrEmpty(temporal))
        {
            try { if (File.Exists(temporal)) File.Delete(temporal); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
            temporal = null;
        }
    }
    void OnDisable()
    {
        StopAllCoroutines(); LiberarDescarga();
        if (Ocupado)
        {
            if (Estado == EstadoVideo.Preparando && reproductor != null) reproductor.Stop();
            EstadoNuevo(EstadoVideo.Disponible, "Operación cancelada");
        }
    }
    void OnDestroy()
    {
        if (reproductor == null) return;
        reproductor.prepareCompleted -= Preparado;
        reproductor.errorReceived -= ErrorVideo;
    }
    [ContextMenu("Probar descarga y reproducción")]
    public void ProbarVideo()
    {
        var catalogo = GetComponent<CatalogoVideosDrive>();
        if (catalogo != null && catalogo.gestorCache == this) catalogo.ProbarVideoSeleccionado();
        else if (VideoActual != null) ReproducirVideoDelCatalogo(VideoActual);
        else Debug.LogWarning("Selecciona un video real desde la biblioteca.", this);
    }
}
