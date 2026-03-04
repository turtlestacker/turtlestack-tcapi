using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Teamcenter.Services.Strong.Classification;
using Teamcenter.Services.Strong.Core;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;
using TcExplorer.Model;

using Cls0ClassSvc      = Cls0.Services.Strong.Classificationcore.ClassificationService;
using Cls0InputInfo     = Cls0.Services.Strong.Classificationcore._2013_05.Classification.GetHierarchyNodeChildrenInputInfo;
using Cls0Filter        = Cls0.Services.Strong.Classificationcore._2013_05.Classification.FilterExpression;
using Cls0NodeDetails   = Cls0.Services.Strong.Classificationcore._2013_05.Classification.HierarchyNodeDetails;
using Cls0HierarchyNode = Teamcenter.Soa.Client.Model.Strong.Cls0HierarchyNode;
using ClassAttr         = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassAttribute;
using SearchClassAttrs  = Teamcenter.Services.Strong.Classification._2007_01.Classification.SearchClassAttributes;
using SearchAttr        = Teamcenter.Services.Strong.Classification._2007_01.Classification.SearchAttribute;
using ClsObject         = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassificationObject;

namespace TcExplorer.Explore
{
    public class ClassificationExplorer
    {
        private readonly Connection              _connection;
        private readonly DataManagementService   _dmService;
        private const int MaxDepth = 30;

        // Call-count and timing accumulators
        private int    _childrenCalls;
        private int    _attributeCalls;
        private double _childrenMs;
        private double _attributeMs;
        private int    _nodesProcessed;
        // Cache of classification attribute ID → display name, populated per class
        private readonly Dictionary<string, string> _attrNameCache = new Dictionary<string, string>();

        public ClassificationExplorer(Connection connection)
        {
            _connection = connection;
            _dmService  = DataManagementService.getService(connection);
        }

        /// <summary>
        /// Build the classification hierarchy for the user-selected top-level node.
        /// When a node name contains <paramref name="keyword"/> (case-insensitive),
        /// all objects classified under that class are fetched and attached.
        /// </summary>
        public List<ClassNode> BuildHierarchy(int nodeLimit = 0, string keyword = "")
        {
            _childrenCalls = _attributeCalls = _nodesProcessed = 0;
            _childrenMs    = _attributeMs    = 0;

            try
            {
                Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

                Cls0ClassSvc cls0Service         = Cls0ClassSvc.getService(_connection);
                ClassificationService classicSvc = ClassificationService.getService(_connection);

                var topResp = cls0Service.GetTopLevelNodes();
                if (topResp == null || topResp.TopLevelNodes == null || topResp.TopLevelNodes.Length == 0)
                {
                    Console.WriteLine("[WARN] Classification: GetTopLevelNodes returned no nodes.");
                    return new List<ClassNode>();
                }

                var detailsResp = cls0Service.GetHierarchyNodeDetails(topResp.TopLevelNodes);
                if (detailsResp == null || detailsResp.NodeDetails == null)
                    return new List<ClassNode>();

                var topDetails = new List<Cls0NodeDetails>();
                foreach (DictionaryEntry entry in detailsResp.NodeDetails)
                {
                    Cls0NodeDetails[] arr = entry.Value as Cls0NodeDetails[];
                    if (arr != null)
                        foreach (var d in arr) topDetails.Add(d);
                    else
                    {
                        Cls0NodeDetails single = entry.Value as Cls0NodeDetails;
                        if (single != null) topDetails.Add(single);
                    }
                }

                topDetails.Sort((a, b) => string.Compare(
                    string.IsNullOrEmpty(a.NodeName) ? a.NodeId : a.NodeName,
                    string.IsNullOrEmpty(b.NodeName) ? b.NodeId : b.NodeName,
                    StringComparison.OrdinalIgnoreCase));

                Cls0NodeDetails selected = PromptTopLevelMenu(topDetails);
                if (selected == null)
                    return new List<ClassNode>();

                if (!string.IsNullOrEmpty(keyword))
                    Console.WriteLine($"[INFO]   Keyword filter: \"{keyword}\" — classified objects fetched for matching nodes");

                return BuildFromDetails(cls0Service, classicSvc, new List<Cls0NodeDetails> { selected }, 0, nodeLimit, keyword);
            }
            catch (Exception e)
            {
                Console.WriteLine("[WARN] Classification hierarchy unavailable: " + e.Message);
                return new List<ClassNode>();
            }
        }

        private static Cls0NodeDetails PromptTopLevelMenu(List<Cls0NodeDetails> nodes)
        {
            Console.WriteLine();
            Console.WriteLine("Classification top-level nodes:");
            Console.WriteLine(new string('─', 60));
            for (int i = 0; i < nodes.Count; i++)
            {
                string label = string.IsNullOrEmpty(nodes[i].NodeName) ? nodes[i].NodeId : nodes[i].NodeName;
                Console.WriteLine($"  {i + 1,3}.  {label}  [{nodes[i].NodeId}]");
            }
            Console.WriteLine(new string('─', 60));
            Console.Write($"Enter number (1–{nodes.Count}): ");

            while (true)
            {
                string line = Console.ReadLine()?.Trim();
                if (int.TryParse(line, out int choice) && choice >= 1 && choice <= nodes.Count)
                    return nodes[choice - 1];
                Console.Write($"  Please enter a number between 1 and {nodes.Count}: ");
            }
        }

        public void PrintCallStats()
        {
            Console.WriteLine($"[STATS]  Classification nodes processed: {_nodesProcessed}");
            Console.WriteLine($"[STATS]  GetHierarchyNodeChildren calls: {_childrenCalls}  ({_childrenMs:F0} ms total, avg {(_childrenCalls > 0 ? _childrenMs / _childrenCalls : 0):F0} ms/call)");
            Console.WriteLine($"[STATS]  GetAttributesForClasses calls:  {_attributeCalls}  ({_attributeMs:F0} ms total, avg {(_attributeCalls > 0 ? _attributeMs / _attributeCalls : 0):F0} ms/call)");
        }

        private List<ClassNode> BuildFromDetails(Cls0ClassSvc cls0Service,
                                                 ClassificationService classicSvc,
                                                 List<Cls0NodeDetails> details,
                                                 int depth,
                                                 int nodeLimit,
                                                 string keyword)
        {
            var result = new List<ClassNode>();
            if (details == null || details.Count == 0 || depth > MaxDepth)
                return result;

            foreach (Cls0NodeDetails d in details)
            {
                if (d == null) continue;
                if (nodeLimit > 0 && _nodesProcessed >= nodeLimit)
                {
                    Console.WriteLine($"[INFO]   Node limit ({nodeLimit}) reached — stopping.");
                    return result;
                }

                _nodesProcessed++;
                Console.Write($"\r[PROGRESS] Classification nodes: {_nodesProcessed}" + (nodeLimit > 0 ? $"/{nodeLimit}" : "") + "   ");

                string nodeName = string.IsNullOrEmpty(d.NodeName) ? d.NodeId : d.NodeName;
                var classNode = new ClassNode
                {
                    Id   = d.NodeId ?? "",
                    Name = nodeName
                };

                // Fetch classified objects — only for matching nodes if keyword supplied, otherwise all nodes
                bool keywordMatch = !string.IsNullOrEmpty(keyword) &&
                                     nodeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                bool shouldFetch  = !string.IsNullOrEmpty(d.NodeId) &&
                                     (string.IsNullOrEmpty(keyword) || keywordMatch);

                if (shouldFetch)
                {
                    try
                    {
                        classNode.ClassifiedObjects = FetchClassifiedObjects(classicSvc, d.NodeId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"\n[WARN] {d.NodeId}: {e.Message}");
                    }

                    int count = classNode.ClassifiedObjects.Count;
                    if (count > 0)
                    {
                        string header = keywordMatch
                            ? $"*** {nodeName} [{d.NodeId}]  ({count} object(s))"
                            : $"    {nodeName} [{d.NodeId}]  ({count} object(s))";
                        Console.WriteLine("\n" + header);
                        foreach (ClassifiedObject co in classNode.ClassifiedObjects)
                        {
                            Console.WriteLine($"  {co.WsoName}  [{co.WsoType}]");
                            foreach (DatasetInfo ds in co.Datasets)
                                Console.WriteLine($"    image: {ds.Name}  [{ds.Type}]  uid={ds.Uid}");
                        }
                    }
                }

                if (d.NodeToUpdate != null)
                {
                    try
                    {
                        List<Cls0NodeDetails> childDetails = TimedFetchChildDetails(cls0Service, d.NodeToUpdate);
                        if (childDetails.Count > 0)
                            classNode.Children = BuildFromDetails(cls0Service, classicSvc, childDetails, depth + 1, nodeLimit, keyword);
                    }
                    catch (Exception e)
                    { Console.WriteLine($"\n[WARN] Children for {d.NodeId}: {e.Message}"); }
                }

                result.Add(classNode);
            }

            return result;
        }

        private List<ClassifiedObject> FetchClassifiedObjects(ClassificationService classicSvc, string classId)
        {
            var result = new List<ClassifiedObject>();

            // ── Step 1: Search for ICOs in this class ────────────────────────────
            var searchResp = classicSvc.Search(new[] { new SearchClassAttrs
            {
                ClassIds         = new[] { classId },
                SearchAttributes = new SearchAttr[0],
                SearchOption     = 0
            }});

            if (searchResp?.ClsObjTags == null) return result;

            var icoObjects = new List<ModelObject>();
            foreach (DictionaryEntry entry in searchResp.ClsObjTags)
            {
                ModelObject[] arr = entry.Value as ModelObject[];
                if (arr != null) icoObjects.AddRange(arr);
                else if (entry.Value is ModelObject mo) icoObjects.Add(mo);
            }
            if (icoObjects.Count == 0) return result;

            // ── Step 2: Get ClassificationObjects (ICO → WsoId + Properties) ────
            var clsObjsResp = classicSvc.GetClassificationObjects(icoObjects.ToArray());
            ClsObject[] clsObjects = ExtractClassificationObjects(clsObjsResp);
            if (clsObjects == null || clsObjects.Length == 0) return result;

            // ── Step 3: Batch-load WSO name/type + image datasets ────────────────
            var wsoList = new List<ModelObject>();
            foreach (ClsObject co in clsObjects)
                if (co.WsoId != null) wsoList.Add(co.WsoId);

            // ── Step 3b: Populate attribute name cache for this class ─────────────
            try
            {
                var attrResp = classicSvc.GetAttributesForClasses(new[] { classId });
                if (attrResp?.Attributes != null)
                    foreach (DictionaryEntry entry in attrResp.Attributes)
                    {
                        ClassAttr[] attrs = entry.Value as ClassAttr[];
                        if (attrs == null) continue;
                        foreach (ClassAttr a in attrs)
                        {
                            string key = a.Id.ToString();
                            if (!_attrNameCache.ContainsKey(key))
                                _attrNameCache[key] = a.Name ?? key;
                        }
                    }
            }
            catch { }

            // ── Step 4: Batch-load WSO name/type + all candidate dataset relations ─
            string[] candidateRelations = {
                "object_string", "object_type",
                "IMAN_reference", "IMAN_specification", "IMAN_manifestation", "TC_Attaches",
            };
            if (wsoList.Count > 0)
            {
                try { _dmService.GetProperties(wsoList.ToArray(), candidateRelations); }
                catch (Exception e) { Console.WriteLine($"\n[WARN] GetProperties (WSO): {e.Message}"); }
            }

            // ── Step 5: Batch-load dataset names/types (collect from all relations) ─
            var allDatasets = new List<ModelObject>();
            foreach (ModelObject wso in wsoList)
            {
                foreach (string rel in new[] { "IMAN_reference", "IMAN_specification", "IMAN_manifestation", "TC_Attaches" })
                {
                    try
                    {
                        Property p = wso.GetProperty(rel);
                        if (p?.ModelObjectArrayValue != null)
                            foreach (ModelObject ds in p.ModelObjectArrayValue)
                                if (ds != null) allDatasets.Add(ds);
                    }
                    catch { }
                }
            }
            if (allDatasets.Count > 0)
            {
                try { _dmService.GetProperties(allDatasets.ToArray(), new[] { "object_string", "object_type" }); }
                catch (Exception e) { Console.WriteLine($"\n[WARN] GetProperties (datasets): {e.Message}"); }
            }

            // ── Step 6: Build result objects ─────────────────────────────────────
            foreach (ClsObject co in clsObjects)
            {
                var obj = new ClassifiedObject
                {
                    IcoUid  = co.ClsObjTag?.Uid ?? "",
                    ClassId = co.ClassId ?? classId,
                    WsoUid  = co.WsoId?.Uid ?? "",
                    WsoName = GetStringProp(co.WsoId, "object_string"),
                    WsoType = GetStringProp(co.WsoId, "object_type"),
                };

                // Collect datasets from all relations
                foreach (string rel in new[] { "IMAN_reference", "IMAN_specification", "IMAN_manifestation", "TC_Attaches" })
                {
                    try
                    {
                        Property p = co.WsoId?.GetProperty(rel);
                        if (p?.ModelObjectArrayValue == null) continue;
                        foreach (ModelObject ds in p.ModelObjectArrayValue)
                            if (ds != null)
                                obj.Datasets.Add(new DatasetInfo
                                {
                                    Uid      = ds.Uid,
                                    Name     = GetStringProp(ds, "object_string"),
                                    Type     = GetStringProp(ds, "object_type"),
                                    Relation = rel,
                                });
                    }
                    catch { }
                }

                if (co.Properties != null)
                    obj.Attributes = ExtractAttributes(co.Properties, _attrNameCache);

                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Extract ClassificationObject[] from GetClassificationObjectsResponse.
        /// Confirmed structure: FIELD Hashtable ClsObjs  (key=string classId, value=ClassificationObject[])
        /// </summary>
        private static ClsObject[] ExtractClassificationObjects(object response)
        {
            if (response == null) return null;
            Type t = response.GetType();

            FieldInfo clsObjsField = t.GetField("ClsObjs", BindingFlags.Public | BindingFlags.Instance);
            if (clsObjsField == null) return null;

            Hashtable ht = clsObjsField.GetValue(response) as Hashtable;
            if (ht == null || ht.Count == 0) return new ClsObject[0];

            var list = new List<ClsObject>();
            foreach (DictionaryEntry entry in ht)
            {
                ClsObject[] arr = entry.Value as ClsObject[];
                if (arr != null) list.AddRange(arr);
                else if (entry.Value is ClsObject single) list.Add(single);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Extract attribute name/value pairs from ClassificationProperty[] via reflection,
        /// since the exact fields depend on the SDK version.
        /// </summary>
        private static List<ClassifiedObjectAttribute> ExtractAttributes(object properties, Dictionary<string, string> nameCache = null)
        {
            var result = new List<ClassifiedObjectAttribute>();
            if (properties == null) return result;

            // properties is ClassificationProperty[] — iterate as Array
            Array arr = properties as Array;
            if (arr == null) return result;

            bool typeDumped = false;
            foreach (object prop in arr)
            {
                if (prop == null) continue;
                Type t = prop.GetType();

                if (!typeDumped) typeDumped = true; // field structure confirmed, no need to dump

                string attrId   = GetFieldString(prop, t, "AttributeId", "Id", "AttrId");
                string attrName = GetFieldString(prop, t, "Name", "AttributeName");

                // Look up display name from cache if the property itself doesn't carry one
                if (string.IsNullOrEmpty(attrName) && attrId != null && nameCache != null)
                    nameCache.TryGetValue(attrId, out attrName);

                string value = GetFieldStringOrArray(prop, t, "Values", "Value", "StringValue");

                if (attrId != null || attrName != null)
                {
                    result.Add(new ClassifiedObjectAttribute
                    {
                        Id    = attrId   ?? "",
                        Name  = attrName ?? attrId ?? "",
                        Value = value    ?? ""
                    });
                }
            }

            return result;
        }

        private static string GetFieldString(object obj, Type t, params string[] names)
        {
            foreach (string name in names)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(obj)?.ToString();
            }
            return null;
        }

        private static string GetFieldStringOrArray(object obj, Type t, params string[] names)
        {
            foreach (string name in names)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                object val = f.GetValue(obj);
                if (val == null) continue;

                // string[] — join directly
                string[] sarr = val as string[];
                if (sarr != null) return string.Join("; ", sarr);

                // ClassificationPropertyValue[] — extract the inner Value field from each element
                Array objArr = val as Array;
                if (objArr != null && objArr.Length > 0)
                {
                    var parts = new List<string>();
                    foreach (object elem in objArr)
                    {
                        if (elem == null) continue;
                        // Try a "Value" field first, then ToString
                        FieldInfo vf = elem.GetType().GetField("Value", BindingFlags.Public | BindingFlags.Instance);
                        string s = vf != null ? vf.GetValue(elem)?.ToString() : elem.ToString();
                        if (!string.IsNullOrEmpty(s)) parts.Add(s);
                    }
                    if (parts.Count > 0) return string.Join("; ", parts);
                    continue; // array was all nulls — try next field name
                }

                return val.ToString();
            }
            return null;
        }

        private static string GetStringProp(ModelObject obj, string propName)
        {
            if (obj == null) return "";
            try
            {
                Property p = obj.GetProperty(propName);
                return p != null ? p.StringValue : "";
            }
            catch { return ""; }
        }

        private List<Cls0NodeDetails> TimedFetchChildDetails(Cls0ClassSvc service, Cls0HierarchyNode parentNode)
        {
            var sw = Stopwatch.StartNew();
            var result = FetchChildDetails(service, parentNode);
            sw.Stop();
            _childrenCalls++;
            _childrenMs += sw.Elapsed.TotalMilliseconds;
            return result;
        }

        private List<ClassAttribute> TimedGetAttributes(ClassificationService service, string classId)
        {
            var sw = Stopwatch.StartNew();
            var result = GetAttributes(service, classId);
            sw.Stop();
            _attributeCalls++;
            _attributeMs += sw.Elapsed.TotalMilliseconds;
            return result;
        }

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
            var result    = new List<Cls0NodeDetails>();

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
            var resp   = service.GetAttributesForClasses(new[] { classId });
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
