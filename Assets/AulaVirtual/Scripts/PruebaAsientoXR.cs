
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class PruebaAsientoXR : MonoBehaviour
{
    [Header("Sistema XR")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform vistaSentado;

    [Header("Interfaz del asiento")]
    [SerializeField] private GameObject botonLevantarse;
    [SerializeField] private GameObject botonAbrirVideo;
    [SerializeField] private GameObject panelControlesVideo;
    [Header("Panel personal opcional (Navy Glass)")]
    [SerializeField] private PanelPersonalNavyGlass panelPersonal;
    public PanelPersonalNavyGlass PanelPersonal => panelPersonal;
    public Camera CamaraXR => xrOrigin != null ? xrOrigin.Camera : null;
    public Transform VistaSentado => vistaSentado;
    public bool EstaSentado => sentado;

    private bool sentado = false;

    private Vector3 posicionAnterior;
    private Vector3 direccionAnterior;

    private void Start()
    {
        sentado = false;
        ActualizarInterfaz(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!sentado && Keyboard.current.bKey.wasPressedThisFrame)
            Sentarse();
        else if (sentado && Keyboard.current.nKey.wasPressedThisFrame)
            Levantarse();
    }

    private bool XRPreparado()
    {
        return xrOrigin != null &&
               vistaSentado != null &&
               xrOrigin.Camera != null;
    }

    private void Sentarse()
    {
        if (sentado || !XRPreparado())
            return;

        Transform cabeza = xrOrigin.Camera.transform;

        posicionAnterior = cabeza.position;

        direccionAnterior = Vector3.ProjectOnPlane(
            cabeza.forward,
            Vector3.up
        ).normalized;

        if (direccionAnterior.sqrMagnitude < 0.001f)
            direccionAnterior = xrOrigin.transform.forward;

        xrOrigin.MatchOriginUpCameraForward(
            Vector3.up,
            vistaSentado.forward
        );

        xrOrigin.MoveCameraToWorldLocation(
            vistaSentado.position
        );

        sentado = true;
        ActualizarInterfaz(true);

        Debug.Log("Estudiante sentado");
    }

    private void Levantarse()
    {
        if (!sentado || !XRPreparado())
            return;

        xrOrigin.MatchOriginUpCameraForward(
            Vector3.up,
            direccionAnterior
        );

        xrOrigin.MoveCameraToWorldLocation(
            posicionAnterior
        );

        sentado = false;
        ActualizarInterfaz(false);

        Debug.Log("Estudiante levantado");
    }

    
private void ActualizarInterfaz(bool estaSentado)
{
    // Conservar los controles 3D originales como alternativa al panel personal.
    if (botonLevantarse != null)
        botonLevantarse.SetActive(false);

    // El menú original solo se usa cuando no hay panel personal asignado.
    if (botonAbrirVideo != null)
        botonAbrirVideo.SetActive(estaSentado && panelPersonal == null);

    // Cerrar los controles originales; el personal aparece completo al sentarse.
    if (panelControlesVideo != null)
        panelControlesVideo.SetActive(false);
    if (panelPersonal != null) panelPersonal.Mostrar(estaSentado);
}

public void AlternarPanelVideo()
{
    if (panelPersonal != null)
    {
        if (sentado) panelPersonal.Mostrar(true);
        return;
    }
    if (!sentado || panelControlesVideo == null)
        return;

    bool abrir = !panelControlesVideo.activeSelf;

    panelControlesVideo.SetActive(abrir);

    // Mostrar Levantarse solamente con el menú abierto.
    if (botonLevantarse != null)
        botonLevantarse.SetActive(abrir);

    Debug.Log(abrir
        ? "Panel de video abierto"
        : "Panel de video cerrado");
}

    public void SentarseDesdeSilla()
    {
        Sentarse();
    }

    public void LevantarseDesdeBoton()
    {
        Levantarse();
    }
}
