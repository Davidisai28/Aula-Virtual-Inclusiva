
using UnityEditor;
using UnityEngine;

public static class ConfigurarAPIQuest
{
    [MenuItem("Tools/Aula Virtual/Configurar API 34")]
    public static void Configurar()
    {
        PlayerSettings.Android.targetSdkVersion =
            AndroidSdkVersions.AndroidApiLevel34;

        Debug.Log(
            "Target API configurado: " +
            PlayerSettings.Android.targetSdkVersion);
    }

    [MenuItem("Tools/Aula Virtual/Verificar API Android")]
    public static void VerificarAPI()
    {
        Debug.Log(
            "Minimum API: " +
            PlayerSettings.Android.minSdkVersion +
            " | Target API: " +
            PlayerSettings.Android.targetSdkVersion);
    }
}

