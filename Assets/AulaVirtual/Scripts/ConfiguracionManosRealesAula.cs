using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

// Marcador y referencias para instalación/restauración. No conduce tracking ni eventos.
public sealed class ConfiguracionManosRealesAula : MonoBehaviour
{
    public const string Id="ManosReales_Aula_v1";
    public XRInputModalityManager gestorModalidad;
    public GameObject manoIzquierda;
    public GameObject manoDerecha;
    public GameObject izquierdaAnterior;
    public GameObject derechaAnterior;
}
