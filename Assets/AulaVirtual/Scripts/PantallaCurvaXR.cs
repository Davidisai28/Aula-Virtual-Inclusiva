
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PantallaCurvaXR : MonoBehaviour
{
    [Header("Dimensiones de pantalla")]
    [Min(0.5f)]
    public float anchoArco = 5.6f;

    [Min(0.5f)]
    public float altura = 3.15f;

    [Min(1f)]
    public float radioCurvatura = 3.5f;

    [Range(8, 128)]
    public int segmentos = 64;

    private Mesh mallaGenerada;

    private void OnEnable()
    {
        GenerarPantalla();
    }

    [ContextMenu("Actualizar pantalla curva")]
    public void GenerarPantalla()
    {
        MeshFilter filtro = GetComponent<MeshFilter>();

        if (filtro == null)
            return;

        int cantidadVertices = (segmentos + 1) * 2;

        Vector3[] vertices =
            new Vector3[cantidadVertices];

        Vector2[] uv =
            new Vector2[cantidadVertices];

        int[] triangulos =
            new int[segmentos * 6];

        for (int i = 0; i <= segmentos; i++)
        {
            float t = (float)i / segmentos;

            float angulo =
                (t - 0.5f) * anchoArco / radioCurvatura;

            float x =
                Mathf.Sin(angulo) * radioCurvatura;

            float z =
                (Mathf.Cos(angulo) - 1f) * radioCurvatura;

            int indice = i * 2;

            vertices[indice] =
                new Vector3(x, -altura / 2f, z);

            vertices[indice + 1] =
                new Vector3(x, altura / 2f, z);

            uv[indice] = new Vector2(t, 0);
            uv[indice + 1] = new Vector2(t, 1);
        }

        for (int i = 0; i < segmentos; i++)
        {
            int v = i * 2;
            int t = i * 6;

            // Caras orientadas hacia el espectador.
            triangulos[t] = v;
            triangulos[t + 1] = v + 1;
            triangulos[t + 2] = v + 2;

            triangulos[t + 3] = v + 2;
            triangulos[t + 4] = v + 1;
            triangulos[t + 5] = v + 3;
        }

        Mesh nuevaMalla = new Mesh();
        nuevaMalla.name = "PantallaCurvaGenerada";

        nuevaMalla.vertices = vertices;
        nuevaMalla.uv = uv;
        nuevaMalla.triangles = triangulos;

        nuevaMalla.RecalculateNormals();
        nuevaMalla.RecalculateBounds();

        filtro.sharedMesh = nuevaMalla;

        if (mallaGenerada != null)
        {
            if (Application.isPlaying)
                Destroy(mallaGenerada);
            else
                DestroyImmediate(mallaGenerada);
        }

        mallaGenerada = nuevaMalla;
    }
}
