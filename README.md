# Unity Asset Organiser
#### Video Demo:
https://www.youtube.com/watch?v=ve__u2ZS6Ew

#### Description:
Asset Organiser is a custom Unity editor tool designed to enforce project organisational standards and streamline development by eliminating tedious manual asset sorting. Accessed via Window > AssetOrganiserEditor, this tool empowers developers to maintain a clean and consistent project structure. It allows users to define project folder layouts, precisely map different file extensions (like .png, .mat, .prefab) to specific folders within that structure, and save these configurations as reusable presets. Once a user applies a chosen structure to the project, the Asset Organiser automatically moves newly imported or modified assets into their designated folders based on the defined extension mappings, ensuring the project remains consistently organised according to the desired layout with minimal ongoing effort.
The "Default" preset is an organisational structure outlined by Unity's best practices guidelines for project organisation, as found here: https://unity.com/how-to/organizing-your-project

## Features

* **Visual Folder Structure Editor:** An editor window (`AssetOrganiserEditor`) displays the target folder structure using a `TreeView`.

* **Structure Modification:**
    * Add new sub-folders to the structure.
    * Delete existing folders (except the root "Assets/" folder).

* **Extension Mapping:**
    * Select a folder in the `TreeView` to view and manage its associated file extensions in a dedicated panel.
    * Displays currently associated extensions in a `ListView`.
    * **Add Mappings:** Opens a separate window (`AddMappingEditor`) allowing selection from a predefined list of common extensions or entry of a custom extension. Performs checks to ensure an extension is only mapped to one folder within a preset.
    * **Remove Mappings:** Allows removing an associated extension from the selected folder.

* **Preset Management:**
    * **Default Structure:** Comes with a built-in default folder structure based on Unity best practices.
    * **Load Preset:** Load the default structure or previously saved custom structures. Loading updates the `TreeView` and the editor's working state. Presets are loaded via a `DropdownField`, which includes a "Default" option.
    * **Save Preset:** Save the currently edited folder structure (including extension mappings) as a named preset (`.json` file) within the project's `ProjectSettings/AssetOrganiserPresets` directory.
    * **Delete Preset:** Delete previously saved preset files via the editor window. The dropdown list updates automatically.

* **Apply Structure to Project:** A dedicated button ("Apply Structure to Project") physically creates the defined folder structure within the `Assets/` directory. It only creates missing folders and does not delete existing folders or files. *Note: Requires manual application by the user at least once.*

* **Automatic Asset Organisation:** An `AssetPostprocessor` (`AssetImportOrganiser`) runs automatically when assets are imported or moved. It checks the asset's file extension, finds the corresponding target folder path based on the currently active preset's mappings, and moves the asset accordingly using `AssetDatabase.MoveAsset`.

* **Startup Behaviour:** On editor startup, automatically applies the structure from the *last preset the user explicitly applied*. If none was applied, it applies the default structure. This ensures the necessary folders exist for the `AssetImportOrganiser`.


## How It Works

### Core Components

1.  **`AssetOrganiserEditor.cs` (Main Editor Window):**
    * The main UI built using **UI Toolkit** (loaded from `AssetOrganiserEditor.uxml` and styled with `.uss`).
    * Holds the `workingPresetCopy` (a deep copy of the currently loaded/edited `List<FolderNode>` structure).
    * Manages UI elements (`TreeView`, `ListView`, `DropdownField`, `TextField`, `Button`s) and their interactions.
    * Handles saving/loading presets to/from JSON files using `EditorJsonUtility` (via a wrapper class).
    * Communicates with `FolderStructureManager` to apply structures and perform utility functions.
    * Opens the `AddMappingEditor` window and handles the `OnApplyMappings` event.

2.  **`AddMappingEditor.cs` (Secondary, Popup Editor Window):**
    * A secondary window, also built using **UI Toolkit**, opened from the main editor.
    * Displays a predefined list of common extensions (`ListView`).
    * Includes a `TextField` for entering custom extensions.
    * Contains "Apply Mapping to Folder" button.
    * Receives the target `FolderNode` from the main window via a public property.
    * Uses an `event Action<string> OnApplyMappings` to send the chosen extension string back to the main window upon clicking "Apply Mapping to Folder" button.
    * Defines a "wrapper" class `FolderNodeListWrapper` to wrap the `FolderNode` structures for proper JSON serialisation/deserialisation.  This wrapper is marked `[Serializable]`

3.  **`FolderNode.cs` (Custom Data Class):**
    * A plain C# class marked `[Serializable]`.
    * Represents a single folder in the structure.
    * Contains public fields (required for `EditorJsonUtility`/`JsonUtility`): `displayName` (string), `path` (string), `children` (`List<FolderNode>`), `associatedExtensions` (`List<string>`).
    * Lists (`children`, `associatedExtensions`) are initialized as new lists (`new List<>()`).

4. **`FolderNodeListWrapper.cs` (Internal wrapper class):**
    * An internal class to wrap `List<FolderNode>` structures for proper JSON serialistion/deserialistion.

5.  **`FolderStructureManager.cs` (Static Helper Class):**
    * Acts as a central utility and data provider.
    * Defines the `public const string LastAppliedPresetPrefKey`
    * Defines the `public static List<FolderNode> DefaultFolderStructure`.
    * Defines the `public static string PresetSaveDirectory`.
    * Provides helper methods:
        * `CloneFolderNode`/`DeepCloneList`: For creating independent copies of the `FolderNode` structures to work with without changing the original.
        * `FindParentNode`: For tree traversal (used in deleting folders).
        *  `IsExtensionAlreadyInUse`: Recursively searches the structure to check for and return conflicting extension mappings.
        * `ApplyFolderStructure`/`EnsureFolderExists`: Contains the core logic for interacting with `AssetDatabase.CreateFolder` to create the physical project folders recursively.
    * Contains the `Initialise` method (`[InitializeOnLoadMethod]`) which ensures the preset save directory exists and apply the last used preset structure on editor load using `LastAppliedPresetPrefKey`.

6.  **`AssetImportOrganiser.cs` (AssetPostprocessor):**
    * Inherits from `AssetPostprocessor`.
    * Uses `OnPostprocessAllAssets` to intercept imported assets.
    * Gets the asset's path and file extension.
    * Calls `FolderStructureManager.FindTargetPathForExtension` to determine the correct destination folder based on the extension *and the currently active preset structure*
    * Checks if the asset is already in the correct place.
    * If not, calls `AssetDatabase.MoveAsset` to move the asset.

## Installation

This tool is distributed as a UPM package.

### Method 1: Install via Git URL (Recommended)

This is the simplest way to install the tool.

1.  In your Unity project, go to **Window > Package Manager**.
2.  Click the **`+`** icon in the top-left corner.
3.  Select **"Add package from git URL..."**
4.  Paste in the following URL:
    ```
    [https://github.com/mnmfullmetal/Unity-Asset-Organiser.git](https://github.com/mnmfullmetal/Unity-Asset-Organiser.git)
    ```
5.  Click **Add**. The package will be installed into your project.

### Method 2: Install via OpenUPM (for easy updates)

You can also install this package using [OpenUPM](https://openupm.com/), a community package registry. This is the best way to get automatic updates.

1.  If you don't have it, install the OpenUPM CLI:
    ```bash
    npm install -g openupm-cli
    ```
2.  Go to your Unity project's folder in your terminal and run:
    ```bash
    openupm add com.mnmfullmetal.unity-asset-organiser
    ```

---

## 📋 Requirements

* **Unity 2022.3** or newer.

### Usage

1.  Open the editor window (`Window > Asset Organiser`).

2.  The `TreeView` shows the current structure (initially the default).

3.  **Modify Structure:** Select a folder and use the "Add Folder" `TextField` and `Button` to add children. Select a folder (not "Assets/") and click "Remove Folder" to delete it.

4.  **Manage Mappings:** Select a folder (not "Assets/"). The "Associated Extensions" panel appears.
    * View current mappings in the list.
    * Select an extension in the list and click "Remove Mapping" to remove it.
    * Click "Add Mapping" to open the popup.
        * In the popup, select a predefined extension OR type a valid custom extension (starting with '.') into the text field.
        * Click "Apply Mapping". The popup closes, and the main window attempts to add the mapping (checking for duplicates first).
        * Close the popup window ('X' button) to cancel.

5.  **Manage Presets:**
    * Load "Default" or a saved preset using the dropdown and "Load Preset" button.
    * Save the current structure by typing a name in the "Save Preset" field and clicking the button.
    * Select a saved preset (not "Default") in the dropdown and click "Delete Preset" to remove it.

6.  **Apply to Project:** Click "Apply Structure to Project" to ensure all folders defined in the current `TreeView` structure physically exist in your `Assets/` directory. A confirmation dialog appears first. This step is required for the `AssetImportOrganiser` to reliably move assets if folders were missing.

## Design Choices & Hurdles

* **UI Toolkit:** I chose to use Unity's UI toolkit as it is now the new standard for modern Unity editor UI development, allowing separation of structure (UXML), style (USS), and logic (C#). Both the main window (`AssetOrganiserEditor`) and the secondary pop-up window (`AddMappingEditor`) utilises UI toolkit's `UI Builder` and is structured and styled using UXML and USS.

* **Deep Copies (`workingPresetCopy`):** Due to the behaviour of value vs reference types, it was essential to ensure that editing the structure in the window does not accidentally modify the original `DefaultFolderStructure` or the loaded preset data until explicitly saved or applied. This required me to implement methods to create "Deep copies/clones" in order to have a editable copy of the folder structure while maintaining the integrity of the default and saved presets.

* **JSON for Presets:** I chose to store the saved presets as JSON files due to them being a human-readable and standard format for saving data, and they handle nested data naturally which was ideal for my `List<FolderNode>` structures with key-value pairs. These files are stored in `ProjectSettings` to be project-specific and potentially committable to version control. Unity's built in JSON class `EditorJsonUtility` is used for its robustness and compatability with Unity types (though `JsonUtility` limitations required using public fields in `FolderNode`).

* **Wrapper class for JSON serialisation/deserialisation (`FolderNodeListWrapper`):** Early on I encountered an issue with the limitations of the EditorJSONUtility class where my `List<FolderNode>` presets were not being serialised as JSON strings properly despite my `FolderNode` class being `[Serializable]`. This was due to a limitation where the class is designed to seralise objects, not a list of objects. To fix this I created a class to "wrap" the `FolderNode` structures in for proper serialisation.

* **Static Manager Class (`FolderStructureManager`):** I created a class that consolidates core data structures (Default) and utility functions related to manipulating the `FolderNode` hierarchy and interacting with the file system, promoting separation of concerns from the UI logic in `AssetOrganiserEditor`.

* **Separate Mapping Window (`AddMappingEditor`):** Chosen over inline editing to provide a more focused UI for selecting/adding predefined or custom extensions without cluttering the main editor panel.

* **Event driven Inter-Window Communication:** Using a C# `event` provides a decoupled way for the `AddMappingEditor` to send data back to the `AssetOrganiserEditor` without the popup needing a direct reference back to the specific main window instance that opened it (beyond the initial subscription).

* **Explicit "Apply Structure" button:** Separating the *editing* of the structure from the *application* of the structure gives the user control over when changes are physically made to their project folders.

* **`AssetPostprocessor` for Automation:** Leverages Unity's built-in asset import pipeline to automatically organize assets based on the defined rules, fulfilling the core purpose of the tool.

* **Startup Behaviour:** Applying the last used preset on startup aims to provide a consistent experience, ensuring the project state reflects the user's configuration and the importer has the necessary folders.

### Known Issues / Notes

* A benign "Serialization depth limit exceeded" warning may appear in the console *only* on the very first time a preset is saved after Unity starts. Tests indicate the preset saves correctly despite this warning, which appears to be an internal quirk of `EditorJsonUtility`/`JsonUtility`'s initial processing.

## Further Information

For a more detailed and complete, step-by-step representation of this programs devlopment lifecycle, please refer to my public GitHub repository here: https://github.com/mnmfullmetal/Unity-Asset-Organiser

