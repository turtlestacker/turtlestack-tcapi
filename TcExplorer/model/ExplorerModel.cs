using System.Collections.Generic;

namespace TcExplorer.Model
{
    public class ItemInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Uid  { get; set; }
    }

    public class FolderNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Uid  { get; set; }
        public List<FolderNode> Children { get; set; } = new List<FolderNode>();
        public List<ItemInfo>   Items    { get; set; } = new List<ItemInfo>();
    }

    public class ClassAttribute
    {
        public string Id       { get; set; }
        public string Name     { get; set; }
        public string DataType { get; set; }
        public string Unit     { get; set; }
    }

    public class ClassNode
    {
        public string Id   { get; set; }
        public string Name { get; set; }
        public List<ClassNode>      Children   { get; set; } = new List<ClassNode>();
        public List<ClassAttribute> Attributes { get; set; } = new List<ClassAttribute>();
    }

    public class ExplorerResult
    {
        public FolderNode       FolderTree         { get; set; }
        public List<ClassNode>  ClassificationTree { get; set; } = new List<ClassNode>();
    }
}
