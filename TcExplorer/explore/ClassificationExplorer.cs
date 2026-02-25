using System;
using System.Collections.Generic;

using Teamcenter.Soa.Client;
using TcExplorer.Model;

// Classification API notes (Phase 2):
// -----------------------------------------------------------
// To verify these APIs, open VS 2019 Object Browser and add:
//   TcSoaClassificationStrong.dll
//   Cls0SoaClassificationCoreStrong.dll
//   Cla0SoaClassificationCommonStrong.dll
//   TcSoaStrongModelClassificationCore.dll
//
// Expected service class names (VERIFY_API — confirm in Object Browser):
//   Teamcenter.Services.Strong.Classification.ClassificationService
//   Cls0.Services.Strong.ClassificationCore.Cls0ClassificationCoreService
//
// Uncomment the relevant using directives and method bodies below once
// the exact namespaces and method signatures are confirmed.
// -----------------------------------------------------------

// VERIFY_API: Uncomment once namespace is confirmed:
// using Teamcenter.Services.Strong.Classification;
// using Cls0.Services.Strong.ClassificationCore;

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
        /// Builds the full classification hierarchy from the TC server.
        /// Returns an empty list (never throws) if the classification API is
        /// unavailable or not yet verified.
        /// </summary>
        public List<ClassNode> BuildHierarchy()
        {
            try
            {
                return GetRootClassNodes();
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                Console.WriteLine("       Classification API calls are not yet verified (see ClassificationExplorer.cs).");
                return new List<ClassNode>();
            }
        }

        private List<ClassNode> GetRootClassNodes()
        {
            // VERIFY_API: Get service stub — confirm class name in Object Browser
            // var service = ClassificationService.getService(_connection);

            // VERIFY_API: Call the operation that returns root-level class definitions.
            // Typical pattern (exact method name TBD):
            //   var response = service.GetRootClasses(new GetRootClassesInput { ... });
            //   foreach (var classDef in response.ClassDefinitions)
            //       roots.Add(BuildClassNode(service, classDef));

            // Returning empty until VERIFY_API blocks are resolved.
            return new List<ClassNode>();
        }

        private ClassNode BuildClassNode(object service, object classDef)
        {
            // VERIFY_API: Map classDef fields to ClassNode.
            // Expected fields on classDef (confirm in Object Browser):
            //   classDef.ClassId    → ClassNode.Id
            //   classDef.ClassName  → ClassNode.Name
            //
            // var node = new ClassNode
            // {
            //     Id   = classDef.ClassId,
            //     Name = classDef.ClassName
            // };

            var node = new ClassNode { Id = "", Name = "" };

            try
            {
                node.Attributes = GetAttributes(service, node.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Could not load attributes for class " + node.Id + ": " + e.Message);
            }

            try
            {
                node.Children = GetChildClassNodes(service, node.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Could not load children for class " + node.Id + ": " + e.Message);
            }

            return node;
        }

        private List<ClassNode> GetChildClassNodes(object service, string parentId)
        {
            // VERIFY_API: Call operation to get children of parentId.
            // Typical pattern:
            //   var response = service.GetChildClasses(new GetChildClassesInput { ParentId = parentId });
            //   var children = new List<ClassNode>();
            //   foreach (var child in response.ClassDefinitions)
            //       children.Add(BuildClassNode(service, child));
            //   return children;

            return new List<ClassNode>();
        }

        private List<ClassAttribute> GetAttributes(object service, string classId)
        {
            // VERIFY_API: Call operation to get attribute definitions for classId.
            // Typical pattern:
            //   var response = service.GetClassAttributes(new GetClassAttributesInput { ClassId = classId });
            //   var attrs = new List<ClassAttribute>();
            //   foreach (var attrDef in response.AttributeDefinitions)
            //   {
            //       attrs.Add(new ClassAttribute
            //       {
            //           Id       = attrDef.AttributeId,
            //           Name     = attrDef.AttributeName,
            //           DataType = attrDef.DataType.ToString(),
            //           Unit     = attrDef.Unit ?? ""
            //       });
            //   }
            //   return attrs;

            return new List<ClassAttribute>();
        }
    }
}
