using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class FuenteMediatecaEditor
{
    public static TMP_FontAsset Obtener()
    {
        const string path = "Assets/AulaVirtual/Fonts/MediatecaSDF.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null && asset.HasCharacter('\u2026')) return asset;
        if (asset == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null) { Debug.LogError("Falta la fuente LiberationSans.ttf de TMP."); return null; }
            if (!AssetDatabase.IsValidFolder("Assets/AulaVirtual/Fonts")) AssetDatabase.CreateFolder("Assets/AulaVirtual", "Fonts");
            asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) return null;
            asset.name = "MediatecaSDF";
            AssetDatabase.CreateAsset(asset,path);
        }
        var chars = new StringBuilder();
        for (int c=32; c<=126; c++) chars.Append((char)c);
        chars.Append("áéíóúÁÉÍÓÚñÑüÜ¿¡…·–—“”‘’");
        asset.TryAddCharacters(chars.ToString(),out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Mediateca: caracteres no disponibles: " + missing,asset);
        foreach(var texture in asset.atlasTextures)
            if(texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture,asset);
        if(asset.material != null && !AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material,asset);
        EditorUtility.SetDirty(asset);
        foreach(var texture in asset.atlasTextures) if(texture != null) EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssetIfDirty(asset);
        if (!asset.HasCharacter('\u2026'))
        { Debug.LogError("No se pudo añadir el glifo de elipsis. Se conserva la interfaz anterior."); return null; }
        return asset;
    }
}
