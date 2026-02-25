using System;
using System.Collections;
using System.Collections.Generic;

using Teamcenter.Services.Strong.Classification;
using Teamcenter.Soa.Client;
using TcExplorer.Model;

using ClassAttr = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassAttribute;
using ChildDef  = Teamcenter.Services.Strong.Classification._2007_01.Classification.ChildDef;
using GetChildrenResponse          = Teamcenter.Services.Strong.Classification._2007_01.Classification.GetChildrenResponse;
using GetAttributesForClassesResponse = Teamcenter.Services.Strong.Classification._2007_01.Classification.GetAttributesForClassesResponse;

namespace TcExplorer.Explore
{
    public class ClassificationExplorer
    {
        private readonly Connection _connection;

        public ClassificationExplorer(Connection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Builds the full classification hierarchy.
        /// Returns an empty list (never throws) if unavailable.
        /// Root classes are retrieved by passing "" to GetChildren — the standard
        /// ICS root ID in Teamcenter. If your installation uses a different root
        /// class ID, update the GetChildClassNodes call below.
        /// </summary>
        public List<ClassNode> BuildHierarchy()
        {
            try
            {
                ClassificationService service = ClassificationService.getService(_connection);
                return GetChildClassNodes(service, "");
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                return new List<ClassNode>();
            }
        }

        private List<ClassNode> GetChildClassNodes(ClassificationService service, string parentId)
        {
            var nodes = new List<ClassNode>();

            GetChildrenResponse resp = service.GetChildren(new[] { parentId });
            if (resp == null || resp.Children == null)
                return nodes;

            // Children hashtable: key = parentId, value = ChildDef[]
            if (!resp.Children.Contains(parentId))
                return nodes;

            ChildDef[] children = resp.Children[parentId] as ChildDef[];
            if (children == null)
                return nodes;

            foreach (ChildDef child in children)
            {
                var node = new ClassNode
                {
                    Id   = child.Id,
                    Name = string.IsNullOrEmpty(child.Name) ? child.Id : child.Name
                };

                try
                {
                    node.Attributes = GetAttributes(service, child.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[WARN] Could not load attributes for class " + child.Id + ": " + e.Message);
                }

                try
                {
                    if (child.ChildCount > 0)
                        node.Children = GetChildClassNodes(service, child.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[WARN] Could not load children for class " + child.Id + ": " + e.Message);
                }

                nodes.Add(node);
            }

            return nodes;
        }

        private static List<ClassAttribute> GetAttributes(ClassificationService service, string classId)
        {
            var result = new List<ClassAttribute>();

            GetAttributesForClassesResponse resp = service.GetAttributesForClasses(new[] { classId });
            if (resp == null || resp.Attributes == null)
                return result;

            // Attributes hashtable: key = classId, value = ClassAttribute[]
            if (!resp.Attributes.Contains(classId))
                return result;

            ClassAttr[] attrs = resp.Attributes[classId] as ClassAttr[];
            if (attrs == null)
                return result;

            foreach (ClassAttr attr in attrs)
            {
                result.Add(new ClassAttribute
                {
                    Id       = attr.Id.ToString(),
                    Name     = attr.Name,
                    DataType = FormatTypeToString(attr.Format.FormatType),
                    Unit     = attr.UnitName ?? ""
                });
            }

            return result;
        }

        // ICS FormatType integer codes (from TC classification internals)
        private static string FormatTypeToString(int formatType)
        {
            switch (formatType)
            {
                case 1:  return "String";
                case 2:  return "Integer";
                case 3:  return "Double";
                case 4:  return "Date";
                case 5:  return "Logical";
                case 6:  return "Reference";
                case 8:  return "ExternalReference";
                default: return "Unknown(" + formatType + ")";
            }
        }
    }
}
