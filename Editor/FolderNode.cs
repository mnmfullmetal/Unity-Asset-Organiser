using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class FolderNode 
{
    public string displayName;
    public string path;
    public List<FolderNode> children = new List<FolderNode>();
    public List<string> associatedExtensions = new List<string>();
}
