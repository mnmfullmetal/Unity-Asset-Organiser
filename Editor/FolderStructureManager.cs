using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;

public static class FolderStructureManager 
{
    public const string LastAppliedPresetPrefKey = "AssetOrganiser_LastAppliedPresetName";

    // Root save directory path string for saved presets.
    public static string PresetSaveDirectory => "ProjectSettings/AssetOrganiserPresets";

    // Array of invalid characters when working with file names and paths. 
    public static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

    //Default folder structure following unity project organisation best practices.
    public static List<FolderNode> DefaultFolderStructure { get; private set; } = new List<FolderNode>()
    {
        new FolderNode { displayName = "Assets", path = "Assets/", children = new List<FolderNode>()
        {
              new FolderNode { displayName = "Art", path = "Assets/Art",  children = new List<FolderNode>()
              {
                  new FolderNode { displayName = "Materials", path = "Assets/Art/Materials",  associatedExtensions = new List<string> { ".mat" } },
                  new FolderNode { displayName = "Models", path = "Assets/Art/Models",  associatedExtensions = new List<string> { ".fbx", ".obj" }},
                  new FolderNode { displayName = "Textures", path = "Assets/Art/Textures", associatedExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff" }  }
              }},
              new FolderNode { displayName = "Audio", path = "Assets/Audio", children= new List<FolderNode>()
              {
                  new FolderNode {displayName ="Music", path = "Assets/Audio/Music",  associatedExtensions = new List<string> { ".mp3", ".ogg" }},
                  new FolderNode {displayName ="Sound", path = "Assets/Audio/Sound", associatedExtensions = new List<string> { ".wav" } }
              }},
              new FolderNode { displayName = "Code", path = "Assets/Code" , children = new List < FolderNode >()
              {
                  new FolderNode {displayName ="Scripts", path = "Assets/Code/Scripts", associatedExtensions = new List<string> { ".cs" }},
                  new FolderNode {displayName = "Shaders", path = "Assets/Code/Shaders", associatedExtensions = new List<string> { ".shader", ".cginc" }}
              }},
              new FolderNode { displayName = "Docs", path = "Assets/Docs" },
              new FolderNode { displayName = "Level", path = "Assets/Level", children = new List < FolderNode >()
              {
                  new FolderNode {displayName = "Prefabs", path = "Assets/Level/Prefabs", associatedExtensions = new List<string> { ".prefab" }},
                  new FolderNode {displayName = "Scenes", path = "Assets/Level/Scenes", associatedExtensions = new List<string> { ".unity" }},
                  new FolderNode {displayName = "UI", path = "Assets/Level/UI"},

              }}
        }
       }
    };

    // Called when editor first loads
    [InitializeOnLoadMethod]
    static void Initialise()
    {
        try
        {
            if (!string.IsNullOrEmpty(PresetSaveDirectory))
            {
                Directory.CreateDirectory(PresetSaveDirectory);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"AssetOrganiser: Failed to create preset directory '{PresetSaveDirectory}'. Error: {ex.Message}");
        }

        List<FolderNode> structureToApply = null;
        string presetNameToLoad = "Default"; 

        try
        {
            presetNameToLoad = EditorPrefs.GetString(LastAppliedPresetPrefKey, "Default");
        }
        catch (Exception ex)
        {
            Debug.LogError($"AssetOrganiser: Error reading EditorPrefs key '{LastAppliedPresetPrefKey}'. Applying default structure. Error: {ex.Message}");
            presetNameToLoad = "Default"; 
        }

        Debug.Log($"AssetOrganiser Init: Attempting to apply structure for '{presetNameToLoad}' based on EditorPrefs.");

        if (presetNameToLoad == "Default" || string.IsNullOrEmpty(presetNameToLoad))
        {
            structureToApply = DefaultFolderStructure;
            Debug.Log("AssetOrganiser Init: Using Default structure.");
        }
        else
        {
            string presetPath = null; 
            try
            {
                presetPath = (Path.Combine(PresetSaveDirectory, presetNameToLoad) + ".json").Replace('\\', '/');

                if (File.Exists(presetPath))
                {
                    string jsonData = File.ReadAllText(presetPath);

                    FolderNodeListWrapper wrapper = JsonUtility.FromJson<FolderNodeListWrapper>(jsonData);

                    if (wrapper != null && wrapper.RootNodes != null) 
                    {
                        structureToApply = wrapper.RootNodes; 
                        Debug.Log($"AssetOrganiser Init: Successfully loaded preset '{presetNameToLoad}'.");
                    }
                    else
                    {
                        Debug.LogWarning($"AssetOrganiser Init: Failed to parse JSON or structure was invalid in '{presetPath}'. Applying default structure instead.");
                        structureToApply = DefaultFolderStructure; 
                    }
                }
                else
                {
                    Debug.LogWarning($"AssetOrganiser Init: Last applied preset file not found: '{presetPath}'. Applying default structure instead.");
                    structureToApply = DefaultFolderStructure; 
                }
            }
            catch (Exception loadEx)
            {
                Debug.LogError($"AssetOrganiser Init: Error loading/deserializing preset '{presetPath ?? presetNameToLoad}'. Applying default structure instead. Error: {loadEx.Message}\n{loadEx.StackTrace}");
                structureToApply = DefaultFolderStructure; 
            }
        }

        if (structureToApply != null)
        {
            ApplyFolderStructure(structureToApply);
        }
        else
        {
            Debug.LogError("AssetOrganiser Init: Critical error - Could not determine any structure (not even default) to apply!");
        }
    }

    //Applies a Folder structure across unity projcect
    public static void ApplyFolderStructure(List<FolderNode> structureToApply)
    {
        if (structureToApply == null) return;

        foreach (var node in structureToApply)
        {
            EnsureFolderExists(node);
        }
        AssetDatabase.Refresh();
        Debug.Log("Folder structure verified/applied.");
    }

    // Helper function to recursively create directories for folder structure.  
    private static void EnsureFolderExists(FolderNode node)
    {
        // Ensure that the folders being created are within root folder "assets"
        if (string.IsNullOrEmpty(node.path) || !node.path.StartsWith("Assets/"))
        {
            Debug.LogWarning($"Skipping folder creation for invalid path: '{node.path}' (Node: {node.displayName})");
            return;
        }

        // Check that the folder being created does not already exist
        if (!Directory.Exists(node.path))
        {
            try
            {
                string parentPath = Path.GetDirectoryName(node.path);
                string folderName = Path.GetFileName(node.path);

                // Check if parent path and the newfolder name exist before creating
                if (!string.IsNullOrEmpty(parentPath) && !string.IsNullOrEmpty(folderName))
                {
                    // Get GUID and check if a folder was created sucessfully. 
                    string guid = AssetDatabase.CreateFolder(parentPath, folderName);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        Debug.Log($"Created folder: {node.path}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to create folder: {node.path}. AssetDatabase.CreateFolder returned empty GUID.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot determine parent path or folder name for: '{node.path}'. Skipping creation.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating folder '{node.path}': {e.Message}");
            }
        }

        // Recursively process children folders
        if (node.children != null && node.children.Count > 0)
        {
            foreach (var childNode in node.children)
            {
                EnsureFolderExists(childNode); 
            }
        }
    }

    // Find target path for file to be moved to, based on the file extension and the folder associated by that extension
    public static string FindTargetPathForExtension(string extension, List<FolderNode> nodesToSearch)
    {
        if (string.IsNullOrEmpty(extension) || nodesToSearch == null)
        {
            return null;
        }

        foreach (var node in nodesToSearch)
        {
            if (node.associatedExtensions != null && node.associatedExtensions.Contains(extension))
            {
                return node.path; 
            }

            if (node.children != null && node.children.Count > 0)
            {
                // Recursively search for target path
                string pathInChildren = FindTargetPathForExtension(extension, node.children);
                if (pathInChildren != null)
                {
                    return pathInChildren;
                }
            }
        }

        return null; 
    }

    // Deep copy method of copying FolderNodes, used to create deep copies of the folder structure
    public static FolderNode CloneFolderNode(FolderNode originalNode)
    {
        if (originalNode == null)
        {
            return null;
        }

        var copyNode = new FolderNode();
        copyNode.displayName = originalNode.displayName;
        copyNode.path = originalNode.path;

        if(originalNode.associatedExtensions == null)
        {
            copyNode.associatedExtensions = new List<string>();
        }
        else
        {
            copyNode.associatedExtensions = new List<string>(originalNode.associatedExtensions);

        }

        if(originalNode.children != null && originalNode.children.Count > 0)
        {
            foreach (var child in originalNode.children)
            {
                copyNode.children.Add(CloneFolderNode(child));
            }
        }

        return copyNode;
    }


    // Helper Function to deep copy/clone FolderNode structures
    public static List<FolderNode> DeepCloneList(List<FolderNode> originalList)
    {
        if (originalList == null) return null;

        List<FolderNode> newList = new List<FolderNode>();
        foreach (var node in originalList)
        {
            FolderNode clonedNode = FolderStructureManager.CloneFolderNode(node);
            if (clonedNode != null)
            {
                newList.Add(clonedNode);
            }
        }
        return newList;
    }

    // Search for the parent of a folder, used for deletion of nodes.
    public static FolderNode FindParentNode (List<FolderNode> nodesToSearch, FolderNode childNode)
    {
        foreach( var potentialParent in nodesToSearch)
        {
            if (potentialParent != null)
            {
                Debug.Log($"Checking if '{potentialParent.displayName}' children list contains '{childNode.displayName}'. Result: {potentialParent.children.Contains(childNode)}");

                // Check if node exists as child of the potential parent node
                if (potentialParent.children.Contains(childNode))
                {
                    return potentialParent;
                }

                // If the potential parent doesnt contain the child, check the potential parent for children
                else if (potentialParent.children.Count > 0)
                {
                    // Recursively search the children, or "subparents" of the potential parent 
                    var potentialSubParent = FindParentNode(potentialParent.children, childNode);
                    if (potentialSubParent != null)
                    {
                        return potentialSubParent;
                    }
                }
            }
          
        }
        return null;

    }

    // Helper method to check for conflictions in extension mapping.
    public static FolderNode IsExtensionAlreadyInUse(List<FolderNode> foldersToSearch,FolderNode folderToExclude, string extension)
    {
        foreach(var folder in foldersToSearch)
        {
            if (folder == null)
            {
                continue; 
            }

            if (folder == folderToExclude)
            {
                continue;
            }

            if (folder.associatedExtensions != null && folder.associatedExtensions.Contains(extension))
            {
                return folder;
            }
            else
            {
                if (folder.children != null && folder.children.Count > 0)
                {
                    var conflictingFolder = IsExtensionAlreadyInUse(folder.children, folderToExclude, extension);

                    if (conflictingFolder != null)
                    {
                        return conflictingFolder;
                    }
                }

            }
        }
        return null;
    }

}

