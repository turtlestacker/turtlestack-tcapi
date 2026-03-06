using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Teamcenter.Services.Strong.Classification;
using Teamcenter.Services.Strong.Core;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;

using SymbolDetective.Model;

using ClsObject  = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassificationObject;
using ClassAttr  = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassAttribute;

namespace SymbolDetective.Detect
{
    /// <summary>
    /// Given a ModelObject (the symbol revision), loads every TC property and
    /// relation we can think of, uses reflection to capture anything else the
    /// server returned, looks up classification data, and assembles a SymbolReport.
    /// </summary>
    public class SymbolInspector
    {
        private readonly Connection              _connection;
        private readonly DataManagementService   _dmService;
        private readonly ClassificationService   _classicSvc;

        // Broad list of standard TC property names to request
        private static readonly string[] CoreProps = {
            "object_name", "object_string", "object_type", "object_desc",
            "item_id", "item_revision_id", "sequence_id", "active_seq",
            "creation_date", "last_mod_date", "date_released",
            "owning_user", "owning_group",
            "checked_out", "checked_out_user",
            "release_status_list", "current_status",
            "fnd0ObjectId", "fnd0RevId",
            "revision_list", "rev_numbering_scheme",
        };

        // Relation property names that may return model objects
        private static readonly string[] RelationProps = {
            "IMAN_reference",
            "IMAN_specification",
            "IMAN_manifestation",
            "TC_Attaches",
            "IMAN_based_on",
            "IMAN_UG_scenario",
            "SymbolImageFiles",
            "SymbolFiles",
            "IMAN_classification",
            "release_status_list",
            "items_tag",
        };

        // Properties to load on each related object
        private static readonly string[] RelatedObjProps = {
            "object_name", "object_string", "object_type", "object_desc",
            "original_file_name", "ref_list",
        };

        public SymbolInspector(Connection connection)
        {
            _connection = connection;
            _dmService  = DataManagementService.getService(connection);
            _classicSvc = ClassificationService.getService(connection);
        }

        public SymbolReport Inspect(ModelObject obj, string symbolId, string revision)
        {
            var report = new SymbolReport
            {
                SymbolItemId   = symbolId,
                SymbolRevision = revision,
                Uid            = obj.Uid,
                Type           = obj.GetType().Name,
            };

            // ── Step 1: Load all property names we know about ────────────────────
            var allPropNames = new List<string>(CoreProps);
            allPropNames.AddRange(RelationProps);

            Console.WriteLine("[INFO] Loading properties...");
            try { _dmService.GetProperties(new[] { obj }, allPropNames.ToArray()); }
            catch (Exception e) { Console.WriteLine($"[WARN] GetProperties (broad): {e.Message}"); }

            // ── Step 2: Collect scalar/string properties ──────────────────────────
            report.Name = GetStringProp(obj, "object_string")
                       ?? GetStringProp(obj, "object_name")
                       ?? obj.Uid;

            // Named properties we explicitly requested
            foreach (string name in CoreProps)
            {
                string val = GetStringProp(obj, name);
                if (val != null)
                    report.Properties.Add(new PropValue { Name = name, Value = val });
            }

            // ── Step 3: Reflection dump — capture everything the server returned ──
            Console.WriteLine("[INFO] Reflecting on model object to discover all loaded properties...");
            List<PropValue> reflectedProps = ReflectAllProperties(obj);
            // Merge: add any keys not already in Properties
            var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pv in report.Properties) knownKeys.Add(pv.Name);
            foreach (var pv in reflectedProps)
                if (knownKeys.Add(pv.Name))
                    report.Properties.Add(pv);

            report.Properties.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            // ── Step 4: Collect relation objects ──────────────────────────────────
            Console.WriteLine("[INFO] Collecting relations...");
            var icoObjects = new List<ModelObject>();

            foreach (string rel in RelationProps)
            {
                try
                {
                    Property p = obj.GetProperty(rel);
                    if (p == null) continue;

                    ModelObject[] relObjs = p.ModelObjectArrayValue;
                    if (relObjs == null || relObjs.Length == 0) continue;

                    // Load name/type on each related object
                    try { _dmService.GetProperties(relObjs, RelatedObjProps); }
                    catch { }

                    foreach (ModelObject relObj in relObjs)
                    {
                        if (relObj == null) continue;
                        var ro = new RelatedObject
                        {
                            Uid      = relObj.Uid,
                            Name     = GetStringProp(relObj, "object_string") ?? GetStringProp(relObj, "object_name") ?? "",
                            Type     = GetStringProp(relObj, "object_type") ?? relObj.GetType().Name,
                            Relation = rel,
                        };
                        // Collect any scalar props on the related object
                        foreach (string rp in RelatedObjProps)
                        {
                            string rv = GetStringProp(relObj, rp);
                            if (rv != null) ro.Properties.Add(new PropValue { Name = rp, Value = rv });
                        }
                        report.Relations.Add(ro);

                        // Collect ICO objects separately for classification lookup
                        if (rel == "IMAN_classification")
                            icoObjects.Add(relObj);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[WARN] Relation {rel}: {e.Message}");
                }
            }

            // ── Step 5: Parent Item ───────────────────────────────────────────────
            Console.WriteLine("[INFO] Loading parent item...");
            try
            {
                Property itemsProp = obj.GetProperty("items_tag");
                ModelObject[] items = itemsProp?.ModelObjectArrayValue;
                if (items != null && items.Length > 0)
                {
                    ModelObject parentItem = items[0];
                    try { _dmService.GetProperties(new[] { parentItem }, CoreProps); }
                    catch { }

                    report.ParentItem = new ParentItemInfo
                    {
                        Uid  = parentItem.Uid,
                        Name = GetStringProp(parentItem, "object_string") ?? GetStringProp(parentItem, "object_name") ?? "",
                        Type = GetStringProp(parentItem, "object_type") ?? parentItem.GetType().Name,
                    };
                    foreach (string cp in CoreProps)
                    {
                        string v = GetStringProp(parentItem, cp);
                        if (v != null) report.ParentItem.Properties.Add(new PropValue { Name = cp, Value = v });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] Parent item lookup: {e.Message}");
            }

            // ── Step 6: Classification ────────────────────────────────────────────
            if (icoObjects.Count > 0)
            {
                Console.WriteLine($"[INFO] Found {icoObjects.Count} ICO(s) — loading classification data...");
                report.Classifications = LoadClassification(icoObjects);
            }
            else
            {
                Console.WriteLine("[INFO] No IMAN_classification links found — symbol may not be classified.");
            }

            return report;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Classification
        // ────────────────────────────────────────────────────────────────────────

        private List<ClassEntry> LoadClassification(List<ModelObject> icoObjects)
        {
            var result = new List<ClassEntry>();
            try
            {
                Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

                object clsObjsResp = _classicSvc.GetClassificationObjects(icoObjects.ToArray());
                ClsObject[] clsObjects = ExtractClassificationObjects(clsObjsResp);

                if (clsObjects == null || clsObjects.Length == 0)
                {
                    Console.WriteLine("[INFO] GetClassificationObjects returned no data.");
                    return result;
                }

                // Get attribute display names for each class
                var classIds = new HashSet<string>();
                foreach (ClsObject co in clsObjects)
                    if (!string.IsNullOrEmpty(co.ClassId)) classIds.Add(co.ClassId);

                var attrNameCache = new Dictionary<string, string>();
                foreach (string classId in classIds)
                {
                    try
                    {
                        var attrResp = _classicSvc.GetAttributesForClasses(new[] { classId });
                        if (attrResp?.Attributes == null) continue;
                        foreach (DictionaryEntry entry in attrResp.Attributes)
                        {
                            ClassAttr[] attrs = entry.Value as ClassAttr[];
                            if (attrs == null) continue;
                            foreach (ClassAttr a in attrs)
                            {
                                string key = a.Id.ToString();
                                if (!attrNameCache.ContainsKey(key))
                                    attrNameCache[key] = a.Name ?? key;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[WARN] GetAttributesForClasses ({classId}): {e.Message}");
                    }
                }

                foreach (ClsObject co in clsObjects)
                {
                    var entry = new ClassEntry
                    {
                        IcoUid  = co.ClsObjTag?.Uid ?? "",
                        ClassId = co.ClassId ?? "",
                    };
                    if (co.Properties != null)
                        entry.Attributes = ExtractAttributes(co.Properties, attrNameCache);
                    result.Add(entry);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] Classification lookup failed: {e.Message}");
            }
            return result;
        }

        /// <summary>
        /// Extract ClsObject[] from GetClassificationObjectsResponse.
        /// Uses reflection because the response type is not publicly documented.
        /// Confirmed field: Hashtable ClsObjs (key=classId string, value=ClassificationObject[])
        /// </summary>
        private static ClsObject[] ExtractClassificationObjects(object response)
        {
            if (response == null) return null;
            FieldInfo clsObjsField = response.GetType().GetField("ClsObjs", BindingFlags.Public | BindingFlags.Instance);
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

        private static List<PropValue> ExtractAttributes(object properties, Dictionary<string, string> nameCache)
        {
            var result = new List<PropValue>();
            Array arr = properties as Array;
            if (arr == null) return result;

            foreach (object prop in arr)
            {
                if (prop == null) continue;
                Type t = prop.GetType();

                string attrId   = GetFieldString(prop, t, "AttributeId", "Id", "AttrId");
                string attrName = GetFieldString(prop, t, "Name", "AttributeName");
                if (string.IsNullOrEmpty(attrName) && attrId != null)
                    nameCache?.TryGetValue(attrId, out attrName);

                string value = GetFieldStringOrArray(prop, t, "Values", "Value", "StringValue");

                if (attrId != null || attrName != null)
                    result.Add(new PropValue
                    {
                        Name  = attrName ?? attrId ?? "",
                        Value = value ?? "",
                    });
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Reflection — dump all properties the server loaded into ModelObject
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Walks the type hierarchy of <paramref name="obj"/> looking for an
        /// IDictionary field that holds loaded Property objects, then extracts
        /// every entry as a PropValue. This captures custom properties returned
        /// by the server that we did not explicitly name in CoreProps.
        /// </summary>
        private static List<PropValue> ReflectAllProperties(ModelObject obj)
        {
            var result = new List<PropValue>();
            if (obj == null) return result;

            // Search for a dictionary field by known names, walking base types
            FieldInfo dictField = FindField(obj.GetType(),
                new[] { "_properties", "properties", "m_properties", "_propMap", "propMap" });

            // If not found by name, look for any generic Dictionary<string, ...> field
            if (dictField == null)
                dictField = FindDictionaryField(obj.GetType());

            if (dictField == null)
            {
                Console.WriteLine("[REFLECT] Could not locate internal property dictionary — reflection dump skipped.");
                return result;
            }

            Console.WriteLine($"[REFLECT] Found internal property store: {dictField.DeclaringType?.Name}.{dictField.Name} ({dictField.FieldType.Name})");

            IDictionary dict = dictField.GetValue(obj) as IDictionary;
            if (dict == null) return result;

            foreach (DictionaryEntry entry in dict)
            {
                string key   = entry.Key?.ToString() ?? "";
                string value = PropertyValueToString(entry.Value) ?? "";
                result.Add(new PropValue { Name = key, Value = value });
            }

            Console.WriteLine($"[REFLECT] {result.Count} properties captured via reflection.");
            return result;
        }

        private static FieldInfo FindField(Type startType, string[] names)
        {
            for (Type t = startType; t != null; t = t.BaseType)
            {
                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) return f;
                }
            }
            return null;
        }

        private static FieldInfo FindDictionaryField(Type startType)
        {
            for (Type t = startType; t != null; t = t.BaseType)
            {
                foreach (FieldInfo fi in t.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!fi.FieldType.IsGenericType) continue;
                    Type def = fi.FieldType.GetGenericTypeDefinition();
                    if (def == typeof(Dictionary<,>))
                    {
                        Type[] args = fi.FieldType.GetGenericArguments();
                        if (args[0] == typeof(string)) return fi;
                    }
                }
            }
            return null;
        }

        private static string PropertyValueToString(object val)
        {
            if (val == null) return null;

            // It may be a Property object — extract using known property names
            Property prop = val as Property;
            if (prop != null) return SafePropertyToString(prop);

            // Or it might already be a primitive
            return val.ToString();
        }

        private static string SafePropertyToString(Property p)
        {
            if (p == null) return null;
            try
            {
                string s = p.StringValue;
                if (!string.IsNullOrEmpty(s)) return s;
            }
            catch { }
            try
            {
                ModelObject[] arr = p.ModelObjectArrayValue;
                if (arr != null && arr.Length > 0)
                    return "[" + string.Join(", ", Array.ConvertAll(arr, o => o?.Uid ?? "null")) + "]";
            }
            catch { }
            try { return p.ToString(); }
            catch { return null; }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        private static string GetStringProp(ModelObject obj, string name)
        {
            if (obj == null) return null;
            try
            {
                Property p = obj.GetProperty(name);
                return p != null ? p.StringValue : null;
            }
            catch { return null; }
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

                string[] sarr = val as string[];
                if (sarr != null) return string.Join("; ", sarr);

                Array objArr = val as Array;
                if (objArr != null && objArr.Length > 0)
                {
                    var parts = new List<string>();
                    foreach (object elem in objArr)
                    {
                        if (elem == null) continue;
                        FieldInfo vf = elem.GetType().GetField("Value", BindingFlags.Public | BindingFlags.Instance);
                        string s = vf != null ? vf.GetValue(elem)?.ToString() : elem.ToString();
                        if (!string.IsNullOrEmpty(s)) parts.Add(s);
                    }
                    if (parts.Count > 0) return string.Join("; ", parts);
                    continue;
                }

                return val.ToString();
            }
            return null;
        }
    }
}
