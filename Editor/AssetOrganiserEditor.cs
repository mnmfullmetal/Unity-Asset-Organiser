using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;



public class AssetOrganiserEditor : EditorWindow
{

    private List<FolderNode> lastAppliedStructure = null;
    private FolderNode nodeSelectedForEdit = null;
    private List<FolderNode> workingPresetCopy;
    private ListView associatedExtensionsList;
    private DropdownField presetDropdown;
    

    [MenuItem("Window/Asset Organiser")]
    public static void DisplayWindow()
    {
        AssetOrganiserEditor wnd = GetWindow<AssetOrganiserEditor>();
        wnd.titleContent = new GUIContent("Asset Organiser");
    }

    public void CreateGUI()
    {
        // Load and Instantiate uxml for main tool window
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/AssetOrganiserEditor.uxml");
        VisualElement root = visualTree.Instantiate();
        rootVisualElement.Add(root);

        // Query for the TreeView element that will represent the folder structure
        var folderTree = rootVisualElement.Q<TreeView>("FolderStructure");

        // Create copy of folder structure to edit and work with
        workingPresetCopy = FolderStructureManager.DeepCloneList(FolderStructureManager.DefaultFolderStructure);

        // Build the TreeViewItemData list
        List<TreeViewItemData<FolderNode>> rootItems = BuildTreeViewData(workingPresetCopy);

        // Populate the TreeView
        folderTree.SetRootItems(rootItems);
        folderTree.makeItem = () => new Label();
        folderTree.bindItem = (element, index) =>
        {
            var label = element as Label;
            label.text = (folderTree.GetItemDataForIndex<FolderNode>(index)).displayName;
        };

       // Query for the AssociatedExtensionsPanel and its elements.
        var associatedExtensionsPanel = rootVisualElement.Q<VisualElement>("AssociatedExtensionsPanel");
        associatedExtensionsList = rootVisualElement.Q<ListView>("AssociatedExtensionsList");
        var addMappingsButton = rootVisualElement.Q<Button>("AddMappingButton");
        var removeMappingsButton = rootVisualElement.Q<Button>("RemoveMappingButton");
        if (associatedExtensionsPanel != null && associatedExtensionsList != null && addMappingsButton != null && removeMappingsButton != null)
        {
            // Hide the extensions panel by default 
            associatedExtensionsPanel.style.display = DisplayStyle.None;

            // Populate the Associated Extensions list 
            associatedExtensionsList.makeItem = () => new Label();
            associatedExtensionsList.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (label != null && associatedExtensionsList.itemsSource != null && index >= 0 && index < associatedExtensionsList.itemsSource.Count)
                {
                    label.text = associatedExtensionsList.itemsSource[index] as string ?? "Invalid Data";
                }
                else if (label != null)
                {
                    label.text = "Error binding item";
                }
            };
             
            // Handle selection of folder node and display of its mapped extensions
            folderTree.selectionChanged += (evt =>
            {
                var selection = folderTree.selectedItem as FolderNode;
                if (selection == null || selection.path == "Assets/")
                {
                    associatedExtensionsPanel.style.display = DisplayStyle.None;
                }
                else
                {
                    associatedExtensionsPanel.style.display = DisplayStyle.Flex;

                    if (associatedExtensionsList != null)
                    {

                        associatedExtensionsList.itemsSource = selection.associatedExtensions ?? new List<string>();

                        associatedExtensionsList.RefreshItems();
                    }
                }
            });

            // Handle "Remove mapping" button when clicked 
            removeMappingsButton.clicked += () =>
            {
                var mappingToRemove = associatedExtensionsList.selectedItem as string;
                if (string.IsNullOrEmpty(mappingToRemove))
                {
                    Debug.LogWarning("Selected item in extension list was not a valid string.");
                    return;
                }

                var selectedFolder = folderTree.selectedItem as FolderNode;
                if (selectedFolder == null)
                {
                    Debug.LogWarning("Selected folder not valid");
                    return;
                }

                selectedFolder.associatedExtensions.Remove(mappingToRemove);
                associatedExtensionsList.RefreshItems();
            };

            // Handle "Add mapping" button when clicked
            addMappingsButton.clicked += () =>
            {
                var selectedNode = folderTree.selectedItem as FolderNode;
                if (selectedNode == null || selectedNode.path == "Assets/")
                {
                    Debug.LogWarning("Select a valid folder node first.");
                    return;
                }

                nodeSelectedForEdit = selectedNode;

                // Display "Add Mapping" window 
                AddMappingEditor wnd = GetWindow<AddMappingEditor>();
                wnd.titleContent = new GUIContent("Add Mapping");
                wnd.TargetNode = selectedNode;

                // Assign delegate function to OnApplyMappings event of the Add Mapping editor to handle the application of extension maps.
                wnd.OnApplyMappings += HandleMappingsApplied;
            };

        }



        // Query for the "Load Preset" button and its dropdownfield of saved presets. 
        var loadPresetButton = rootVisualElement.Q<Button>("LoadPresetButton");
        presetDropdown = rootVisualElement.Q<DropdownField>("LoadPresetDropdown");

        // Create dropdown choices from saved presets
        RefreshPresetDropdown();

        // Handle "Load Preset" button when clicked
        loadPresetButton.clicked += () =>
        {
            var selectedPreset = presetDropdown.value;
            if (string.IsNullOrEmpty(selectedPreset))
            {
                Debug.LogWarning("No preset selected");
                return;
            }

            // If "Default" is being loaded.
            if (selectedPreset == "Default")
            {
                // Deep copy default structure to working preset copy and rebuild folder tree. 
                workingPresetCopy = FolderStructureManager.DeepCloneList(FolderStructureManager.DefaultFolderStructure);

                List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                folderTree.SetRootItems(updatedRootItems);
                folderTree.Rebuild();

                EditorUtility.DisplayDialog("Load Default", "Default structure loaded.", "OK");
                return;
            }

            // Construct path of preset to be loaded. 
            var rootSaveDirectory = FolderStructureManager.PresetSaveDirectory;
            var loadPath = Path.Combine(rootSaveDirectory, selectedPreset).Replace('\\', '/');
            loadPath = loadPath + ".json";

            if (!File.Exists(loadPath))
            {
                EditorUtility.DisplayDialog("Load Error", $"Preset file '{selectedPreset}.json' not found.", "OK");

                RefreshPresetDropdown();
                return;
            }

            try
            {
                // Deserialise Json data into wrapper class
                var jsonData = File.ReadAllText(loadPath);
                FolderNodeListWrapper loadedWrapper = JsonUtility.FromJson<FolderNodeListWrapper>(jsonData);
                List<FolderNode> loadedPresetList = null;
                if (loadedWrapper != null)
                {
                    loadedPresetList = loadedWrapper.RootNodes;
                }

                if (loadedPresetList != null)
                {
                    // Deep copy the deserialised json data (now in wrapper) to working  preset copy
                    workingPresetCopy = FolderStructureManager.DeepCloneList(loadedWrapper.RootNodes);

                    // Refresh the TreeView with the newly loaded data
                    List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                    folderTree.SetRootItems(updatedRootItems);
                    folderTree.Rebuild();


                    EditorUtility.DisplayDialog("Load Successful", $"Preset '{selectedPreset}' loaded.", "OK");
                    Debug.Log($"Preset '{selectedPreset}' loaded successfully.");
                }
                else
                {
                    throw new Exception("Preset file might be corrupted or in an invalid format.");
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load preset '{selectedPreset}' from '{loadPath}'. An unexpected error occurred: {e.Message}\nStack Trace: {e.StackTrace}");


                EditorUtility.DisplayDialog(
                    "Load Preset Error",
                    $"Failed to load preset '{selectedPreset}'.\n\nThe file might be corrupted, unreadable, or not in the expected format.\n\nError details: {e.Message}",
                    "OK");
            }

            RefreshPresetDropdown();

        };


        //Query for "Add folder" button and textfield
        var addFolderButton = rootVisualElement.Q<Button>("AddFolderButton");
        var addFolderText = rootVisualElement.Q<TextField>("AddFolderTextField");
        if (addFolderButton != null && addFolderText != null)
        {
            // Disable "Add folder" button by default.
            addFolderButton.SetEnabled(false);

            // Handle Enabling/Disabling Add folder button when textfield value changes.
            addFolderText.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue))
                {
                    addFolderButton.SetEnabled(false);
                }
                else
                {
                    addFolderButton.SetEnabled(true);
                }
            });

            // Handle "Add folder" button when clicked, adding a folder to the TreeView. 
            addFolderButton.clicked += () =>
            {
                var parentFolder = folderTree.selectedItem as FolderNode;
                var newFolderName = addFolderText.value;
                if (parentFolder == null || string.IsNullOrEmpty(newFolderName))
                {
                    return;
                }

                var newFolder = new FolderNode();

                string trimmedFolderName = newFolderName.Trim();
                newFolder.displayName = trimmedFolderName;

                var newPath = Path.Combine(parentFolder.path, newFolderName);
                newFolder.path = newPath;

                // Add the new folder to the structure, rebuild the editor, and reset the value of the text field for the user.
                parentFolder.children.Add(newFolder);
                List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                folderTree.SetRootItems(updatedRootItems);
                folderTree.Rebuild();
                addFolderText.value = null;

            };
        }
        
        // Query for "Delete Folder" button
        var deleteFolderButton = rootVisualElement.Q<Button>("RemoveFolderButton");
        if (deleteFolderButton != null)
        {
            // Disable Delete folder by default
            deleteFolderButton.SetEnabled(false);

            // Handle enabling/disabling Delete Folder button when selection changes
            folderTree.selectionChanged += (evt =>
            {
                var selection = folderTree.selectedItem as FolderNode;
                if (selection == null || selection.path == "Assets/")
                {
                    deleteFolderButton.SetEnabled(false);
                }
                else
                {
                    deleteFolderButton.SetEnabled(true);
                }
            });

            // Handle when Delete Folder button is clicked, deleting the selected folder from the tree view. 
            deleteFolderButton.clicked += () =>
            {
                var folderToDelete = folderTree.selectedItem as FolderNode;
                if (folderToDelete == null || folderToDelete.path == "Assets/")
                {
                    return;
                }

                bool deleted = false;
                if (workingPresetCopy.Remove(folderToDelete))
                {
                    deleted = true;
                }
                else
                {
                    var parentNode = FolderStructureManager.FindParentNode(workingPresetCopy, folderToDelete);
                    if (parentNode != null)
                    {
                        if (parentNode.children.Remove(folderToDelete))
                        {
                            deleted = true;
                        }
                        else
                        {
                            Debug.LogError("Failed to remove child from parent list, though parent was found.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Could not find parent for node '{folderToDelete.displayName}' during deletion attempt.");
                        return;
                    }
                }

                // Check if successfully deleted
                if (deleted)
                {
                    // Update tree view 
                    List<TreeViewItemData<FolderNode>> updatedRootItems = BuildTreeViewData(workingPresetCopy);
                    folderTree.SetRootItems(updatedRootItems);
                    folderTree.Rebuild();
                }
            };
        }

        // Query for "Save Preset" button and text field
        var savePresetButton = rootVisualElement.Q<Button>("SavePresetButton");
        var savePresetText = rootVisualElement.Q<TextField>("SavePresetTextField");
        if (savePresetButton != null && savePresetText != null)
        {
            savePresetButton.clicked += () =>
            {
                var presetName = savePresetText.value;

                if (string.IsNullOrWhiteSpace(presetName) || presetName.IndexOfAny(FolderStructureManager.invalidFileNameChars) != -1)
                {
                    Debug.LogWarning($"Invalid Preset Name: '{presetName}'. Name cannot be empty, whitespace, or contain invalid characters (e.g., / \\ : * ? \" < > |).");
                    return;

                }

                // Construct path to save preset
                var saveDirectory = FolderStructureManager.PresetSaveDirectory;
                var savePath = Path.Combine(saveDirectory, presetName).Replace("\\", "/");
                savePath = savePath + ".json";

                try
                {
                    // Create wrapper for Json serialisation and copy the working preset copy into the wrapper. 
                    var wrapper = new FolderNodeListWrapper();
                    wrapper.RootNodes = workingPresetCopy;

                    // Serialise the wrapped data into Json data files
                    var jsonData = EditorJsonUtility.ToJson(wrapper, true);
                    File.WriteAllText(savePath, jsonData);

                    EditorUtility.DisplayDialog("Save succesful", "Preset saved succesfully", "OK");
                    RefreshPresetDropdown();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save preset '{presetName}'. An unexpected error occurred: {e.Message}");
                    EditorUtility.DisplayDialog("Save Error", $"Failed to save preset '{presetName}'.\nError: {e.Message}", "OK");
                }

                // Clear the text field
                savePresetText.value = string.Empty;
            };
        }

        // Query for the "Delete Preset" button
        var deletePresetButton = rootVisualElement.Q<Button>("DeletePresetButton");
        if (deletePresetButton != null)
        {
            deletePresetButton.clicked += () =>
            {
                var selectedItem = presetDropdown.value;
                if (string.IsNullOrEmpty(selectedItem))
                {
                    return;
                }

                // Construct path for preset deletion
                var rootPresetFolder = FolderStructureManager.PresetSaveDirectory;
                var fileToDelete = Path.Combine(rootPresetFolder, selectedItem).Replace("\\", "/");
                fileToDelete = fileToDelete + ".json";
                if (!File.Exists(fileToDelete))
                {
                    return;
                }

                try
                {
                    // Delete saved preset file 
                    File.Delete(fileToDelete);
                    EditorUtility.DisplayDialog("Deletion Successful", $"File '{fileToDelete}' Deleted.", "OK");

                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete file '{fileToDelete}'. An unexpected error occurred: {e.Message}");

                }

                // Refresh dropdown list of presets
                RefreshPresetDropdown();

            };
        }

        // Query for "Apply Preset" button
        var applyStructureButton = rootVisualElement.Q<Button>("ApplyFolderStructureButton");
        if (applyStructureButton == null)
        {
            Debug.LogError("ApplyStructureButton not found in UXML! Cannot attach handler.");
        }
        else
        {
            // Assign delegate funciton to clicked event of "apply preset" button 
            applyStructureButton.clicked += HandleApplyStructureClicked;
        }
    }

    // Helper to recursively create the treeview
    private List<TreeViewItemData<FolderNode>> BuildTreeViewData(List<FolderNode> folderNodes)
    {
        List<TreeViewItemData<FolderNode>> treeViewItems = new List<TreeViewItemData<FolderNode>>();

        if (folderNodes == null) return treeViewItems; 

        foreach (var node in folderNodes)
        {
            List<TreeViewItemData<FolderNode>> childItems = BuildTreeViewData(node.children);

            var itemData = new TreeViewItemData<FolderNode>(
                id: node.path.GetHashCode(), 
                data: node,
                children: childItems 
            );

            treeViewItems.Add(itemData);
        }

        return treeViewItems;
    }


    // Helper to refresh preset dropdown options 
    private void RefreshPresetDropdown()
    {
        if (presetDropdown == null) return;

        var presetDirectory = FolderStructureManager.PresetSaveDirectory;
        List<string> presetNames = new List<string>();
        try
        {
            var filePaths = Directory.GetFiles(presetDirectory, "*.json");
            Debug.Log($"Found {filePaths.Length} *.json files.");

            foreach (var path in filePaths)
            {
                var presetName = Path.GetFileNameWithoutExtension(path);
                Debug.Log($"-- Extracted name: {presetName}");
                presetNames.Add(presetName);

            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading presets from '{presetDirectory}': {ex.Message}");
        
            presetNames.Clear();
        }

        presetNames.Insert(0, "Default");
        presetDropdown.choices = presetNames;
        presetDropdown.index = presetNames.Count > 0 ? 0 : -1;
        presetDropdown.value = string.Empty;
        Debug.Log($"Assigning {presetNames.Count} names to dropdown: {string.Join(", ", presetNames)}");

    }

    private void HandleMappingsApplied(string extensionToAdd)
    {
        if (!string.IsNullOrEmpty(extensionToAdd))
        {
            var selectedFolder = nodeSelectedForEdit;
            if (selectedFolder == null)
            {
                return;
            }

            if (selectedFolder.associatedExtensions.Contains(extensionToAdd))
            {
                EditorUtility.DisplayDialog("Mapping Error", "extension mapping already exists in this folder", "OK");
                return;
            }

            var conflictingNode = FolderStructureManager.IsExtensionAlreadyInUse(workingPresetCopy, selectedFolder, extensionToAdd);
            if (conflictingNode != null)
            {
                EditorUtility.DisplayDialog("Mapping Error", $"Extension mapping'{extensionToAdd}' already mapped to {conflictingNode.displayName}. User must remove existing mapping before applying to another ", "OK");
                return;
            }

            selectedFolder.associatedExtensions.Add(extensionToAdd);

            associatedExtensionsList.RefreshItems();
        }
    }

    private void HandleApplyStructureClicked()
    {
        string presetNameBeingApplied = "Default"; 
        if (presetDropdown != null && !string.IsNullOrEmpty(presetDropdown.value))
        {
            presetNameBeingApplied = presetDropdown.value; 
        }

        if (workingPresetCopy == null)
        {
            Debug.LogError("Cannot apply structure: No working preset data available.");
            EditorUtility.DisplayDialog("Error", "Cannot apply structure: No preset data loaded.", "OK");
            return;
        }

        string currentJson = EditorJsonUtility.ToJson(new FolderNodeListWrapper { RootNodes = workingPresetCopy }, false);
        string lastAppliedJson = (lastAppliedStructure == null) ? null : EditorJsonUtility.ToJson(new FolderNodeListWrapper { RootNodes = lastAppliedStructure }, false);

        if (currentJson == lastAppliedJson)
        {
            EditorUtility.DisplayDialog(
            "Already Applied Structure",
            "This structure is already applied to the project.\n",
            "Cancel");
            return;

        }
       
        bool confirm = EditorUtility.DisplayDialog(
             "Confirm Apply Structure", 
             "This will create any missing folders in your project based on the current structure view.\n" + 
             "(Note: This process does NOT delete existing folders or files).\n\n" +
             "Are you sure you want to proceed?",
             "Apply Structure", 
             "Cancel");
        
         if (!confirm)
         {
             return; 
         }

        try
        {
            Debug.Log($"Applying structure for preset: {presetNameBeingApplied}..."); 

            FolderStructureManager.ApplyFolderStructure(workingPresetCopy);

            
            Debug.Log("Folder structure applied successfully.");
            EditorUtility.DisplayDialog(
                "Success",
                "Folder structure applied successfully.\n(Missing folders were created).",
                "OK");

            lastAppliedStructure = FolderStructureManager.DeepCloneList(workingPresetCopy);

            EditorPrefs.SetString(FolderStructureManager.LastAppliedPresetPrefKey, presetNameBeingApplied);

            Debug.Log($"Saved '{presetNameBeingApplied}' to EditorPrefs as last applied preset.");

        }
        catch (IOException ioEx)
        {
            Debug.LogError($"Error applying folder structure (IO Exception): {ioEx.Message}\n{ioEx.StackTrace}");
            EditorUtility.DisplayDialog("Apply Error", $"Could not apply folder structure due to a file system IO error:\n{ioEx.Message}", "OK");
        }
        catch (UnauthorizedAccessException authEx)
        {
            Debug.LogError($"Error applying folder structure (Unauthorized Access): {authEx.Message}\n{authEx.StackTrace}");
            EditorUtility.DisplayDialog("Apply Error", $"Could not apply folder structure due to insufficient permissions:\n{authEx.Message}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"An unexpected error occurred while applying folder structure: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Apply Error", $"An unexpected error occurred:\n{ex.Message}", "OK");
        }

    }
}
