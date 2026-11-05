using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public sealed class GeneradorBibliotecaVideoXR : EditorWindow
{
    static GeneradorBibliotecaVideoXR()
    {
        Undo.undoRedoPerformed += ValidarDespuesDeUndo;
    }

    static void ValidarDespuesDeUndo()
    {
        // No crea interfaces. Solo resincroniza gráficos de raíces propias restauradas.
        foreach (var root in Resources.FindObjectsOfTypeAll<BibliotecaVisualAula>())
            if (root != null && root.name == BibliotecaVisualAula.Id &&
                root.transform.parent != null && root.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(root))
            {
                AsegurarRenderers(root.gameObject);
                Validar(root.gameObject);
            }
    }
    GameObject pantalla;
    static Material materialVidrio;
    static readonly Color Texto = Hex("F5F7FA"), Suave = Hex("AEBBC9"), Turquesa = Hex("6FE7DA");
    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }

    [MenuItem("Aula Virtual/Biblioteca visual/Generar o actualizar")]
    static void Abrir() => GetWindow<GeneradorBibliotecaVideoXR>("Biblioteca del aula");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Biblioteca · Diseño y prueba XR", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Biblioteca funcional: catálogo, descarga y reproducción en Play. Modos Mouse/XR en el menú Prueba Editor. Si XR ya funciona, no repitas Preparar XR. No guarda la escena principal.", MessageType.Info);
        pantalla = (GameObject)EditorGUILayout.ObjectField("Pantalla del aula", pantalla, typeof(GameObject), true);
        if (GUILayout.Button("Generar / actualizar interfaz", GUILayout.Height(36))) Generar();
        if (GUILayout.Button("Eliminar únicamente interfaz", GUILayout.Height(28))) Eliminar();
        if (GUILayout.Button("Preparar interacción XR (con respaldo)", GUILayout.Height(36)))
        {
            var target = Resolver();
            if (target != null && RespaldarEscena(target)) PreparadorBibliotecaXR.Preparar(target);
        }
        if (GUILayout.Button("Diagnosticar XR (sin cambios)", GUILayout.Height(28)))
        {
            var target = Resolver(true);
            if (target != null) PreparadorBibliotecaXR.Diagnosticar(target);
        }
        if (GUILayout.Button("Diagnosticar nitidez (sin cambios)", GUILayout.Height(28)))
        {
            var target = Resolver(true);
            if (target != null)
                foreach (var ui in Propios(target))
                {
                    var canvas = ui.GetComponent<Canvas>();
                    var camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                    if (camera == null) { Debug.LogWarning("No se encontró cámara para medir el pizarrón."); continue; }
                    var corners = new Vector3[4]; ((RectTransform)ui.transform).GetWorldCorners(corners);
                    Vector2 a = camera.WorldToScreenPoint(corners[0]), b = camera.WorldToScreenPoint(corners[3]);
                    float width = Vector2.Distance(a,b);
                    Debug.Log($"[NITIDEZ-BIBLIOTECA] Cámara {camera.pixelWidth}x{camera.pixelHeight}, render {camera.scaledPixelWidth}x{camera.scaledPixelHeight}. Pizarrón proyectado: {width:0} px de ancho; texto de 36 unidades: aprox. {width*36/1600:0.0} px de cuerpo. Revisa Game maximizado, resolución 1920x1080 y escala 1x. La medida no incluye reescalado posterior de la ventana.", ui);
                }
        }
        if (GUILayout.Button("Validar interfaz (sin cambios)", GUILayout.Height(28)))
        {
            var target = Resolver(true);
            if (target != null)
                foreach (var root in Propios(target)) Validar(root.gameObject);
        }
    }

    GameObject Resolver(bool permitirPlay = false)
    {
        if (Application.isPlaying && !permitirPlay) { Debug.LogWarning("Genera la biblioteca fuera de Play."); return null; }
        if (pantalla == null) pantalla = GameObject.Find("PantallaVideo_Aula");
        if (pantalla == null || !pantalla.scene.IsValid() || EditorUtility.IsPersistent(pantalla))
        { Debug.LogWarning("Abre AulaPrincipal_GLB y asigna PantallaVideo_Aula de la escena."); return null; }
        if (pantalla.GetComponent<MeshRenderer>() == null)
        { Debug.LogWarning("La pantalla debe tener MeshRenderer."); return null; }
        var scale = pantalla.transform.lossyScale;
        if (Mathf.Abs(scale.x) < .00001f || Mathf.Abs(scale.y) < .00001f || Mathf.Abs(scale.z) < .00001f)
        { Debug.LogError("La pantalla tiene un eje de escala cero. No se modificó la interfaz."); return null; }
        return pantalla;
    }

    void BorrarPropios(GameObject target)
    {
        foreach (var item in Propios(target))
            {
                // Versiones anteriores podían carecer del renderer. Reparar antes de
                // OnDisable permite que RectMask2D retire sus clippables con seguridad.
                // La instantánea de DestroyObjectImmediate conserva esta reparación al deshacer.
                AsegurarRenderers(item.gameObject);
                Validar(item.gameObject);
                Undo.RegisterFullObjectHierarchyUndo(item.gameObject, "Desactivar biblioteca visual");
                item.gameObject.SetActive(false);
                Undo.DestroyObjectImmediate(item.gameObject);
            }
    }

    static BibliotecaVisualAula[] Propios(GameObject target)
    {
        return Array.FindAll(target.GetComponentsInChildren<BibliotecaVisualAula>(true),
            item => item.transform.parent == target.transform && item.name == BibliotecaVisualAula.Id);
    }

    static string Ruta(Transform item)
    {
        string path = item.name;
        while (item.parent != null) { item = item.parent; path = item.name + "/" + path; }
        return item.gameObject.scene.path + ":/" + path;
    }

    static bool Validar(GameObject root)
    {
        bool valid = true;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            var actual = graphic.GetComponent<CanvasRenderer>();
            // No usar graphic.canvasRenderer: su getter crea componentes y oculta el fallo.
            var field = typeof(Graphic).GetField("m_CanvasRenderer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var cached = field == null ? null : field.GetValue(graphic);
            if (actual == null || (cached != null && !ReferenceEquals(cached, actual)))
            {
                Debug.LogError("Biblioteca UI: " + (actual == null ? "CanvasRenderer ausente" : "referencia CanvasRenderer obsoleta") +
                    " en " + Ruta(graphic.transform), graphic);
                valid = false;
            }
        }
        var canvasRect = root.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            var corners = new Vector3[4];
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == canvasRect) continue;
                rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var local = canvasRect.InverseTransformPoint(corner);
                    if (local.x < canvasRect.rect.xMin - .5f || local.x > canvasRect.rect.xMax + .5f ||
                        local.y < canvasRect.rect.yMin - .5f || local.y > canvasRect.rect.yMax + .5f)
                    {
                        Debug.LogError("Elemento fuera del pizarrón: " + Ruta(rect), rect);
                        valid = false; break;
                    }
                }
            }
        }
        var fondo = root.transform.Find("FondoAzul") as RectTransform;
        if (fondo != null)
        {
            string[] names = { "Etiqueta", "Titulo", "Subtitulo", "Actualizar", "EstadoCatalogo", "ListaVideos",
                "TotalVideos", "Anterior", "IndicadorPagina", "Siguiente", "EntrarAlCine" };
            for (int i=0; i<names.Length; i++)
            {
                var a = fondo.Find(names[i]) as RectTransform;
                if (a == null) continue;
                for (int j=i+1; j<names.Length; j++)
                {
                    var b = fondo.Find(names[j]) as RectTransform;
                    if (b == null) continue;
                    Rect ra = RectLocal(a,fondo), rb = RectLocal(b,fondo);
                    if (Mathf.Min(ra.xMax,rb.xMax)-Mathf.Max(ra.xMin,rb.xMin) > .5f &&
                        Mathf.Min(ra.yMax,rb.yMax)-Mathf.Max(ra.yMin,rb.yMin) > .5f)
                    { Debug.LogError("Superposición UI: " + Ruta(a) + " / " + b.name, a); valid=false; }
                }
            }
        }
        if (valid) Debug.Log("Biblioteca UI válida: renderers y límites del pizarrón (incluye objetos inactivos): " + Ruta(root.transform), root);
        return valid;
    }

    static Rect RectLocal(RectTransform rect, RectTransform basis)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity,float.PositiveInfinity), max = -min;
        foreach(var corner in corners)
        { Vector2 p = basis.InverseTransformPoint(corner); min=Vector2.Min(min,p); max=Vector2.Max(max,p); }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    static void AsegurarRenderers(GameObject root)
    {
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.TryGetComponent<CanvasRenderer>(out _))
                graphic.gameObject.AddComponent<CanvasRenderer>();
            // Añadir un renderer no invalida el wrapper destruido cacheado en Graphic.
            if (!VidrioBibliotecaGraphic.RestablecerRenderer(graphic))
                throw new InvalidOperationException("No se pudo resincronizar Graphic en " + Ruta(graphic.transform));
            var renderer = graphic.canvasRenderer;
            if (renderer == null)
                throw new InvalidOperationException("Falta CanvasRenderer en " + graphic.name);
        }
    }

    static bool RespaldarEscena(GameObject target)
    {
        if (string.IsNullOrEmpty(target.scene.path))
        {
            Debug.LogError("Guarda primero la escena para poder crear su copia de seguridad.");
            return false;
        }
        // saveAsCopy conserva también los cambios sin guardar, sin sobrescribir el original.
        string folder = "Assets/AulaVirtual/Scenes/BackupsBibliotecaVisual";
        Directory.CreateDirectory(folder);
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" +
            target.scene.name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
        bool dirty = target.scene.isDirty;
        bool saved = EditorSceneManager.SaveScene(target.scene, path, true);
        if (dirty) EditorSceneManager.MarkSceneDirty(target.scene);
        if (!saved) Debug.LogError("No se pudo respaldar la escena. Operación cancelada.");
        else Debug.Log("Copia previa de la escena: " + path);
        return saved;
    }

    void Eliminar()
    {
        var target = Resolver(); if (target == null) return;
        if (!RespaldarEscena(target)) return;
        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Eliminar biblioteca visual");
        int group = Undo.GetCurrentGroup();
        BorrarPropios(target); EditorSceneManager.MarkSceneDirty(target.scene);
        Undo.CollapseUndoOperations(group);
    }

    void Generar()
    {
        var target = Resolver(); if (target == null) return;
        if (!RespaldarEscena(target)) return;
        var font = FuenteMediatecaEditor.Obtener();
        if (font == null) return;
        var shader = Shader.Find("AulaVirtual/UI/LiquidGlass");
        if (shader == null) { Debug.LogError("Espera a que Unity importe UI_LiquidGlassAula.shader antes de regenerar."); return; }
        const string materialPath = "Assets/AulaVirtual/Materials/MAT_UI_LiquidGlassAula.mat";
        materialVidrio = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (materialVidrio == null)
        {
            materialVidrio = new Material(shader) { name = "MAT_UI_LiquidGlassAula" };
            AssetDatabase.CreateAsset(materialVidrio, materialPath);
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generar biblioteca visual");
        var anteriores = Propios(target);
        var catalogoAnterior = anteriores.Length == 1 ? anteriores[0].catalogoDrive : null;
        var cacheAnterior = anteriores.Length == 1 ? anteriores[0].gestorCache : null;
        var cineAnterior = anteriores.Length == 1 ? anteriores[0].gestorCine : null;
        var camaraAnterior = anteriores.Length == 1 ? anteriores[0].GetComponent<Canvas>().worldCamera : null;
        BorrarPropios(target);
        var root = new GameObject(BibliotecaVisualAula.Id, typeof(RectTransform), typeof(Canvas), typeof(BibliotecaVisualAula));
        // No registrar gráficos en RectMask2D hasta completar todos sus componentes.
        root.SetActive(false);
        root.transform.SetParent(target.transform, false);
        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(1600, 900);
        var inherited = target.transform.lossyScale;
        var mesh = target.GetComponent<MeshFilter>();
        var bounds = mesh != null && mesh.sharedMesh != null ? mesh.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        float density = Mathf.Min(Mathf.Abs(inherited.x) * bounds.size.x / 1600f,
            Mathf.Abs(inherited.y) * bounds.size.y / 900f);
        // Escala mundial uniforme: evita estirar los glifos al heredar el Quad no uniforme.
        rt.localScale = new Vector3(density / Mathf.Abs(inherited.x), density / Mathf.Abs(inherited.y), density / Mathf.Abs(inherited.z));
        rt.localPosition = bounds.center + new Vector3(0, 0, -.008f / Mathf.Abs(inherited.z));
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        root.GetComponent<Canvas>().additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2;
        scaler.referencePixelsPerUnit = 100;
        var raycaster = root.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        raycaster.checkFor3DOcclusion = true;
        var refs = root.GetComponent<BibliotecaVisualAula>();
        var fondo = Box(rt, "FondoAzul", 0, 0, 1600, 900);
        // La base opaca cubre también las esquinas: el video no se filtra por los bordes.
        Glass(fondo, "BaseOpaca", 0, 0, 1600, 900, Hex("161A20"), Hex("161A20"), 0);
        Glass(fondo, "PanelAhumado", 12, 12, 1576, 876, Hex("1F232A"), Hex("161A20"), 32);
        Label(fondo, "Etiqueta", "Aula Virtual / Mediateca", 64, 28, 1072, 48, 36, Suave, font);
        Label(fondo, "Titulo", "Biblioteca de videos", 64, 88, 1072, 84, 68, Texto, font).fontStyle = FontStyles.Bold;
        Label(fondo, "Subtitulo", "Selecciona un video para reproducirlo", 64, 180, 1072, 50, 40, Hex("D7DEE7"), font);
        refs.actualizar = Boton(fondo, "Actualizar", "Actualizar", 1200, 88, 336, 96, false, font);
        refs.estadoCatalogo = Label(fondo, "EstadoCatalogo", "Pulsa Actualizar para consultar", 64, 240, 1472, 50, 40, Suave, font);
        // Paginación real: sin ScrollRect ni máscara que corte títulos o botones.
        var content = Box(fondo, "ListaVideos", 64, 304, 1472, 456);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        refs.contenido = content;
        var template = Fila(content, "PlantillaFilaVideo", "Nombre del video", "Tamaño · Disponible", "Ver video", false, false, font);
        template.gameObject.AddComponent<FilaCatalogoVideoAula>();
        template.gameObject.SetActive(false); refs.plantillaFila = template.gameObject;
        refs.estadoSinConexion = null; // Un único mensaje de estado, sin pie redundante.
        refs.totalVideos = Label(fondo, "TotalVideos", "0 videos", 64, 800, 300, 76, 40, Suave, font);
        refs.totalVideos.alignment = TextAlignmentOptions.MidlineLeft;
        refs.anterior = Boton(fondo, "Anterior", "Anterior", 400, 800, 216, 76, false, font);
        refs.indicadorPagina = Label(fondo, "IndicadorPagina", "1 / 1", 632, 800, 168, 76, 40, Suave, font);
        refs.indicadorPagina.alignment = TextAlignmentOptions.Center;
        refs.siguiente = Boton(fondo, "Siguiente", "Siguiente", 816, 800, 240, 76, false, font);
        refs.anterior.gameObject.SetActive(false); refs.siguiente.gameObject.SetActive(false);
        refs.indicadorPagina.gameObject.SetActive(false);
        refs.entrarCine = Boton(fondo, "EntrarAlCine", "Entrar al cine", 1136, 800, 400, 76, false, font);
        PreparadorBibliotecaXR.AsignarReferencias(target, refs);
        foreach (var sceneRoot in target.scene.GetRootGameObjects())
        {
            var catalogs = sceneRoot.GetComponentsInChildren<CatalogoVideosDrive>(true);
            if (catalogs.Length > 0) { refs.catalogoDrive = catalogs[0]; break; }
        }
        if (catalogoAnterior != null) refs.catalogoDrive = catalogoAnterior;
        refs.gestorCache = cacheAnterior != null ? cacheAnterior : refs.catalogoDrive != null ? refs.catalogoDrive.gestorCache : null;
        if (cineAnterior != null) refs.gestorCine = cineAnterior;
        if (camaraAnterior != null) root.GetComponent<Canvas>().worldCamera = camaraAnterior;
        AsegurarRenderers(root);
        if (!Validar(root)) throw new InvalidOperationException("Interfaz inválida antes de activación.");
        // Undo debe recrear primero la jerarquía inactiva con TODOS sus componentes;
        // la activación es un paso posterior, también registrado.
        Undo.RegisterCreatedObjectUndo(root, "Crear biblioteca visual inactiva");
        Undo.RecordObject(root, "Activar biblioteca visual");
        root.SetActive(true);
        Undo.FlushUndoRecordObjects();
        Canvas.ForceUpdateCanvases();
        Validar(root);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Diseño Mediateca creado sin guardar la escena. Si XR ya estaba preparado, no es necesario repetir su configuración.", root);
    }

    static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
    }
    static RectTransform Glass(Transform p, string n, float x, float y, float w, float h, Color top, Color bottom, float radius)
    {
        var r = Box(p, n, x, y, w, h);
        r.gameObject.AddComponent<CanvasRenderer>();
        var g = r.gameObject.AddComponent<VidrioBibliotecaGraphic>();
        g.vidrioAnalitico = radius > 8 && materialVidrio != null;
        if (g.vidrioAnalitico) g.material = materialVidrio;
        g.grosorBorde = radius > 8 ? .8f : 0;
        g.colorBorde = new Color(1,1,1,.13f);
        g.color = top; g.colorInferior = bottom; g.radio = radius; g.raycastTarget = false; return r;
    }
    static TMP_Text Label(Transform p, string n, string value, float x, float y, float w, float h, float size, Color c, TMP_FontAsset font)
    {
        var r = Box(p, n, x, y, w, h);
        r.gameObject.AddComponent<CanvasRenderer>();
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = value; t.fontSize = size; t.color = c;
        t.enableAutoSizing = false;
        t.extraPadding = true;
        // Altura suficiente para ascenders/descenders, incluso en etiquetas inferiores.
        r.sizeDelta = new Vector2(w, Mathf.Max(h, 46));
        t.maxVisibleLines = 1; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false; return t;
    }
    static Button Boton(Transform p, string n, string text, float x, float y, float w, float h, bool primary, TMP_FontAsset font)
    {
        var r = Glass(p, n, x, y, w, h, Hex("303640"), Hex("242A33"), h*.5f);
        var graphic = r.GetComponent<VidrioBibliotecaGraphic>(); graphic.raycastTarget = true;
        graphic.colorBorde = new Color(1,1,1,.16f);
        graphic.grosorBorde = .8f;
        var b = r.gameObject.AddComponent<BotonVidrioAula>(); b.targetGraphic = graphic;
        // ColorTint multiplica el color del Graphic; no volver a aplicar el color base.
        var colors = b.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f,1.15f,1.18f,1);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.7f,.78f,.8f,1);
        colors.disabledColor = new Color(.7f,.7f,.7f,.65f);
        colors.fadeDuration = .12f; b.colors = colors;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        var textLabel = Label(r, "TextoTMP", text, 16, 0, w - 32, h, 40, Texto, font);
        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.fontStyle = FontStyles.Bold;
        return b;
    }
    static DecoracionMediatecaGraphic Icono(Transform p, string n, DecoracionMediatecaGraphic.Forma shape, float x, float y, float w, float h, Color tint)
    {
        var r = Box(p,n,x,y,w,h); r.gameObject.AddComponent<CanvasRenderer>();
        var graphic = r.gameObject.AddComponent<DecoracionMediatecaGraphic>();
        graphic.forma = shape; graphic.color = tint; graphic.raycastTarget = false;
        return graphic;
    }
    static RectTransform Fila(Transform p, string n, string title, string subtitle, string action, bool cached, bool downloading, TMP_FontAsset font)
    {
        var r = Box(p, n, 0, 0, 1472, 216);
        var element = r.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 216; element.minHeight = 216;
        Glass(r, "Sombra", 8, 8, 1456, 208, new Color(0,0,0,.18f), new Color(0,0,0,.18f), 26);
        var surface = Glass(r, "Superficie", 0, 0, 1472, 208, Hex("303640"), Hex("242A33"), 26);
        var glass = surface.GetComponent<VidrioBibliotecaGraphic>();
        var top = glass.color; top.a = .96f; glass.color = top;
        var bottom = glass.colorInferior; bottom.a = .98f; glass.colorInferior = bottom;
        var nameLabel = Label(r, "NombreTMP", title, 36, 18, 1040, 116, 48, Texto, font);
        nameLabel.fontStyle = FontStyles.Bold;
        nameLabel.maxVisibleLines = 2; nameLabel.textWrappingMode = TextWrappingModes.Normal;
        Label(r, "TamanoTMP", "—", 36, 152, 240, 50, 40, Suave, font);
        var badge = Box(r, "Insignia", 310, 152, 710, 50);
        Label(badge, "TextoEstadoTMP", "Disponible", 0, 0, 710, 50, 40, Turquesa, font);
        Label(r, "EstadoTMP", subtitle, 36, 152, 1040, 50, 40, Suave, font).gameObject.SetActive(false);
        Boton(r, "AccionVideo", action, 1136, 54, 300, 100, true, font);
        var progress = Glass(r, "ProgresoDescarga", 36, 204, 1040, 4, Hex("42494A"), Hex("42494A"), 2);
        Glass(progress, "Relleno", 0, 0, 0, 4, Turquesa, Turquesa, 2);
        progress.gameObject.SetActive(downloading);
        MiniaturaVideoAula.PrepararFila(r); return r;
    }
}
