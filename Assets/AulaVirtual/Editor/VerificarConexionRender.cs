using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Prueba explícita en Editor: usa el catálogo y la lectura de caché reales en una escena
// de previsualización. No crea reproductores, no reproduce ni guarda escenas del aula.
public static class VerificarConexionRender
{
    [Serializable] class Informe
    {
        public bool ok, servidorLocalApagado, consultaRed, descargaCompleta, estructuraMp4Valida,
            hashCoincideConCache, cacheIntacta, catalogoPrevioPreservado, androidUsaHttps;
        public string url, sha256, error;
        public long httpCatalogo, httpDescarga, bytesEsperados, bytesRecibidos;
        public int videos, archivosCache, videosOfflineAntes, videosOfflineDespues, timeoutConsulta;
        public float segundosConsulta;
        public string alcance = "Editor: transporte y lógica de catálogo/caché; no prueba visual XR ni reproducción audiovisual";
    }
    static Informe informe;
    static Scene preview;
    static GameObject temporal;
    static CatalogoVideosDrive catalogo;
    static GestorCacheVideos cache;
    static IEnumerator rutina;
    static UnityWebRequest descarga;
    static string catalogoPath, mp4Temporal;
    static byte[] catalogoAnterior;
    static Dictionary<string, string> cacheAntes;
    static double inicio, inicioConsulta;
    static bool batch;

    [MenuItem("Aula Virtual/Biblioteca visual/Verificar Render sin reproducir")]
    static void Menu() => Iniciar(false);
    public static void EjecutarBatch() => Iniciar(true);
    static object Invocar(object objeto, string nombre, params object[] argumentos) =>
        objeto.GetType().GetMethod(nombre, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(objeto, argumentos);
    static string Hash(string path)
    {
        using (var sha = SHA256.Create()) using (var file = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
    }
    static Dictionary<string, string> InventarioCache()
    {
        string path = Path.Combine(Application.persistentDataPath, "BibliotecaVideos");
        return Directory.Exists(path) ? Directory.GetFiles(path).ToDictionary(p => p, Hash) : new Dictionary<string, string>();
    }
    static void Iniciar(bool cerrarAlTerminar)
    {
        if (informe != null || Application.isPlaying) throw new InvalidOperationException("Detén Play o espera a la prueba activa.");
        batch = cerrarAlTerminar;
        informe = new Informe(); inicio = EditorApplication.timeSinceStartup;
        try
        {
            using (var socket = new TcpClient())
            {
                try { socket.Connect("127.0.0.1", 3000); }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionRefused) { informe.servidorLocalApagado = true; }
            }
            if (!informe.servidorLocalApagado) throw new InvalidOperationException("El puerto local 3000 está activo; no se apagó ningún proceso ajeno.");
            if (!ConfiguracionBackendAula.IntentarObtener(null, out informe.url, out var error) ||
                informe.url != "https://aula-virtual-inclusiva.onrender.com")
                throw new InvalidOperationException(error ?? "Selecciona Remoto con la URL real antes de esta prueba.");
            var config = Resources.Load<TextAsset>("BibliotecaBackend");
            informe.androidUsaHttps = config != null && config.text.Contains(informe.url) && new Uri(informe.url).Scheme == "https";
            catalogoPath = Path.Combine(Application.persistentDataPath, "catalogo-videos-drive.json");
            catalogoAnterior = File.Exists(catalogoPath) ? File.ReadAllBytes(catalogoPath) : null;
            cacheAntes = InventarioCache(); informe.archivosCache = cacheAntes.Count;
            preview = EditorSceneManager.NewPreviewScene();
            temporal = new GameObject("PruebaRender_Temporal") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(temporal, preview);
            catalogo = temporal.AddComponent<CatalogoVideosDrive>(); cache = temporal.AddComponent<GestorCacheVideos>();
            Invocar(catalogo, "CargarUltimoCatalogo");
            informe.videosOfflineAntes = catalogo.catalogo?.videos.Count(v => cache.EstaEnCache(v)) ?? 0;
            rutina = Prueba();
            EditorApplication.update += Paso;
        }
        catch (Exception e) { Terminar(e); }
    }
    static IEnumerator Prueba()
    {
        // Ejecutar la misma coroutine privada usada por ConsultarBiblioteca, sin entrar
        // en Play ni activar las interacciones de la escena principal.
        typeof(CatalogoVideosDrive).GetProperty("EnConsulta").SetValue(catalogo, true);
        Invocar(catalogo, "CambiarEstado", CatalogoVideosDrive.EstadoConsulta.Cargando, "Conectando con el servidor...");
        inicioConsulta = EditorApplication.timeSinceStartup;
        var consulta = (IEnumerator)Invocar(catalogo, "ObtenerCatalogo");
        try
        {
            while (consulta.MoveNext())
            {
                var solicitud = (UnityWebRequest)typeof(CatalogoVideosDrive).GetField("solicitud", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalogo);
                if (solicitud != null) informe.timeoutConsulta = solicitud.timeout;
                yield return null;
            }
        }
        finally { (consulta as IDisposable)?.Dispose(); }
        informe.segundosConsulta = (float)(EditorApplication.timeSinceStartup - inicioConsulta);
        informe.httpCatalogo = catalogo.UltimoCodigoHttp;
        informe.consultaRed = catalogo.Estado == CatalogoVideosDrive.EstadoConsulta.Conectado && !catalogo.EsCatalogoLocal &&
            catalogo.UltimaConsultaUrl == informe.url + "/api/videos" && informe.httpCatalogo == 200;
        if (!informe.consultaRed || informe.timeoutConsulta != 120) throw new InvalidOperationException("La consulta real a Render no se confirmó: " + catalogo.Mensaje);
        informe.videos = catalogo.catalogo.videos.Length;
        var video = catalogo.catalogo.videos.Where(v => long.TryParse(v.size, out long n) && n > 0)
            .OrderBy(v => long.Parse(v.size)).FirstOrDefault();
        if (video == null) throw new InvalidOperationException("El catálogo no contiene un video comprobable.");
        string referencia = (string)Invocar(cache, "BuscarCache", video);
        string hashPrevio = referencia != null ? Hash(referencia) : null;
        informe.bytesEsperados = long.Parse(video.size);
        mp4Temporal = Path.Combine(Path.GetTempPath(), "AulaRender_" + Guid.NewGuid().ToString("N") + ".mp4");
        descarga = UnityWebRequest.Get(informe.url + "/api/videos/" + Uri.EscapeDataString(video.id) + "/download");
        descarga.downloadHandler = new DownloadHandlerFile(mp4Temporal) { removeFileOnAbort = true };
        descarga.timeout = 120;
        var operacion = descarga.SendWebRequest(); while (!operacion.isDone) yield return null;
        informe.httpDescarga = descarga.responseCode;
        if (descarga.result != UnityWebRequest.Result.Success || informe.httpDescarga != 200) throw new InvalidOperationException("No se completó la descarga autorizada de Render.");
        informe.bytesRecibidos = new FileInfo(mp4Temporal).Length;
        string longitud = descarga.GetResponseHeader("Content-Length");
        informe.descargaCompleta = informe.bytesRecibidos == informe.bytesEsperados &&
            (string.IsNullOrEmpty(longitud) || (long.TryParse(longitud, out var n) && n == informe.bytesEsperados));
        informe.estructuraMp4Valida = Mp4Completo(mp4Temporal);
        informe.sha256 = Hash(mp4Temporal);
        informe.hashCoincideConCache = hashPrevio != null && hashPrevio == informe.sha256;
        if (!informe.descargaCompleta || !informe.estructuraMp4Valida || !informe.hashCoincideConCache)
            throw new InvalidOperationException("Integridad no confirmada contra una copia previa de caché. No se declara exitosa la descarga.");
        // Lectura offline del catálogo persistido: no solicita la red ni reproduce archivos.
        Invocar(catalogo, "CargarUltimoCatalogo");
        informe.videosOfflineDespues = catalogo.catalogo.videos.Count(v => cache.EstaEnCache(v));
    }
    static bool Mp4Completo(string path)
    {
        bool ftyp = false, moov = false, mdat = false;
        using (var f = File.OpenRead(path)) using (var r = new BinaryReader(f))
        {
            while (f.Position < f.Length)
            {
                long start = f.Position; if (f.Length - start < 8) return false;
                var header = r.ReadBytes(8); ulong size = ((ulong)header[0] << 24) | ((ulong)header[1] << 16) | ((ulong)header[2] << 8) | header[3];
                string type = System.Text.Encoding.ASCII.GetString(header, 4, 4); int minimum = 8;
                if (size == 1) { if (f.Length - f.Position < 8) return false; size = 0; foreach (byte b in r.ReadBytes(8)) size = (size << 8) | b; minimum = 16; }
                else if (size == 0) size = (ulong)(f.Length - start);
                if (size < (ulong)minimum || size > (ulong)(f.Length - start)) return false;
                ftyp |= type == "ftyp"; moov |= type == "moov"; mdat |= type == "mdat";
                f.Position = start + (long)size;
            }
        }
        return ftyp && moov && mdat;
    }
    static void Paso()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - inicio > 260) throw new TimeoutException("Tiempo máximo de la prueba agotado.");
            if (!rutina.MoveNext()) Terminar(null);
        }
        catch (Exception e) { Terminar(e); }
    }
    static void Terminar(Exception error)
    {
        EditorApplication.update -= Paso;
        try
        {
            (rutina as IDisposable)?.Dispose(); descarga?.Dispose();
            if (catalogo != null) typeof(CatalogoVideosDrive).GetProperty("EnConsulta").SetValue(catalogo, false);
            if (temporal != null) UnityEngine.Object.DestroyImmediate(temporal);
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            if (mp4Temporal != null && File.Exists(mp4Temporal)) File.Delete(mp4Temporal); // Solo el temporal propio.
            if (catalogoAnterior != null) File.WriteAllBytes(catalogoPath, catalogoAnterior);
            informe.catalogoPrevioPreservado = catalogoAnterior == null || File.ReadAllBytes(catalogoPath).SequenceEqual(catalogoAnterior);
            var despues = InventarioCache();
            informe.cacheIntacta = cacheAntes != null && cacheAntes.Count == despues.Count && cacheAntes.All(p => despues.TryGetValue(p.Key, out var value) && value == p.Value);
            informe.ok = error == null && informe.cacheIntacta && informe.catalogoPrevioPreservado && informe.videosOfflineDespues >= informe.videosOfflineAntes;
            informe.error = error?.GetBaseException().Message;
        }
        catch (Exception e) { informe.ok = false; informe.error = e.GetBaseException().Message; }
        string json = JsonUtility.ToJson(informe, true);
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/prueba-render-unity.json")), json);
        Debug.Log("[PRUEBA-RENDER] " + json);
        bool correcto = informe.ok; informe = null; rutina = null; descarga = null; temporal = null;
        catalogoAnterior = null; cacheAntes = null; mp4Temporal = null; preview = default;
        if (batch) EditorApplication.Exit(correcto ? 0 : 1);
    }
}
