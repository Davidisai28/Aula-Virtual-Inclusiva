using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Solo valida un build solicitado por el usuario. No crea APK ni modifica la escena.
public sealed class ValidarBackendAndroid : IPreprocessBuildWithReport
{
    [Serializable] class Datos { public string urlRemota; }
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        var text = Resources.Load<TextAsset>("BibliotecaBackend");
        var datos = text != null ? JsonUtility.FromJson<Datos>(text.text) : null;
        if (datos == null || !Uri.TryCreate(datos.urlRemota, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.IsLoopback || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new BuildFailedException("Configura urlRemota con la URL HTTPS real del backend en BibliotecaBackend.json antes de generar Android. No se acepta localhost ni HTTP sin cifrar.");
    }
}
