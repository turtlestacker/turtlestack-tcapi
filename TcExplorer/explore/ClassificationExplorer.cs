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

                Cls0ClassSvc cls0Service         = Cls0ClassSvc.getService(_connection);
                ClassificationService classicSvc = ClassificationService.getService(_connection);

                // Step 1: get top-level node model objects
                var topResp = cls0Service.GetTopLevelNodes();
                if (topResp == null || topResp.TopLevelNodes == null || topResp.TopLevelNodes.Length == 0)
                {
                    Console.WriteLine("[WARN] Classification: GetTopLevelNodes returned no nodes.");
                    return new List<ClassNode>();
                }

                // Step 2: get details for top-level nodes
                // NodeDetails hashtable: key = Cls0HierarchyNode, value = HierarchyNodeDetails[]
                // (same structure as getHierarchyNodeChildren)
                var detailsResp = cls0Service.GetHierarchyNodeDetails(topResp.TopLevelNodes);
                if (detailsResp == null || detailsResp.NodeDetails == null)
                    return new List<ClassNode>();

                var topDetails = new List<Cls0NodeDetails>();
                foreach (DictionaryEntry entry in detailsResp.NodeDetails)
                {
                    // Value is HierarchyNodeDetails[] (confirmed via runtime diagnostics)
                    Cls0NodeDetails[] arr = entry.Value as Cls0NodeDetails[];
                    if (arr != null)
                        foreach (var d in arr) topDetails.Add(d);
                    else
                    {
                        // Fallback: single HierarchyNodeDetails (not in an array)
                        Cls0NodeDetails single = entry.Value as Cls0NodeDetails;
                        if (single != null) topDetails.Add(single);
                    }
                }

                return BuildFromDetails(cls0Service, classicSvc, topDetails, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                return new List<ClassNode>();
            }
        }

        // Recursively build ClassNodes from a flat list of HierarchyNodeDetails.
        // Children come back as HierarchyNodeDetails[] from GetHierarchyNodeChildren,
        // so we never need to call GetHierarchyNodeDetails again after the top level.
        private List<ClassNode> BuildFromDetails(Cls0ClassSvc cls0Service,
                                                 ClassificationService classicSvc,
                                                 List<Cls0NodeDetails> details,
                                                 int depth)
        {
            var result = new List<ClassNode>();
            if (details == null || details.Count == 0 || depth > MaxDepth)
                return result;

            foreach (Cls0NodeDetails d in details)
            {
                if (d == null) continue;

                var classNode = new ClassNode
                {
                    Id   = d.NodeId ?? "",
                    Name = string.IsNullOrEmpty(d.NodeName) ? d.NodeId : d.NodeName
                };

                if (!string.IsNullOrEmpty(d.NodeId))
                {
                    try { classNode.Attributes = GetAttributes(classicSvc, d.NodeId); }
                    catch (Exception e)
                    { Console.WriteLine("[WARN] Attributes for " + d.NodeId + ": " + e.Message); }
                }

                // d.NodeToUpdate is the Cls0HierarchyNode needed to query children
                if (d.NodeToUpdate != null)
                {
                    try
                    {
                        List<Cls0NodeDetails> childDetails = FetchChildDetails(cls0Service, d.NodeToUpdate);
                        if (childDetails.Count > 0)
                            classNode.Children = BuildFromDetails(cls0Service, classicSvc, childDetails, depth + 1);
                    }
                    catch (Exception e)
                    { Console.WriteLine("[WARN] Children for " + d.NodeId + ": " + e.Message); }
                }

                result.Add(classNode);
            }

            return result;
        }

        // Call GetHierarchyNodeChildren and extract HierarchyNodeDetails[] from the response hashtable.
        // Confirmed structure: key = Cls0GroupNode, value = HierarchyNodeDetails[]
        private static List<Cls0NodeDetails> FetchChildDetails(Cls0ClassSvc service, Cls0HierarchyNode parentNode)
        {
            var input = new Cls0InputInfo
            {
                Node                  = parentNode,
                Recursive             = false,
                Filters               = new Cls0Filter[0],
                ExtendedInfoRequested = new string[0]
            };

            var childResp = service.GetHierarchyNodeChildren(new[] { input });
            var result = new List<Cls0NodeDetails>();

            if (childResp == null || childResp.Children == null || childResp.Children.Count == 0)
                return result;

            foreach (DictionaryEntry entry in childResp.Children)
            {
                Cls0NodeDetails[] arr = entry.Value as Cls0NodeDetails[];
                if (arr != null)
                    foreach (var d in arr) result.Add(d);
            }

            return result;
        }

        private static List<ClassAttribute> GetAttributes(ClassificationService service, string classId)
        {
            var result = new List<ClassAttribute>();

            var resp = service.GetAttributesForClasses(new[] { classId });
            if (resp == null || resp.Attributes == null)
                return result;

            foreach (DictionaryEntry entry in resp.Attributes)
            {
                ClassAttr[] attrs = entry.Value as ClassAttr[];
                if (attrs == null) continue;

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
