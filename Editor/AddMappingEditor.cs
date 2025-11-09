 using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AddMappingEditor : EditorWindow
{
    public event Action<string> OnApplyMappings;
    public FolderNode TargetNode { get; set; }

    // List of pre-defined extensions for the user to map to folders. 
    private List<string> extensions = new List<string>()
    {
        // Textures
        ".png",
        ".jpg",
        ".jpeg",
        ".tga",
        ".psd", // Photoshop File
        ".tiff",
        ".bmp",
        ".gif",
        ".exr", // High Dynamic Range
        ".hdr", // High Dynamic Range

        // Models
        ".fbx",
        ".obj",
        ".blend", 
        ".max",
        ".ma",
        ".mb",
        

        // Materials
        ".mat",
        ".physicsmaterial", 

        // Audio
        ".wav",
        ".mp3",
        ".ogg",
        ".aif", // AIFF Audio

        // Code / Shaders
        ".cs", // C# Script
        ".asmdef", // Assembly Definition
        ".shader", // Unity ShaderLab
        ".cginc", // Shader Include
        ".hlsl", // HLSL Shader File
        ".compute", // Compute Shader

        // Unity Specific Assets
        ".prefab",
        ".unity", // Scene file
        ".asset", // Used for ScriptableObjects, Animation Clips, etc. (Very Generic!)
        ".anim", // Animation Clip (often saved as .asset too)
        ".controller", // Animator Controller
        ".overridecontroller", // Animator Override Controller
        ".mask", // Avatar Mask
        ".rendertexture",
        ".cubemap",
        ".preset", // Unity Preset Asset

        // Fonts
        ".ttf",
        ".otf",

        // Data / Text
        ".txt",
        ".json",
        ".xml",
        ".csv",
        ".bytes", 

        // Video
        ".mp4",
        ".mov",
        ".webm"

    };

    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ExtensionMappingsEditor.uxml");
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        extensions.Sort();

        var predefinedExtensionsList = rootVisualElement.Q<ListView>("ExtensionsListView");
        if (predefinedExtensionsList != null)
        {
            predefinedExtensionsList.itemsSource = extensions;

            predefinedExtensionsList.makeItem = () => new Label();

            predefinedExtensionsList.bindItem = (element, index) =>
            {

                var label = element as Label;
                if (label != null && predefinedExtensionsList.itemsSource != null && index >= 0 && index < predefinedExtensionsList.itemsSource.Count)
                {
                    label.text = predefinedExtensionsList.itemsSource[index] as string ?? "Invalid Data";

                }
                else
                {
                    Debug.LogWarning("error in binding items to label in add mapping window ");
                    return;
                }
            };
        }
        else
        {
            Debug.LogWarning("ListView for extensions not found");
            return ;
        }

        var applyButton = rootVisualElement.Q<Button>("ApplyMappingButton");
        var newMapTextField = rootVisualElement.Q<TextField>("CreateMappingTextField");
        if (applyButton == null)
        {
            Debug.LogWarning("Apply button can not be found ");
            return;

        }

        applyButton.clicked += () =>
        {
            var customExtension = newMapTextField.text;
            var selectedExtension = predefinedExtensionsList.selectedItem as string;
            if (selectedExtension == null && string.IsNullOrWhiteSpace(customExtension))
            {
                Debug.LogWarning("Need to select or enter an extension");
                return;
            }

            string extensionToApply = null;
            if (!string.IsNullOrWhiteSpace(customExtension))
            {
                bool isCustomValid = true;
                string finalCustomExtension = customExtension; 

                if (!finalCustomExtension.StartsWith("."))
                {
                    Debug.LogWarning($"Custom extension '{finalCustomExtension}' missing leading period. Adding '.' automatically.");
                    finalCustomExtension = "." + finalCustomExtension;
                }

                if (finalCustomExtension.Length > 1 && finalCustomExtension.Substring(1).IndexOfAny(FolderStructureManager.invalidFileNameChars) != -1)
                {
                    EditorUtility.DisplayDialog("Invalid Characters", $"Custom extension '{finalCustomExtension}' contains invalid filename characters.", "OK");
                    isCustomValid = false;
                    
                }

                if (finalCustomExtension.Length < 2)
                {
                    EditorUtility.DisplayDialog("Invalid Custom Input", "Custom extension must be at least one character after the period.", "OK");
                    isCustomValid = false;
                }

               if (isCustomValid)
                {
                    extensionToApply = finalCustomExtension;
                }
            }

            if (string.IsNullOrWhiteSpace(extensionToApply))
            {
                if (selectedExtension != null)
                {
                    extensionToApply = selectedExtension;
                }
            }
           

            OnApplyMappings?.Invoke(extensionToApply);

            this.Close();
        };
    }
}
