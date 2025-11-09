using System;
using System.Collections.Generic;

// Wrapper for correct json deserialisation
[Serializable]
internal class FolderNodeListWrapper
{
    public List<FolderNode> RootNodes = new List<FolderNode>();
}
