using System;
using System.Collections;
using System.Collections.Generic;

using Teamcenter.Soa.Client;
using TcExplorer.Model;

using Cls0ClassSvc    = Cls0.Services.Strong.Classificationcore.ClassificationService;
using Cls0InputInfo   = Cls0.Services.Strong.Classificationcore._2013_05.Classification.GetHierarchyNodeChildrenInputInfo;
using Cls0Filter      = Cls0.Services.Strong.Classificationcore._2013_05.Classification.FilterExpression;
using Cls0NodeDetails = Cls0.Services.Strong.Classificationcore._2013_05.Classification.HierarchyNodeDetails;
using Cls0HierarchyNode = Teamcenter.Soa.Client.Model.Strong.Cls0HierarchyNode;

namespace TcExplorer.Explore
{
    public class ClassificationExplorer
    {
        private readonly Connection _connection;

        public ClassificationExplorer(Connection connection)
        {
            _connection = connection;
        }

        public List<ClassNode> BuildHierarchy()
        {
            try
            {
                Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

                Cls0ClassSvc service = Cls0ClassSvc.getService(_connection);

                var topResp = service.GetTopLevelNodes();
                if (topResp == null || topResp.TopLevelNodes == null || topResp.TopLevelNodes.Length == 0)
                {
                    Console.WriteLine("[WARN] Classification: GetTopLevelNodes returned no nodes.");
                    return new List<ClassNode>();
                }

                return WalkNodes(service, topResp.TopLevelNodes);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                return new List<ClassNode>();
            }
        }

        private List<ClassNode> WalkNodes(Cls0ClassSvc service, Cls0HierarchyNode[] nodes)
        {
            var result = new List<ClassNode>();
            if (nodes == null || nodes.Length == 0)
                return result;

            var detailsResp = service.GetHierarchyNodeDetails(nodes);
            if (detailsResp == null || detailsResp.NodeDetails == null)
                return result;

            // Iterate the hashtable directly — key format varies by TC version.
            // HierarchyNodeDetails.NodeToUpdate gives back the original Cls0HierarchyNode,
            // so we never need to look up by UID.
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

                if (!details.IsLeafNode && details.NodeToUpdate != null)
                {
                    try
                    {
                        classNode.Children = GetChildren(service, details.NodeToUpdate);
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

        private List<ClassNode> GetChildren(Cls0ClassSvc service, Cls0HierarchyNode parentNode)
        {
            var input = new Cls0InputInfo
            {
                Node                  = parentNode,
                Recursive             = false,
                Filters               = new Cls0Filter[0],
                ExtendedInfoRequested = new string[0]
            };

            var childResp = service.GetHierarchyNodeChildren(new[] { input });
            if (childResp == null || childResp.Children == null || childResp.Children.Count == 0)
                return new List<ClassNode>();

            // Collect all child nodes from all hashtable entries.
            // Since we pass one parent at a time there is only one entry,
            // but iterating is safer than assuming the key format.
            var allChildren = new List<Cls0HierarchyNode>();
            foreach (DictionaryEntry entry in childResp.Children)
            {
                Cls0HierarchyNode[] children = entry.Value as Cls0HierarchyNode[];
                if (children != null)
                    allChildren.AddRange(children);
            }

            return WalkNodes(service, allChildren.ToArray());
        }
    }
}
