using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetImportOrganiser : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            // Exclude Files in Editor folder
            if (assetPath.StartsWith("Packages/") || assetPath.StartsWith("Assets/Editor/"))
            {
                continue;
            }
            
            // Get file extension of processed assets
            string extension = Path.GetExtension(assetPath)?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(extension))
            {
                string destinationFolder = FolderStructureManager.FindTargetPathForExtension( extension, FolderStructureManager.DefaultFolderStructure);

                if (!string.IsNullOrEmpty(destinationFolder))
                {
                    // Schedule moving asset to target path 
                    string currentAssetPath = assetPath; 
                    EditorApplication.delayCall += () => MoveAssetToFolder(currentAssetPath, destinationFolder);
                }
                else
                {
                    Debug.Log($"No defined folder found in structure for extension: {extension} (Asset: {assetPath})");
                }              
            }          
        }
    }

    private static void MoveAssetToFolder(string assetPath, string destinationFolder)
    {
       
        if (!File.Exists(assetPath))
        {
            return; 
        }

        //  Create paths normalised to Unity path structure
        string fileName = Path.GetFileName(assetPath);
        string targetPath = Path.Combine(destinationFolder, fileName).Replace('\\', '/');

        try
        {
            string normalizedAssetPath = Path.GetFullPath(assetPath).Replace('\\', '/');
            string normalizedTargetPath = Path.GetFullPath(targetPath).Replace('\\', '/');

            if (normalizedAssetPath.Equals(normalizedTargetPath, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Asset Organiser: Asset '{fileName}' is already in the correct folder '{destinationFolder}'. No move needed."); 
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Asset Organiser: Error normalizing paths for '{assetPath}' and '{targetPath}'. Skipping move. Error: {ex.Message}");
            return; 
        }

        // Handle event of identical file paths
        if (File.Exists(targetPath))
        {
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
            Debug.LogWarning($"Asset already exists at '{targetPath}'. Moving to '{uniquePath}' instead.");
            targetPath = uniquePath;
        }

        // Move asset from its current path to the newly defined path
        string moveResult = AssetDatabase.MoveAsset(assetPath, targetPath);

        if (string.IsNullOrEmpty(moveResult))
        {
            Debug.Log($"Asset moved to: {targetPath}");
        }
        else
        {
            Debug.LogError($"Asset move failed for '{assetPath}' to '{targetPath}'. Error: {moveResult}");
        }

    }
}
