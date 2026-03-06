using System.Collections.Generic;

namespace SymbolDetective.Model
{
    /// <summary>A single property name/value pair collected from the TC model object.</summary>
    public class PropValue
    {
        public string Name  { get; set; }
        public string Value { get; set; }
    }

    /// <summary>A named-reference file discovered inside a Dataset via ref_list.</summary>
    public class DatasetFile
    {
        public string Uid          { get; set; }
        public string FileName     { get; set; }  // original_file_name
        public string FileType     { get; set; }  // object_type (e.g. "ImanFile")
        public string FileLocation { get; set; }  // file_location if available
    }

    /// <summary>A TC object related to the symbol via a named GRM relation.</summary>
    public class RelatedObject
    {
        public string Uid      { get; set; }
        public string Name     { get; set; }
        public string Type     { get; set; }
        public string Relation { get; set; }
        /// <summary>Scalar properties loaded from the related object.</summary>
        public List<PropValue>  Properties { get; set; } = new List<PropValue>();
        /// <summary>Files discovered via ref_list on this object (for Dataset types).</summary>
        public List<DatasetFile> Files     { get; set; } = new List<DatasetFile>();
    }

    /// <summary>A classification instance (ICO) the symbol belongs to.</summary>
    public class ClassEntry
    {
        public string IcoUid  { get; set; }
        public string ClassId { get; set; }
        /// <summary>Attribute name/value pairs from the classification instance.</summary>
        public List<PropValue> Attributes { get; set; } = new List<PropValue>();
    }

    /// <summary>Simplified info about the parent Item (not the revision).</summary>
    public class ParentItemInfo
    {
        public string Uid  { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<PropValue> Properties { get; set; } = new List<PropValue>();
    }

    /// <summary>The top-level result: everything discovered about the target symbol revision.</summary>
    public class SymbolReport
    {
        public string SymbolItemId   { get; set; }
        public string SymbolRevision { get; set; }
        public string Uid            { get; set; }
        public string Name           { get; set; }
        public string Type           { get; set; }

        /// <summary>All scalar properties loaded on the revision object.</summary>
        public List<PropValue>     Properties      { get; set; } = new List<PropValue>();

        /// <summary>All relation properties that returned one or more TC objects.</summary>
        public List<RelatedObject> Relations       { get; set; } = new List<RelatedObject>();

        /// <summary>Classification instances (ICOs) the symbol revision is classified under.</summary>
        public List<ClassEntry>    Classifications { get; set; } = new List<ClassEntry>();

        /// <summary>Parent Item properties (item_id level, not the revision).</summary>
        public ParentItemInfo      ParentItem      { get; set; }
    }
}
