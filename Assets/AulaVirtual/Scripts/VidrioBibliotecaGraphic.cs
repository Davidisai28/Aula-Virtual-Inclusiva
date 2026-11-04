using UnityEngine;
using UnityEngine.UI;

// Vidrio estilizado mediante geometría uGUI: sin blur, cámaras ni texturas auxiliares.
[AddComponentMenu("")]
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class VidrioBibliotecaGraphic : MaskableGraphic
{
#if UNITY_EDITOR
    static readonly System.Reflection.FieldInfo RendererCache = typeof(Graphic).GetField("m_CanvasRenderer",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    // Graphic conserva esta referencia no serializada. Undo puede recrear el
    // componente nativo dejando vivo el wrapper administrado del renderer anterior.
    protected override void OnEnable()
    {
        RestablecerRenderer(this);
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        RestablecerRenderer(this);
        base.OnDisable();
    }

    protected override void OnValidate()
    {
        RestablecerRenderer(this);
        base.OnValidate();
    }

    public static bool RestablecerRenderer(Graphic graphic)
    {
        if (RendererCache == null) return false;
        var actual = graphic.GetComponent<CanvasRenderer>();
        if (actual == null) return false;
        RendererCache.SetValue(graphic, actual);
        return true;
    }
#endif

    [Min(0)] public float radio = 24;
    public Color colorInferior = new Color(.04f, .09f, .16f, 1);
    [Min(0)] public float grosorBorde;
    public Color colorBorde = new Color(.6f, .9f, .95f, .5f);
    public bool vidrioAnalitico;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (vidrioAnalitico && material != null && material.shader != null && material.shader.name == "AulaVirtual/UI/LiquidGlass")
        {
            Vector2[] corners = { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
            for (int i=0; i<4; i++)
            {
                var v = UIVertex.simpleVert;
                v.position = new Vector3(Mathf.Lerp(r.xMin,r.xMax,corners[i].x),Mathf.Lerp(r.yMin,r.yMax,corners[i].y));
                v.color=color; v.uv0=corners[i];
                v.uv1=new Vector4(r.width,r.height,radio,grosorBorde);
                v.uv2=colorInferior; v.uv3=colorBorde;
                vh.AddVert(v);
            }
            vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,0);
            return;
        }
        float radius = Mathf.Min(radio, Mathf.Min(r.width, r.height) * .5f);
        Add(vh, r.center, r);
        const int steps = 10;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
                corner < 2 ? r.yMax - radius : r.yMin + radius);
            for (int i = 0; i <= steps; i++)
            {
                float a = (corner * 90 + i * 90f / steps) * Mathf.Deg2Rad;
                Add(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, r);
            }
        }
        int count = 4 * (steps + 1);
        for (int i = 1; i <= count; i++) vh.AddTriangle(0, i, i == count ? 1 : i + 1);
        if (grosorBorde <= 0) return;
        float b = Mathf.Min(grosorBorde, Mathf.Min(r.width, r.height) * .5f);
        Rect inner = new Rect(r.xMin + b, r.yMin + b, r.width - 2 * b, r.height - 2 * b);
        int offset = vh.currentVertCount;
        for (int i = 0; i < count; i++)
        {
            int corner = i / (steps + 1);
            float angle = (corner * 90 + (i % (steps + 1)) * 90f / steps) * Mathf.Deg2Rad;
            vh.AddVert(Perimetro(r, radius, corner, angle), colorBorde, Vector2.zero);
            vh.AddVert(Perimetro(inner, Mathf.Max(0, radius - b), corner, angle), colorBorde, Vector2.zero);
        }
        for (int i = 0; i < count; i++)
        {
            int a = offset + 2 * i, next = offset + 2 * ((i + 1) % count);
            vh.AddTriangle(a, next, a + 1); vh.AddTriangle(a + 1, next, next + 1);
        }
    }

    static Vector2 Perimetro(Rect r, float radius, int corner, float angle)
    {
        var center = new Vector2(corner == 0 || corner == 3 ? r.xMax - radius : r.xMin + radius,
            corner < 2 ? r.yMax - radius : r.yMin + radius);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    void Add(VertexHelper vh, Vector2 point, Rect r)
    {
        Color tint = Color.Lerp(colorInferior, color, Mathf.InverseLerp(r.yMin, r.yMax, point.y));
        vh.AddVert(point, tint, Vector2.zero);
    }
}
