using System;
using UnityEngine;

// Una sola configuración pública. Nunca contiene claves ni credenciales.
public static class ConfiguracionBackendAula
{
    [Serializable] class Datos
    {
        public string entornoEditor = "Local";
        public string urlLocal = "http://127.0.0.1:3000";
        public string urlRemota = "";
    }
    static Datos datos;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => datos = null;
    public static bool IntentarObtener(string legado, out string url, out string error)
    {
        url = ""; error = null;
        if (datos == null)
        {
            var texto = Resources.Load<TextAsset>("BibliotecaBackend");
            try { datos = texto != null ? JsonUtility.FromJson<Datos>(texto.text) : new Datos { urlLocal = legado }; }
            catch (ArgumentException) { error = "Configuración del servidor inválida"; return false; }
            if (datos == null) { error = "Falta la configuración del servidor"; return false; }
        }
        bool remoto = !Application.isEditor || string.Equals(datos.entornoEditor, "Remoto", StringComparison.OrdinalIgnoreCase);
        url = (remoto ? datos.urlRemota : datos.urlLocal)?.Trim().TrimEnd('/') ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            (uri.Scheme != "https" && (remoto || uri.Scheme != "http")) || (remoto && uri.IsLoopback))
        { error = remoto ? "Configura la URL HTTPS real del servidor publicado" : "Dirección local del servidor inválida"; url = ""; return false; }
        return true;
    }
    public static string Obtener(string legado) => IntentarObtener(legado, out var url, out _) ? url : "";
}
