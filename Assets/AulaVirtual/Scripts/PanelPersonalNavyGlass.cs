using TMPro;
using UnityEngine;

// Presentación del asiento: no mueve el rig ni controla descargas.
public sealed class PanelPersonalNavyGlass : MonoBehaviour
{
    public const string Id = "PanelPersonal_NavyGlass_v1";
    public PruebaAsientoXR asiento;
    public ControladorVideoXR controlador;
    public GestorCacheVideos gestorCache;
    public TMP_Text tituloVideo;
    public TMP_Text estadoVideo;
    public GameObject bibliotecaCercanaReservada;
    public GameObject barraCompacta;
    public GameObject vistaControles;
    public BibliotecaVisualAula bibliotecaPersonal;
    public enum EstadoPanel { Compacto, Controles, Biblioteca }
    public EstadoPanel Estado { get; private set; }
    [Header("Biblioteca expandida (mantiene el centro y orientación del panel)")]
    [Range(.6f,1.5f)] public float anchoBibliotecaMetros = 1f;
    string videoPendiente;

    [Header("Posición relativa a la mirada al sentarse (metros)")]
    [Min(.4f)] public float distancia = .85f;
    public float altura = -.28f;
    public float desplazamientoLateral;
    [Range(.4f, 1.2f)] public float anchoMetros = .78f;
    public Vector3 ajusteOrientacion;
    float siguienteActualizacion;

    public void Mostrar(bool visible)
    {
        if (!visible) { gameObject.SetActive(false); return; }
        if (asiento == null || asiento.CamaraXR == null) return;
        Colocar(asiento.CamaraXR.transform);
        gameObject.SetActive(true);
        Minimizar();
        ActualizarTextos();
    }

    void OnEnable() { if(gestorCache!=null) gestorCache.EstadoCambiado+=AlEstadoVideo; }
    void OnDisable()
    {
        if(gestorCache!=null) gestorCache.EstadoCambiado-=AlEstadoVideo;
        videoPendiente=null;
    }
    public void Minimizar() => CambiarEstado(EstadoPanel.Compacto);
    public void AbrirControles() => CambiarEstado(EstadoPanel.Controles);
    public void AbrirBiblioteca()
    {
        CambiarEstado(EstadoPanel.Biblioteca);
        if(bibliotecaPersonal!=null && bibliotecaPersonal.catalogoDrive!=null &&
            (gestorCache==null || !gestorCache.Ocupado)) bibliotecaPersonal.catalogoDrive.ConsultarBiblioteca();
    }
    void CambiarEstado(EstadoPanel estado)
    {
        if(asiento==null || !asiento.EstaSentado || !gameObject.activeInHierarchy) return;
        Estado=estado;
        if(barraCompacta!=null) barraCompacta.SetActive(estado==EstadoPanel.Compacto);
        if(vistaControles!=null) vistaControles.SetActive(estado==EstadoPanel.Controles);
        if(bibliotecaCercanaReservada!=null)
        {
            // Escala propia sin cambiar la pose ni el tamaño ajustado de los controles.
            if(bibliotecaPersonal!=null)
            {
                float factor=anchoBibliotecaMetros/Mathf.Max(.01f,anchoMetros)*1200f/1600f;
                var rect=(RectTransform)bibliotecaCercanaReservada.transform;
                rect.localScale=Vector3.one*factor;
                rect.anchoredPosition=new Vector2((1200-1600*factor)*.5f,-(620-900*factor)*.5f);
            }
            bibliotecaCercanaReservada.SetActive(estado==EstadoPanel.Biblioteca);
        }
    }
    public void NotificarSeleccion(VideoInfoDrive video) { videoPendiente=video!=null?video.id:null; }
    void AlEstadoVideo()
    {
        if(gestorCache==null || string.IsNullOrEmpty(videoPendiente)) return;
        if(gestorCache.Estado==GestorCacheVideos.EstadoVideo.Error) { videoPendiente=null; return; }
        if(gestorCache.Estado!=GestorCacheVideos.EstadoVideo.Reproduciendo) return;
        if(gestorCache.VideoActual!=null && gestorCache.VideoActual.id==videoPendiente && Estado==EstadoPanel.Biblioteca) Minimizar();
        videoPendiente=null;
    }

    public void Colocar(Transform cabeza)
    {
        Vector3 forward = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.ProjectOnPlane(asiento.VistaSentado.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 position = cabeza.position + forward * distancia + Vector3.up * altura + right * desplazamientoLateral;
        // El frontal del Canvas apunta hacia -Z, por eso su forward se aleja de la cabeza.
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - cabeza.position, Vector3.up) * Quaternion.Euler(ajusteOrientacion));
        Vector3 inherited = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float scale = anchoMetros / 1200f;
        transform.localScale = new Vector3(scale / Mathf.Max(.00001f,Mathf.Abs(inherited.x)),
            scale / Mathf.Max(.00001f,Mathf.Abs(inherited.y)), scale / Mathf.Max(.00001f,Mathf.Abs(inherited.z)));
    }

    [ContextMenu("Recolocar panel durante Play")]
    public void Recolocar()
    {
        if (Application.isPlaying && asiento != null && asiento.EstaSentado && asiento.CamaraXR != null)
            Colocar(asiento.CamaraXR.transform);
    }

    void Update()
    {
        // Solo texto, a 4 Hz. La posición queda fija: no sigue los movimientos de cabeza.
        if (Time.unscaledTime < siguienteActualizacion) return;
        siguienteActualizacion = Time.unscaledTime + .25f;
        ActualizarTextos();
    }

    void ActualizarTextos()
    {
        var player = controlador != null ? controlador.GetComponent<UnityEngine.Video.VideoPlayer>() : null;
        if (tituloVideo != null)
        {
            tituloVideo.richText = false;
            tituloVideo.text = gestorCache != null && gestorCache.VideoActual != null
                ? FilaCatalogoVideoAula.NombreVisible(gestorCache.VideoActual.name)
                : player != null && player.clip != null ? player.clip.name : "Sin video seleccionado";
        }
        if (estadoVideo != null)
        {
            estadoVideo.richText = false;
            estadoVideo.text = gestorCache != null && gestorCache.Ocupado ? gestorCache.Mensaje :
                player == null ? "Reproductor no asignado" : player.isPlaying ? "Reproduciendo" :
                player.isPrepared ? "En pausa" : "Detenido";
        }
    }
}
