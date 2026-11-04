
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class ControladorVideoXR : MonoBehaviour
{
    private VideoPlayer reproductor;

    private void Awake()
    {
        reproductor = GetComponent<VideoPlayer>();
    }

    public void Reproducir()
    {
        reproductor.Play();
        Debug.Log("Video reproduciéndose");
    }

    public void Pausar()
    {
        reproductor.Pause();
        Debug.Log("Video pausado");
    }

    public void Reiniciar()
    {
        reproductor.Stop();
        reproductor.Play();
        Debug.Log("Video reiniciado");
    }

    public void Detener()
    {
        reproductor.Stop();
        Debug.Log("Video detenido");
    }
}
