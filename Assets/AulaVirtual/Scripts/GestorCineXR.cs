
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class GestorCineXR : MonoBehaviour
{
    [Header("Sistema XR")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Destino inmersivo")]
    [SerializeField] private Transform puntoVistaCine;

    private bool enCine = false;

    private Vector3 posicionAnterior;
    private Vector3 direccionAnterior;

    private void Update()
    {
        // Controles temporales para probar desde la PC.
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.uKey.wasPressedThisFrame)
            EntrarAlCine();

        if (Keyboard.current.oKey.wasPressedThisFrame)
            VolverAlAula();
    }

    public void EntrarAlCine()
    {
        if (enCine || !ReferenciasValidas())
            return;

        Transform cabeza = xrOrigin.Camera.transform;

        // Guardar la posición y orientación actuales.
        posicionAnterior = cabeza.position;

        direccionAnterior = Vector3.ProjectOnPlane(
            cabeza.forward, Vector3.up
        ).normalized;

        if (direccionAnterior.sqrMagnitude < 0.001f)
            direccionAnterior = xrOrigin.transform.forward;

        // Orientar al estudiante hacia la pantalla.
        xrOrigin.MatchOriginUpCameraForward(
            Vector3.up,
            puntoVistaCine.forward
        );

        // Llevar la cámara a la posición del cine.
        xrOrigin.MoveCameraToWorldLocation(
            puntoVistaCine.position
        );

        enCine = true;

        Debug.Log("Entrando al cine inmersivo");
    }

    public void VolverAlAula()
    {
        if (!enCine || !ReferenciasValidas())
            return;

        // Recuperar la orientación anterior.
        xrOrigin.MatchOriginUpCameraForward(
            Vector3.up,
            direccionAnterior
        );

        // Regresar al punto exacto anterior.
        xrOrigin.MoveCameraToWorldLocation(
            posicionAnterior
        );

        enCine = false;

        Debug.Log("Regresando al aula virtual");
    }

    private bool ReferenciasValidas()
    {
        if (xrOrigin == null ||
            xrOrigin.Camera == null ||
            puntoVistaCine == null)
        {
            Debug.LogWarning(
                "GestorCineXR: faltan referencias."
            );

            return false;
        }

        return true;
    }
}
