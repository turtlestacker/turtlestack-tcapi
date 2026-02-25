using System;
using System.Collections.Generic;

using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;
using TcExplorer.Model;

using Cls0ClassSvc     = Cls0.Services.Strong.Classificationcore.ClassificationService;
using Cls0InputInfo    = Cls0.Services.Strong.Classificationcore._2013_05.Classification.GetHierarchyNodeChildrenInputInfo;
using Cls0Filter       = Cls0.Services.Strong.Classificationcore._2013_05.Classification.FilterExpression;
using Cls0NodeDetails  = Cls0.Services.Strong.Classificationcore._2013_05.Classification.HierarchyNodeDetails;
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

        /// <summary>
        /// Builds the classification hierarchy using the Cls0 service.
        /// GetTopLevelNodes() requires no root class ID — it works regardless
        /// of whether the TC instance uses ICS or Cls0-based classification.
        /// Returns an empty list (never throws) if unavailable.
        /// </summary>
        public List<ClassNode> BuildHierarchy()
        {
            try
            {
                // Register Cls0 strong model types with the model manager
                // so that Cls0HierarchyNode objects are correctly instantiated
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

            // Batch-load details for all sibling nodes in one call
            var detailsResp = service.GetHierarchyNodeDetails(nodes);
            if (detailsResp == null || detailsResp.NodeDetails == null)
                return result;

            foreach (Cls0HierarchyNode node in nodes)
            {
                // Cls0HierarchyNode inherits Uid from ModelObject
                string uid = ((ModelObject)node).Uid;

                // NodeDetails hashtable: key = node UID, value = Cls0NodeDetails
                Cls0NodeDetails details = detailsResp.NodeDetails[uid] as Cls0NodeDetails;
                if (details == null)
                    continue;

                var classNode = new ClassNode
                {
                    Id   = details.NodeId,
                    Name = string.IsNullOrEmpty(details.NodeName) ? details.NodeId : details.NodeName
                };

                if (!details.IsLeafNode)
                {
                    try
                    {
                        classNode.Children = GetChildren(service, node);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[WARN] Could not load children for " + details.NodeId + ": " + e.Message);
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
            if (childResp == null || childResp.Children == null)
                return new List<ClassNode>();

            string parentUid = ((ModelObject)parentNode).Uid;

            // Children hashtable: key = parent node UID, value = Cls0HierarchyNode[]
            Cls0HierarchyNode[] children = childResp.Children[parentUid] as Cls0HierarchyNode[];
            if (children == null)
                return new List<ClassNode>();

            return WalkNodes(service, children);
        }
    }
}
