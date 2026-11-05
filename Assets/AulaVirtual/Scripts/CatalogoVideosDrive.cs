
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class VideoInfoDrive
{
    public string id;
    public string name;
    public string mimeType;
    public string size;
    public string modifiedTime;
}

[Serializable]
public class RespuestaCatalogo
{
    public int total;
    public VideoInfoDrive[] videos;
}

public class CatalogoVideosDrive : MonoBehaviour
{
    public enum EstadoConsulta { Neutral, Cargando, Conectado, Error, SinConexion }
    [Serializable]
    class CatalogoGuardado
    {
        public string servidor;
        public RespuestaCatalogo catalogo;
    }

    public event Action EstadoCambiado;
    public EstadoConsulta Estado { get; private set; }
    public string Mensaje { get; private set; } = "Pulsa Actualizar para consultar";
    public bool EnConsulta { get; private set; }
    public bool TieneCatalogoValido { get; private set; }
    public bool EsCatalogoLocal { get; private set; }
    public string ServidorActual => Origen;
    public string UltimaConsultaUrl { get; private set; }
    public long UltimoCodigoHttp { get; private set; }
    UnityWebRequest solicitud;
    string Origen => ConfiguracionBackendAula.Obtener(servidor);
    string RutaCatalogo => Path.Combine(Application.persistentDataPath, "catalogo-videos-drive.json");

    void Awake()
    {
        CargarUltimoCatalogo();
    }
    void Start() { if (consultarAlIniciar) ConsultarBiblioteca(); }

    void CambiarEstado(EstadoConsulta estado, string mensaje)
    {
        Estado = estado;
        Mensaje = mensaje;
        EstadoCambiado?.Invoke();
    }

    [Header("Servidor de biblioteca")]
    public string servidor = "http://127.0.0.1:3000";
    public bool consultarAlIniciar = true;

    [Header("Videos obtenidos de Drive")]
    public RespuestaCatalogo catalogo;

    [ContextMenu("Consultar biblioteca de Drive")]
    public void ConsultarBiblioteca()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Activa Play antes de consultar.");
            return;
        }

        if (!isActiveAndEnabled || EnConsulta) return;
        if (!ConfiguracionBackendAula.IntentarObtener(servidor, out _, out string errorConfiguracion))
        {
            EsCatalogoLocal = TieneCatalogoValido;
            CambiarEstado(EstadoConsulta.Error, errorConfiguracion);
            return;
        }
        EnConsulta = true;
        CambiarEstado(EstadoConsulta.Cargando, "Conectando con el servidor...");
        StartCoroutine(ObtenerCatalogo());
    }

    private IEnumerator ObtenerCatalogo()
    {
        UltimaConsultaUrl = Origen + "/api/videos";
        UltimoCodigoHttp = 0;
        using (UnityWebRequest request = UnityWebRequest.Get(UltimaConsultaUrl))
        {
            solicitud = request;
            request.timeout = Origen.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? 120 : 15;
            Debug.Log("[CATALOGO-DRIVE] CONSULTA RED · " + UltimaConsultaUrl + " · timeout=" + request.timeout + " s", this);

            UnityWebRequestAsyncOperation operacion = null;
            try { operacion = request.SendWebRequest(); }
            catch (Exception e) when (e is InvalidOperationException || e is UnityException)
            {
                solicitud = null;
                EnConsulta = false;
                EsCatalogoLocal = TieneCatalogoValido;
                CambiarEstado(EstadoConsulta.Error, "No se pudo iniciar la consulta");
                Debug.LogWarning("[CATALOGO-DRIVE] " + e.Message, this);
            }
            if (operacion == null) yield break;
            float inicio = Time.realtimeSinceStartup;
            bool esperaAvisada = false;
            while (!operacion.isDone)
            {
                if (!esperaAvisada && Time.realtimeSinceStartup - inicio >= 10)
                {
                    esperaAvisada = true;
                    CambiarEstado(EstadoConsulta.Cargando, "Esperando al servidor · El inicio puede tardar");
                }
                yield return null;
            }

            solicitud = null;
            EnConsulta = false;
            UltimoCodigoHttp = request.responseCode;
            if (request.result != UnityWebRequest.Result.Success)
            {
                EsCatalogoLocal = TieneCatalogoValido;
                var estado = request.result == UnityWebRequest.Result.ConnectionError
                    ? EstadoConsulta.SinConexion : EstadoConsulta.Error;
                CambiarEstado(estado, estado == EstadoConsulta.SinConexion
                    ? "Servidor no disponible · Pulsa Actualizar para reintentar" : "Error del servidor · HTTP " + request.responseCode + " · Reintenta con Actualizar");
                Debug.LogWarning("[CATALOGO-DRIVE] " + Mensaje + ": " + request.error, this);
                yield break;
            }

            if (!IntentarLeer(request.downloadHandler.text, out var nuevo))
            {
                EsCatalogoLocal = TieneCatalogoValido;
                CambiarEstado(EstadoConsulta.Error, "Respuesta del catálogo inválida");
                Debug.LogWarning("[CATALOGO-DRIVE] Respuesta inválida; se conserva el último catálogo válido.", this);
                yield break;
            }
            catalogo = nuevo;
            TieneCatalogoValido = true;
            EsCatalogoLocal = false;
            GuardarUltimoCatalogo();
            CambiarEstado(EstadoConsulta.Conectado, "Conectado · " + catalogo.videos.Length + " videos");
            Debug.Log("[CATALOGO-DRIVE] HTTP " + UltimoCodigoHttp + " · RED · " + UltimaConsultaUrl + " · " + catalogo.videos.Length + " videos. No es el catálogo guardado.", this);
        }
    }

    static bool EsValido(RespuestaCatalogo respuesta)
    {
        if (respuesta == null || respuesta.videos == null) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var video in respuesta.videos)
            if (video == null || string.IsNullOrWhiteSpace(video.id) ||
                string.IsNullOrWhiteSpace(video.name) || !ids.Add(video.id)) return false;
        return true;
    }

    static bool IntentarLeer(string json, out RespuestaCatalogo respuesta)
    {
        respuesta = null;
        try { respuesta = JsonUtility.FromJson<RespuestaCatalogo>(json); }
        catch (ArgumentException) { return false; }
        return EsValido(respuesta);
    }

    void GuardarUltimoCatalogo()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string temporal = RutaCatalogo + ".tmp";
            File.WriteAllText(temporal, JsonUtility.ToJson(new CatalogoGuardado { servidor = Origen, catalogo = catalogo }));
            if (File.Exists(RutaCatalogo)) File.Replace(temporal, RutaCatalogo, null);
            else File.Move(temporal, RutaCatalogo);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
        {
            Debug.LogWarning("[CATALOGO-DRIVE] Catálogo disponible en memoria, pero no se pudo guardar: " + e.Message, this);
        }
    }

    void CargarUltimoCatalogo()
    {
        try
        {
            if (!File.Exists(RutaCatalogo)) return;
            var guardado = JsonUtility.FromJson<CatalogoGuardado>(File.ReadAllText(RutaCatalogo));
            if (guardado == null || !ConfiguracionBackendAula.EsCatalogoCompatible(guardado.servidor, servidor) || !EsValido(guardado.catalogo)) return;
            catalogo = guardado.catalogo;
            TieneCatalogoValido = true;
            EsCatalogoLocal = true;
            Mensaje = "Catálogo local · Pulsa Actualizar";
            Debug.Log("[CATALOGO-DRIVE] CATÁLOGO GUARDADO · Solo archivos con caché verificada; todavía no hay respuesta de red.", this);
            EstadoCambiado?.Invoke();
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            Debug.LogWarning("[CATALOGO-DRIVE] No se pudo leer el catálogo local: " + e.Message, this);
        }
    }

    void OnDisable()
    {
        if (!EnConsulta) return;
        solicitud?.Abort();
        StopAllCoroutines();
        solicitud?.Dispose();
        solicitud = null;
        EnConsulta = false;
        CambiarEstado(EstadoConsulta.Neutral, "Consulta cancelada · Pulsa Actualizar");
    }
    
[Header("Prueba de selección")]
public GestorCacheVideos gestorCache;
public int indiceVideoPrueba = 0;

[ContextMenu("Probar video seleccionado del catálogo")]
public void ProbarVideoSeleccionado()
{
    if (catalogo == null || catalogo.videos == null ||
        catalogo.videos.Length == 0)
    {
        Debug.LogWarning("Primero consulta la biblioteca.");
        return;
    }

    if (gestorCache == null)
    {
        Debug.LogError("Asigna el GestorCacheVideos.");
        return;
    }

    if (indiceVideoPrueba < 0 ||
        indiceVideoPrueba >= catalogo.videos.Length)
    {
        Debug.LogWarning("El índice seleccionado no existe.");
        return;
    }

    gestorCache.ReproducirVideoDelCatalogo(
        catalogo.videos[indiceVideoPrueba]
    );
}

}
