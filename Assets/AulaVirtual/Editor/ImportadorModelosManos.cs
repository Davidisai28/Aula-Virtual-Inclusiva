using UnityEditor;

// Solo los dos modelos nuevos, sin tocar samples ni paquetes instalados.
public sealed class ImportadorModelosManos : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/AulaVirtual/Models/ControllerHands/") || !assetPath.EndsWith(".fbx")) return;
        var importer=(ModelImporter)assetImporter;
        importer.animationType=ModelImporterAnimationType.Generic;
        importer.optimizeGameObjects=false;
        importer.preserveHierarchy=true;
        importer.importAnimation=false;
        importer.importCameras=false; importer.importLights=false;
        importer.addCollider=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.globalScale=1; importer.useFileScale=true;
    }
}
