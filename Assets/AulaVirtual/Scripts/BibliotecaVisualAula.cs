using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentación del catálogo. La conexión pertenece a CatalogoVideosDrive.
[DisallowMultipleComponent]
public sealed class BibliotecaVisualAula : MonoBehaviour
{
    public const string Id = "BibliotecaVisual_Aula_v1";
    public RectTransform contenido;
    public GameObject plantillaFila;
    public Button actualizar;
    public Button entrarCine;
    public GameObject estadoSinConexion;
    public TMP_Text estadoCatalogo;
    public GestorCineXR gestorCine;
    public CatalogoVideosDrive catalogoDrive;
    public GestorCacheVideos gestorCache;
    public Button anterior;
    public Button siguiente;
    public TMP_Text indicadorPagina;
    public TMP_Text totalVideos;
    [Tooltip("Opcional. Solo para la vista personal; los servicios siguen siendo compartidos.")]
    public PanelPersonalNavyGlass panelPersonal;
    int pagina;
    readonly System.Collections.Generic.List<VideoInfoDrive> videosVisibles = new System.Collections.Generic.List<VideoInfoDrive>();
    GameObject barraVideo;
    CatalogoVideosDrive suscrito;
    readonly System.Collections.Generic.List<GameObject> filas = new System.Collections.Generic.List<GameObject>();

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (actualizar != null) actualizar.onClick.AddListener(ActualizarCatalogo);
        if (entrarCine != null) entrarCine.onClick.AddListener(EntrarAlCine);
        if (anterior != null) anterior.onClick.AddListener(PaginaAnterior);
        if (siguiente != null) siguiente.onClick.AddListener(PaginaSiguiente);
        // Compatibilidad con la UI ya generada: no requiere regenerar ni tocar XR.
        if (catalogoDrive == null)
            foreach (var candidato in FindObjectsByType<CatalogoVideosDrive>(FindObjectsInactive.Include))
                if (candidato.gameObject.scene == gameObject.scene)
                {
                    if (catalogoDrive != null)
                    { Debug.LogError("[BIBLIOTECA] Hay varios catálogos; asigna Catalogo Drive en Inspector.", this); catalogoDrive = null; break; }
                    catalogoDrive = candidato;
                }
        suscrito = catalogoDrive;
        if (suscrito != null) suscrito.EstadoCambiado += MostrarEstado;
        if (gestorCache == null && suscrito != null) gestorCache = suscrito.gestorCache;
        if (gestorCache != null) gestorCache.EstadoCambiado += ActualizarEstadoVideo;
        CrearBarraVideo();
        LimpiarContenido();
        MostrarEstado();
    }

    void Start() => MostrarEstado(); // El catálogo puede cargar su respaldo en Awake después de OnEnable de la UI.

    void OnDisable()
    {
        if (actualizar != null) actualizar.onClick.RemoveListener(ActualizarCatalogo);
        if (entrarCine != null) entrarCine.onClick.RemoveListener(EntrarAlCine);
        if (anterior != null) anterior.onClick.RemoveListener(PaginaAnterior);
        if (siguiente != null) siguiente.onClick.RemoveListener(PaginaSiguiente);
        if (suscrito != null) suscrito.EstadoCambiado -= MostrarEstado;
        if (gestorCache != null) gestorCache.EstadoCambiado -= ActualizarEstadoVideo;
        suscrito = null;
    }

    void ActualizarCatalogo()
    {
        if (gestorCache != null && gestorCache.Ocupado) return;
        if (suscrito == null) { MostrarEstado(); return; }
        pagina = 0;
        Debug.Log("[BIBLIOTECA] Actualizar: consultando catálogo real.", this);
        suscrito.ConsultarBiblioteca();
    }

    void MostrarEstado()
    {
        if (!Application.isPlaying) return;
        bool ocupadoPersonal = panelPersonal != null && gestorCache != null && gestorCache.Ocupado;
        if (gestorCache != null && gestorCache.Ocupado && panelPersonal == null) { ActualizarEstadoVideo(); return; }
        if (estadoSinConexion != null) estadoSinConexion.SetActive(false);
        if (actualizar != null) actualizar.interactable = !ocupadoPersonal && (suscrito == null || !suscrito.EnConsulta);
        if (suscrito == null)
        {
            if (estadoCatalogo != null) estadoCatalogo.text = "ERROR · Asigna Catalogo Drive en Inspector";
            return;
        }
        if (estadoCatalogo != null)
            estadoCatalogo.text = suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.Cargando
                ? "CARGANDO · " + suscrito.Mensaje : suscrito.Mensaje;
        if (suscrito.EnConsulta) { ActualizarPaginacion(); if(ocupadoPersonal) ActualizarEstadoVideo(); return; }
        LimpiarContenido();
        videosVisibles.Clear();
        bool local = suscrito.EsCatalogoLocal || suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.SinConexion ||
                     suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.Error;
        if (suscrito.TieneCatalogoValido && contenido != null && plantillaFila != null)
            foreach (var video in suscrito.catalogo.videos)
            {
                // Solo afirmamos disponibilidad offline tras comprobar el archivo completo.
                bool cache = gestorCache != null && gestorCache.EstaEnCache(video);
                if (local && !cache) continue;
                videosVisibles.Add(video);
            }
        pagina = PaginacionBiblioteca.Ajustar(pagina, videosVisibles.Count);
        // Solo se instancian las dos filas de la página actual, después del filtro offline.
        int inicio = anterior != null && siguiente != null ? PaginacionBiblioteca.Inicio(pagina, videosVisibles.Count) : 0;
        int fin = anterior != null && siguiente != null ? PaginacionBiblioteca.Fin(pagina, videosVisibles.Count) : videosVisibles.Count;
        for (int i = inicio; i < fin; i++)
            {
                var video = videosVisibles[i];
                bool cache = gestorCache != null && gestorCache.EstaEnCache(video);
                var row = Instantiate(plantillaFila, contenido, false);
                row.name = "VideoCatalogo_" + filas.Count;
                var view = row.GetComponent<FilaCatalogoVideoAula>();
                if (view == null) view = row.AddComponent<FilaCatalogoVideoAula>();
                view.Mostrar(video, cache, SeleccionarVideo);
                view.Actualizar(gestorCache);
                filas.Add(row);
                row.SetActive(true);
            }
        if (estadoCatalogo != null)
        {
            if (suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.SinConexion)
                estadoCatalogo.text = "Sin conexión · Videos guardados en este dispositivo";
            else if (suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.Error)
                estadoCatalogo.text = "ERROR · " + suscrito.Mensaje;
            else if (suscrito.Estado == CatalogoVideosDrive.EstadoConsulta.Conectado)
                estadoCatalogo.text = videosVisibles.Count == 0 ? "No hay videos disponibles" : "Biblioteca actualizada";
        }
        if (estadoSinConexion != null && local)
        {
            estadoSinConexion.SetActive(true);
            var text = estadoSinConexion.GetComponent<TMP_Text>();
            if (text != null) text.text = filas.Count > 0 ? "Mostrando solo archivos con caché verificada" :
                "No hay videos con caché verificada para mostrar";
        }
        // Solo el listado antiguo tenía un Content desplazable. En la paginación,
        // ListaVideos está colocada bajo la cabecera y no debe moverse al origen.
        var scroll = contenido != null ? contenido.GetComponentInParent<ScrollRect>() : null;
        if (scroll != null && scroll.content == contenido) scroll.verticalNormalizedPosition = 1;
        ActualizarPaginacion();
        if(ocupadoPersonal) ActualizarEstadoVideo();
    }

    void PaginaAnterior() => CambiarPagina(-1);
    void PaginaSiguiente() => CambiarPagina(1);
    void CambiarPagina(int delta)
    {
        if ((gestorCache != null && gestorCache.Ocupado) || (suscrito != null && suscrito.EnConsulta)) return;
        pagina = PaginacionBiblioteca.Ajustar(pagina + delta, videosVisibles.Count);
        MostrarEstado();
    }
    void ActualizarPaginacion()
    {
        int paginas = PaginacionBiblioteca.TotalPaginas(videosVisibles.Count);
        bool ocupado = (gestorCache != null && gestorCache.Ocupado) || (suscrito != null && suscrito.EnConsulta);
        if (totalVideos != null) totalVideos.text = videosVisibles.Count + (videosVisibles.Count == 1 ? " video" : " videos");
        if (indicadorPagina != null)
        { indicadorPagina.text = (pagina + 1) + " / " + paginas; indicadorPagina.gameObject.SetActive(paginas > 1); }
        if (anterior != null) { anterior.gameObject.SetActive(paginas > 1); anterior.interactable = !ocupado && pagina > 0; }
        if (siguiente != null) { siguiente.gameObject.SetActive(paginas > 1); siguiente.interactable = !ocupado && pagina + 1 < paginas; }
    }

    void SeleccionarVideo(VideoInfoDrive video)
    {
        if (gestorCache == null)
        { if (estadoCatalogo != null) estadoCatalogo.text = "ERROR · GestorCacheVideos sin asignar"; return; }
        if (gestorCache.Ocupado) return;
        if (panelPersonal != null) panelPersonal.NotificarSeleccion(video);
        if (suscrito != null) gestorCache.servidor = suscrito.servidor;
        gestorCache.ReproducirVideoDelCatalogo(video);
    }

    void ActualizarEstadoVideo()
    {
        if (gestorCache == null || !isActiveAndEnabled) return;
        ActualizarPaginacion();
        if (actualizar != null) actualizar.interactable = !gestorCache.Ocupado && (suscrito == null || !suscrito.EnConsulta);
        foreach (var row in filas)
            if (row != null) row.GetComponent<FilaCatalogoVideoAula>()?.Actualizar(gestorCache);
        if (estadoCatalogo != null && gestorCache.VideoActual != null)
        {
            estadoCatalogo.richText = false;
            estadoCatalogo.text = gestorCache.Estado == GestorCacheVideos.EstadoVideo.Descargando
                ? "DESCARGANDO · " + Mathf.FloorToInt(gestorCache.Progreso * 100) + " %" : gestorCache.Mensaje;
        }
        // La vista personal no crea una barra de reproducción ni altera sus vistas
        // por operaciones iniciadas desde el pizarrón. Su dueño coordina los estados.
        if (panelPersonal != null)
        {
            if(!gestorCache.Ocupado)
            {
                MostrarEstado();
                if(gestorCache.Estado == GestorCacheVideos.EstadoVideo.Error && estadoCatalogo != null)
                    estadoCatalogo.text = gestorCache.Mensaje;
            }
            return;
        }
        if (gestorCache.Estado == GestorCacheVideos.EstadoVideo.Reproduciendo)
        {
            var fondo = transform.Find("FondoAzul");
            if (fondo != null) fondo.gameObject.SetActive(false);
            if (barraVideo != null) barraVideo.SetActive(true);
        }
        else if (gestorCache.Estado == GestorCacheVideos.EstadoVideo.Error)
        {
            var fondo = transform.Find("FondoAzul");
            if (fondo != null) fondo.gameObject.SetActive(true);
            if (barraVideo != null) barraVideo.SetActive(false);
        }
    }

    public void VolverABiblioteca()
    {
        if (panelPersonal != null) { panelPersonal.AbrirBiblioteca(); return; }
        if (gestorCache != null && gestorCache.reproductor != null) gestorCache.reproductor.Pause();
        var fondo = transform.Find("FondoAzul");
        if (fondo != null) fondo.gameObject.SetActive(true);
        if (barraVideo != null) barraVideo.SetActive(false);
        MostrarEstado();
    }

    void CrearBarraVideo()
    {
        if (panelPersonal != null) return;
        if (barraVideo != null || entrarCine == null) return;
        var anterior = transform.Find("BarraVideoBiblioteca");
        if (anterior != null)
        { anterior.gameObject.SetActive(false); Destroy(anterior.gameObject); }
        barraVideo = new GameObject("BarraVideoBiblioteca", typeof(RectTransform));
        barraVideo.transform.SetParent(transform, false);
        var rect = (RectTransform)barraVideo.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(56, -24); rect.sizeDelta = new Vector2(1488, 90);
        CrearBotonBarra("Volver a biblioteca", 0, VolverABiblioteca);
        CrearBotonBarra("Entrar al cine", 550, EntrarAlCine);
        barraVideo.SetActive(false);
    }

    void CrearBotonBarra(string titulo, float x, UnityEngine.Events.UnityAction accion)
    {
        var button = Instantiate(entrarCine, barraVideo.transform, false);
        button.name = titulo;
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(x, 0); rect.sizeDelta = new Vector2(520, 90);
        button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(accion);
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = titulo;
            var textRect = label.rectTransform; textRect.anchoredPosition = new Vector2(12, 0);
            textRect.sizeDelta = new Vector2(496, 90);
        }
        var icon = button.transform.Find("IconoAccion");
        if (icon != null) icon.gameObject.SetActive(false);
    }

    void LimpiarContenido()
    {
        filas.Clear();
        if (contenido == null) return;
        foreach (Transform child in contenido)
        {
            if (plantillaFila != null && child.gameObject == plantillaFila) continue;
            // Este Content pertenece exclusivamente a la lista generada. Desactivar
            // primero retira el gráfico de RectMask2D antes de Destroy al final del frame.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    void EntrarAlCine()
    {
        if (gestorCine != null) gestorCine.EntrarAlCine();
        else Debug.LogWarning("[BIBLIOTECA-XR] Asigna GestorCineXR mediante Preparar interacción XR.", this);
    }
}
