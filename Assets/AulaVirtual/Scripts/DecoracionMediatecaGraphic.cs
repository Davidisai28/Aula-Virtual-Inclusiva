using UnityEngine;
using UnityEngine.UI;

// Decoración vectorial ligera: no descarga miniaturas ni depende de glifos de iconos.
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class DecoracionMediatecaGraphic : MaskableGraphic
{
    public enum Forma { Ondas, Play, Actualizar, Cine, Academia, Descargar, Pausa, Reiniciar, Detener }
    public Forma forma;
    public int variante;
#if UNITY_EDITOR
    protected override void OnEnable() { VidrioBibliotecaGraphic.RestablecerRenderer(this); base.OnEnable(); }
    protected override void OnDisable() { VidrioBibliotecaGraphic.RestablecerRenderer(this); base.OnDisable(); }
    protected override void OnValidate() { VidrioBibliotecaGraphic.RestablecerRenderer(this); base.OnValidate(); }
#endif
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        if (forma == Forma.Ondas)
        {
            for (int layer = 0; layer < 3; layer++)
            {
                Color top = new Color(.08f, .9f, .8f, .045f + layer * .025f);
                float lastX = r.xMin + 8, lastY = Onda(0, layer, r);
                for (int i = 1; i <= 48; i++)
                {
                    float t = i / 48f, x = Mathf.Lerp(r.xMin + 8, r.xMax - 8, t), y = Onda(t, layer, r);
                    int start = vh.currentVertCount;
                    var clear = new Color(top.r,top.g,top.b,0);
                    vh.AddVert(new Vector2(lastX,r.yMin+8),clear,Vector2.zero);
                    vh.AddVert(new Vector2(x,r.yMin+8),clear,Vector2.zero);
                    vh.AddVert(new Vector2(x,y),top,Vector2.zero);
                    vh.AddVert(new Vector2(lastX,lastY),top,Vector2.zero);
                    vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
                    Linea(vh, new Vector2(lastX, lastY), new Vector2(x, y), 2.2f, new Color(.4f, 1, .9f, .13f));
                    lastX = x; lastY = y;
                }
            }
            return;
        }
        Vector2 P(float x, float y) => new Vector2(r.xMin + x * r.width, r.yMin + y * r.height);
        void L(float x, float y, float xx, float yy) => Linea(vh, P(x,y), P(xx,yy), 2.8f, color);
        if (forma == Forma.Play)
        {
            int a = vh.currentVertCount;
            vh.AddVert(P(.27f,.18f), color, Vector2.zero); vh.AddVert(P(.27f,.82f), color, Vector2.zero);
            vh.AddVert(P(.82f,.5f), color, Vector2.zero); vh.AddTriangle(a,a+1,a+2);
        }
        else if (forma == Forma.Pausa)
        { Quad(vh,P(.23f,.18f),P(.4f,.18f),P(.4f,.82f),P(.23f,.82f),color); Quad(vh,P(.6f,.18f),P(.77f,.18f),P(.77f,.82f),P(.6f,.82f),color); }
        else if (forma == Forma.Detener)
        { Quad(vh,P(.2f,.2f),P(.8f,.2f),P(.8f,.8f),P(.2f,.8f),color); }
        else if (forma == Forma.Reiniciar)
        {
            for (int i=0; i<29; i++)
            {
                float a=(45+i*10)*Mathf.Deg2Rad,b=a+10*Mathf.Deg2Rad;
                Linea(vh,P(.5f+.34f*Mathf.Cos(a),.5f+.34f*Mathf.Sin(a)),P(.5f+.34f*Mathf.Cos(b),.5f+.34f*Mathf.Sin(b)),3,color);
            }
            L(.72f,.76f,.94f,.78f); L(.72f,.76f,.74f,.98f);
        }
        else if (forma == Forma.Actualizar)
        {
            for (int i = 0; i < 28; i++)
            {
                float a = (40 + i * 10) * Mathf.Deg2Rad, b = a + 10 * Mathf.Deg2Rad;
                Linea(vh, P(.5f + .35f*Mathf.Cos(a), .5f + .35f*Mathf.Sin(a)),
                    P(.5f + .35f*Mathf.Cos(b), .5f + .35f*Mathf.Sin(b)), 3, color);
            }
            L(.76f,.77f,.8f,.98f); L(.76f,.77f,.55f,.78f);
        }
        else if (forma == Forma.Cine)
        {
            L(.12f,.4f,.12f,.86f); L(.12f,.86f,.88f,.86f); L(.88f,.86f,.88f,.4f);
            for(int i=0;i<3;i++) { float x=.05f+i*.32f; L(x,.04f,x,.3f); L(x,.3f,x+.24f,.3f); L(x+.24f,.3f,x+.24f,.04f); }
        }
        else if (forma == Forma.Academia)
        {
            L(.05f,.63f,.5f,.88f); L(.5f,.88f,.95f,.63f); L(.95f,.63f,.5f,.38f); L(.5f,.38f,.05f,.63f);
            L(.22f,.52f,.22f,.24f); L(.22f,.24f,.5f,.12f); L(.5f,.12f,.78f,.24f); L(.78f,.24f,.78f,.52f); L(.94f,.6f,.94f,.27f);
        }
        else { L(.5f,.9f,.5f,.3f); L(.22f,.55f,.5f,.27f); L(.5f,.27f,.78f,.55f); L(.15f,.2f,.15f,.08f); L(.15f,.08f,.85f,.08f); L(.85f,.08f,.85f,.2f); }
    }
    float Onda(float t, int layer, Rect r) => r.yMin + r.height *
        (.32f + layer * .13f + .13f * Mathf.Sin(t * 6.2f + layer * 1.4f + variante));
    static void Linea(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
    {
        Vector2 n = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f;
        Quad(vh,a-n,b-n,b+n,a+n,c);
    }
    static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int i=vh.currentVertCount;
        vh.AddVert(a,tint,Vector2.zero); vh.AddVert(b,tint,Vector2.zero);
        vh.AddVert(c,tint,Vector2.zero); vh.AddVert(d,tint,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
}
