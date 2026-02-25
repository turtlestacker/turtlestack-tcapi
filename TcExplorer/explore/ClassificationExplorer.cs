using System;
using System.Collections;
using System.Collections.Generic;

using Teamcenter.Services.Strong.Classification;
using Teamcenter.Soa.Client;
using TcExplorer.Model;

using Cls0ClassSvc    = Cls0.Services.Strong.Classificationcore.ClassificationService;
using Cls0InputInfo   = Cls0.Services.Strong.Classificationcore._2013_05.Classification.GetHierarchyNodeChildrenInputInfo;
using Cls0Filter      = Cls0.Services.Strong.Classificationcore._2013_05.Classification.FilterExpression;
using Cls0NodeDetails = Cls0.Services.Strong.Classificationcore._2013_05.Classification.HierarchyNodeDetails;
using Cls0HierarchyNode = Teamcenter.Soa.Client.Model.Strong.Cls0HierarchyNode;
using ClassAttr       = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassAttribute;

namespace TcExplorer.Explore
{
    public class ClassificationExplorer
    {
        private readonly Connection _connection;
        private const int MaxDepth = 30;

        public ClassificationExplorer(Connection connection)
        {
            _connection = connection;
        }

        public List<ClassNode> BuildHierarchy()
        {
            try
            {
                Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

                Cls0ClassSvc cls0Service  = Cls0ClassSvc.getService(_connection);
                ClassificationService classicService = ClassificationService.getService(_connection);

                var topResp = cls0Service.GetTopLevelNodes();
                if (topResp == null || topResp.TopLevelNodes == null || topResp.TopLevelNodes.Length == 0)
                {
                    Console.WriteLine("[WARN] Classification: GetTopLevelNodes returned no nodes.");
                    return new List<ClassNode>();
                }

                return WalkNodes(cls0Service, classicService, topResp.TopLevelNodes, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                return new List<ClassNode>();
            }
        }

        private List<ClassNode> WalkNodes(Cls0ClassSvc cls0Service, ClassificationService classicService,
                                          Cls0HierarchyNode[] nodes, int depth)
        {
            var result = new List<ClassNode>();
            if (nodes == null || nodes.Length == 0 || depth > MaxDepth)
                return result;

            var detailsResp = cls0Service.GetHierarchyNodeDetails(nodes);
            if (detailsResp == null || detailsResp.NodeDetails == null)
                return result;

            foreach (DictionaryEntry entry in detailsResp.NodeDetails)
            {
                Cls0NodeDetails details = entry.Value as Cls0NodeDetails;
                if (details == null)
                    continue;

                var classNode = new ClassNode
                {
                    Id   = details.NodeId ?? entry.Key?.ToString() ?? "",
                    Name = string.IsNullOrEmpty(details.NodeName) ? details.NodeId : details.NodeName
                };

                // Load attributes via classic service using the NodeId as the ICS class ID
                if (!string.IsNullOrEmpty(details.NodeId))
                {
                    try
                    {
                        classNode.Attributes = GetAttributes(classicService, details.NodeId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[WARN] Could not load attributes for " + classNode.Id + ": " + e.Message);
                    }
                }

                // Always recurse — do NOT rely on IsLeafNode.
                // In Cls0, IsLeafNode means "objects can be classified here"
                // not "this node has no children in the hierarchy".
                if (details.NodeToUpdate != null)
                {
                    try
                    {
                        classNode.Children = GetChildren(cls0Service, classicService,
                                                         details.NodeToUpdate, depth + 1);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[WARN] Could not load children for " + classNode.Id + ": " + e.Message);
                    }
                }

                result.Add(classNode);
            }

            return result;
        }

        private List<ClassNode> GetChildren(Cls0ClassSvc cls0Service, ClassificationService classicService,
                                            Cls0HierarchyNode parentNode, int depth)
        {
            var input = new Cls0InputInfo
            {
                Node                  = parentNode,
                Recursive             = false,
                Filters               = new Cls0Filter[0],
                ExtendedInfoRequested = new string[0]
            };

            var childResp = cls0Service.GetHierarchyNodeChildren(new[] { input });
            if (childResp == null || childResp.Children == null || childResp.Children.Count == 0)
                return new List<ClassNode>();

            // Diagnostic: show actual runtime key/value types in the Children hashtable
            foreach (DictionaryEntry dbg in childResp.Children)
            {
                Console.WriteLine("[DEBUG] Children hashtable entry — key type: "
                    + (dbg.Key?.GetType().FullName ?? "null")
                    + "  value type: "
                    + (dbg.Value?.GetType().FullName ?? "null"));
                if (dbg.Value is Array arr && arr.Length > 0)
                    Console.WriteLine("[DEBUG]   array element type: " + arr.GetValue(0)?.GetType().FullName);
            }

            var allChildren = new List<Cls0HierarchyNode>();
            foreach (DictionaryEntry entry in childResp.Children)
            {
                Cls0HierarchyNode[] children = entry.Value as Cls0HierarchyNode[];
                if (children != null)
                    allChildren.AddRange(children);
            }

            return WalkNodes(cls0Service, classicService, allChildren.ToArray(), depth);
        }

        private static List<ClassAttribute> GetAttributes(ClassificationService service, string classId)
        {
            var result = new List<ClassAttribute>();

            var resp = service.GetAttributesForClasses(new[] { classId });
            if (resp == null || resp.Attributes == null)
                return result;

            // Hashtable: key = classId, value = ClassAttribute[]
            // Iterate to avoid key-format assumptions
            foreach (DictionaryEntry entry in resp.Attributes)
            {
                ClassAttr[] attrs = entry.Value as ClassAttr[];
                if (attrs == null)
                    continue;

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
            }

            return result;
        }

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
