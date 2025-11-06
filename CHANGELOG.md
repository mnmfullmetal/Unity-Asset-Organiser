# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-11-06

### Added

* Initial public release of the Unity Asset Organiser.
* Editor window (`Window > AssetOrganiserEditor`) for visually creating, editing, and viewing folder structures using a TreeView.
* Ability to map file extensions (e.g., `.png`, `.mat`, `.prefab`) to specific folders in the structure.
* Preset management system:
    * Save custom folder structures and mappings as JSON presets.
    * Load previously saved presets.
    * Delete presets from the project.
    * Includes a "Default" preset based on Unity's best practice guidelines.
* "Apply Structure to Project" button to physically create the defined folders in `Assets/`.
* Automatic asset sorting: A post-processor automatically moves newly imported assets to their correct folder based on the active preset.
* On editor startup, automatically applies the last used preset to ensure the folder structure is always active.
* All UI built using Unity's modern UI Toolkit (UXML/USS).
