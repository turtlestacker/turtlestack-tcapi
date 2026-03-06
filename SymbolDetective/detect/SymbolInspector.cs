using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Teamcenter.Services.Strong.Classification;
using Teamcenter.Services.Strong.Core;
using Teamcenter.Services.Strong.Query;
using Teamcenter.Services.Strong.Query._2006_03.SavedQuery;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;

using SymbolDetective.Model;

using ClsObject = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassificationObject;
using ClassAttr = Teamcenter.Services.Strong.Classification._2007_01.Classification.ClassAttribute;
using ImanQuery           = Teamcenter.Soa.Client.Model.Strong.ImanQuery;
using SavedQueriesResponse = Teamcenter.Services.Strong.Query._2007_09.SavedQuery.SavedQueriesResponse;
using QueryInput           = Teamcenter.Services.Strong.Query._2008_06.SavedQuery.QueryInput;

namespace SymbolDetective.Detect
{
    /// <summary>
    /// Given a ModelObject (the symbol revision), loads every TC property and
    /// relation we can think of, uses reflection to capture anything else the
    /// server returned, discovers files inside datasets, looks up the parent item,
    /// gets classification data, and assembles a SymbolReport.
    /// </summary>
    public class SymbolInspector
    {
        private readonly Connection            _connection;
        private readonly DataManagementService _dmService;
        private readonly ClassificationService _classicSvc;

        // Standard properties to request on the revision object
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

        // Properties to load on each related object (datasets etc.)
        private static readonly string[] RelatedObjProps = {
            "object_name", "object_string", "object_type", "object_desc",
            "creation_date", "last_mod_date",
            "ref_list",          // Named references (files) on Dataset objects
        };

        // Properties to load on ImanFile objects
        private static readonly string[] FileProps = {
            "original_file_name", "file_location", "object_type", "object_name",
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

            // ── Step 1: Broad GetProperties on the revision ───────────────────────
            var allPropNames = new List<string>(CoreProps);
            allPropNames.AddRange(RelationProps);

            Console.WriteLine("[INFO] Loading revision properties...");
            try { _dmService.GetProperties(new[] { obj }, allPropNames.ToArray()); }
            catch (Exception e) { Console.WriteLine($"[WARN] GetProperties (revision): {e.Message}"); }

            // ── Step 2: Extract named CoreProps as scalars ─────────────────────────
            report.Name = SafeGetString(obj, "object_string")
                       ?? SafeGetString(obj, "object_name")
                       ?? obj.Uid;
            report.Type = SafeGetString(obj, "object_type") ?? report.Type;

            foreach (string name in CoreProps)
            {
                string val = SafeGetString(obj, name);
                if (val != null)
                    report.Properties.Add(new PropValue { Name = name, Value = val });
            }

            // ── Step 3: Reflection dump — capture everything the server returned ───
            Console.WriteLine("[INFO] Reflecting on model object internal property store...");
            List<PropValue> reflected = ReflectAllProperties(obj);
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pv in report.Properties) seenKeys.Add(pv.Name);
            foreach (var pv in reflected)
                if (seenKeys.Add(pv.Name))
                    report.Properties.Add(pv);

            report.Properties.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

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

                    // Load name/type + ref_list on each related object
                    try { _dmService.GetProperties(relObjs, RelatedObjProps); }
                    catch { }

                    foreach (ModelObject relObj in relObjs)
                    {
                        if (relObj == null) continue;
                        var ro = new RelatedObject
                        {
                            Uid      = relObj.Uid,
                            Name     = SafeGetString(relObj, "object_string") ?? SafeGetString(relObj, "object_name") ?? "",
                            Type     = SafeGetString(relObj, "object_type") ?? relObj.GetType().Name,
                            Relation = rel,
                        };
                        foreach (string rp in new[] { "object_name", "object_string", "object_type", "object_desc", "creation_date", "last_mod_date" })
                        {
                            string rv = SafeGetString(relObj, rp);
                            if (rv != null) ro.Properties.Add(new PropValue { Name = rp, Value = rv });
                        }

                        // ── Step 4b: File discovery via ref_list ─────────────────
                        ro.Files = DiscoverFiles(relObj);

                        report.Relations.Add(ro);

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
            Console.WriteLine("[INFO] Locating parent item...");
            ModelObject parentObj = GetParentItem(obj, symbolId);
            if (parentObj != null)
            {
                try { _dmService.GetProperties(new[] { parentObj }, CoreProps); }
                catch { }

                report.ParentItem = new ParentItemInfo
                {
                    Uid  = parentObj.Uid,
                    Name = SafeGetString(parentObj, "object_string") ?? SafeGetString(parentObj, "object_name") ?? "",
                    Type = SafeGetString(parentObj, "object_type") ?? parentObj.GetType().Name,
                };
                foreach (string cp in CoreProps)
                {
                    string v = SafeGetString(parentObj, cp);
                    if (v != null)
                        report.ParentItem.Properties.Add(new PropValue { Name = cp, Value = v });
                }

                // Reflect on parent item too
                List<PropValue> parentReflected = ReflectAllProperties(parentObj);
                var parentSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pv in report.ParentItem.Properties) parentSeen.Add(pv.Name);
                foreach (var pv in parentReflected)
                    if (parentSeen.Add(pv.Name))
                        report.ParentItem.Properties.Add(pv);

                report.ParentItem.Properties.Sort((a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                Console.WriteLine("[INFO] Parent item could not be resolved.");
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
        // File discovery
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// For a dataset object, reads its ref_list property to find attached
        /// ImanFile objects, then loads original_file_name on each.
        /// </summary>
        private List<DatasetFile> DiscoverFiles(ModelObject dataset)
        {
            var result = new List<DatasetFile>();
            try
            {
                Property refProp = dataset.GetProperty("ref_list");
                if (refProp == null) return result;

                ModelObject[] fileObjs = refProp.ModelObjectArrayValue;
                if (fileObjs == null || fileObjs.Length == 0) return result;

                Console.WriteLine($"[INFO]   Dataset {dataset.Uid} has {fileObjs.Length} named reference(s).");

                try { _dmService.GetProperties(fileObjs, FileProps); }
                catch { }

                foreach (ModelObject fo in fileObjs)
                {
                    if (fo == null) continue;
                    var df = new DatasetFile
                    {
                        Uid          = fo.Uid,
                        FileName     = SafeGetString(fo, "original_file_name") ?? SafeGetString(fo, "object_name") ?? "",
                        FileType     = SafeGetString(fo, "object_type") ?? fo.GetType().Name,
                        FileLocation = SafeGetString(fo, "file_location") ?? "",
                    };
                    result.Add(df);
                    Console.WriteLine($"[INFO]     File: {df.FileName}  [{df.FileType}]");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] DiscoverFiles ({dataset.Uid}): {e.Message}");
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Parent Item resolution — multi-strategy with fallbacks
        // ────────────────────────────────────────────────────────────────────────

        private ModelObject GetParentItem(ModelObject revObj, string itemId)
        {
            // Strategy 1: items_tag via GetProperties (dedicated call)
            try
            {
                _dmService.GetProperties(new[] { revObj }, new[] { "items_tag" });
                Property p = revObj.GetProperty("items_tag");
                if (p != null)
                {
                    // 1a. Array (some TC versions wrap single refs as 1-element arrays)
                    try
                    {
                        ModelObject[] arr = p.ModelObjectArrayValue;
                        if (arr != null && arr.Length > 0 && arr[0] != null)
                        {
                            Console.WriteLine($"[INFO] Parent item found via items_tag array: uid={arr[0].Uid}");
                            return arr[0];
                        }
                    }
                    catch { }

                    // 1b. StringValue might be the UID
                    try
                    {
                        string uid = p.StringValue;
                        if (!string.IsNullOrEmpty(uid) && uid.Length > 5)
                        {
                            Console.WriteLine($"[INFO] items_tag string value: \"{uid}\" — loading...");
                            ServiceData sd = _dmService.LoadObjects(new[] { uid });
                            if (sd.sizeOfPlainObjects() > 0) return sd.GetPlainObject(0);
                        }
                    }
                    catch { }

                    // 1c. Reflect on Property to find a ModelObject-typed field
                    ModelObject reflected = ReflectModelObjectFromProperty(p);
                    if (reflected != null)
                    {
                        Console.WriteLine($"[INFO] Parent item found via Property reflection: uid={reflected.Uid}");
                        return reflected;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] items_tag lookup: {e.Message}");
            }

            // Strategy 2: SavedQuery for "Item..." using the known item_id
            Console.WriteLine($"[INFO] items_tag unresolved — falling back to SavedQuery for item_id=\"{itemId}\"...");
            return FindItemByQuery(itemId);
        }

        private ModelObject FindItemByQuery(string itemId)
        {
            try
            {
                SavedQueryService qSvc = SavedQueryService.getService(_connection);
                GetSavedQueriesResponse resp = qSvc.GetSavedQueries();
                if (resp?.Queries == null) return null;

                ImanQuery q       = null;
                string[]  entries = null;
                string[]  values  = null;

                foreach (var sq in resp.Queries)
                {
                    if (sq.Name.Equals("Item...", StringComparison.OrdinalIgnoreCase) ||
                        sq.Name.Equals("Item",    StringComparison.OrdinalIgnoreCase))
                    {
                        q       = sq.Query;
                        entries = new[] { "Item ID" };
                        values  = new[] { itemId };
                        break;
                    }
                }
                if (q == null)
                {
                    Console.WriteLine("[INFO] No \"Item...\" saved query found — parent item unresolved.");
                    return null;
                }

                var input = new QueryInput
                {
                    Query          = q,
                    MaxNumToReturn = 5,
                    LimitList      = new ModelObject[0],
                    Entries        = entries,
                    Values         = values,
                };

                SavedQueriesResponse execResult = qSvc.ExecuteSavedQueries(new[] { input });
                string[] uids = execResult?.ArrayOfResults?[0]?.ObjectUIDS;
                if (uids == null || uids.Length == 0)
                {
                    Console.WriteLine("[INFO] SavedQuery for parent item returned 0 results.");
                    return null;
                }

                Console.WriteLine($"[INFO] Parent item found via SavedQuery: uid={uids[0]}");
                ServiceData sd = _dmService.LoadObjects(uids);
                return sd.sizeOfPlainObjects() > 0 ? sd.GetPlainObject(0) : null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] FindItemByQuery: {e.Message}");
                return null;
            }
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

                var attrNameCache = new Dictionary<string, string>();
                var classIds = new HashSet<string>();
                foreach (ClsObject co in clsObjects)
                    if (!string.IsNullOrEmpty(co.ClassId)) classIds.Add(co.ClassId);

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

        private static ClsObject[] ExtractClassificationObjects(object response)
        {
            if (response == null) return null;
            FieldInfo f = response.GetType().GetField("ClsObjs", BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return null;
            Hashtable ht = f.GetValue(response) as Hashtable;
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
                    result.Add(new PropValue { Name = attrName ?? attrId ?? "", Value = value ?? "" });
            }
            return result;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Reflection — dump ALL properties the server loaded into ModelObject
        // ────────────────────────────────────────────────────────────────────────

        private static List<PropValue> ReflectAllProperties(ModelObject obj)
        {
            var result = new List<PropValue>();
            if (obj == null) return result;

            FieldInfo dictField = FindFieldByName(obj.GetType(),
                new[] { "_properties", "properties", "m_properties", "_propMap", "propMap" });

            if (dictField == null)
                dictField = FindDictionaryField(obj.GetType());

            if (dictField == null)
            {
                Console.WriteLine("[REFLECT] Could not locate internal property dictionary.");
                return result;
            }

            Console.WriteLine($"[REFLECT] Using field: {dictField.DeclaringType?.Name}.{dictField.Name}");

            IDictionary dict = dictField.GetValue(obj) as IDictionary;
            if (dict == null) return result;

            foreach (DictionaryEntry entry in dict)
            {
                string key   = entry.Key?.ToString() ?? "";
                string value = ConvertPropertyToString(entry.Value) ?? "";
                result.Add(new PropValue { Name = key, Value = value });
            }

            Console.WriteLine($"[REFLECT] {result.Count} properties captured.");
            return result;
        }

        /// <summary>
        /// Convert a Property (or any object) to a readable string.
        /// Tries StringValue, ModelObjectArrayValue, then reflects on ALL public
        /// property getters of the Property type to handle dates, bools, ints.
        /// </summary>
        private static string ConvertPropertyToString(object val)
        {
            if (val == null) return null;

            Property prop = val as Property;
            if (prop == null) return val.ToString();

            // 1. StringValue
            try
            {
                string s = prop.StringValue;
                if (!string.IsNullOrEmpty(s)) return s;
            }
            catch { }

            // 2. ModelObjectArrayValue → UIDs
            try
            {
                ModelObject[] arr = prop.ModelObjectArrayValue;
                if (arr != null && arr.Length > 0)
                    return "[" + string.Join(", ", Array.ConvertAll(arr, o => o?.Uid ?? "null")) + "]";
            }
            catch { }

            // 3. Reflect on ALL public readable properties of Property to find dates, ints, bools
            Type t = prop.GetType();
            foreach (System.Reflection.PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (pi.Name == "StringValue" || pi.Name == "ModelObjectArrayValue") continue;
                if (!pi.CanRead) continue;
                try
                {
                    object pval = pi.GetValue(prop);
                    if (pval == null) continue;

                    // Skip zero/false/empty defaults that carry no information
                    if (pval is int    iv && iv == 0) continue;
                    if (pval is bool   bv && !bv)     continue;
                    if (pval is double dv && dv == 0) continue;
                    if (pval is Array  av && av.Length == 0) continue;
                    if (pval is string sv && string.IsNullOrEmpty(sv)) continue;

                    // Skip DateTime.MinValue (unset)
                    if (pval is DateTime dt && dt == DateTime.MinValue) continue;

                    string result = pval.ToString();
                    if (!string.IsNullOrEmpty(result) && result != pi.PropertyType.FullName)
                        return result;
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Reflect on a Property object to find a single ModelObject-typed field or property.
        /// Used to resolve items_tag which is a scalar object reference, not an array.
        /// </summary>
        private static ModelObject ReflectModelObjectFromProperty(Property p)
        {
            if (p == null) return null;
            Type t = p.GetType();

            foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!typeof(ModelObject).IsAssignableFrom(fi.FieldType)) continue;
                try
                {
                    ModelObject mo = fi.GetValue(p) as ModelObject;
                    if (mo != null) { Console.WriteLine($"[INFO] items_tag via field reflection ({fi.Name})"); return mo; }
                }
                catch { }
            }
            foreach (System.Reflection.PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typeof(ModelObject).IsAssignableFrom(pi.PropertyType)) continue;
                try
                {
                    ModelObject mo = pi.GetValue(p) as ModelObject;
                    if (mo != null) { Console.WriteLine($"[INFO] items_tag via property reflection ({pi.Name})"); return mo; }
                }
                catch { }
            }
            return null;
        }

        private static FieldInfo FindFieldByName(Type startType, string[] names)
        {
            for (Type t = startType; t != null; t = t.BaseType)
                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) return f;
                }
            return null;
        }

        private static FieldInfo FindDictionaryField(Type startType)
        {
            for (Type t = startType; t != null; t = t.BaseType)
                foreach (FieldInfo fi in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!fi.FieldType.IsGenericType) continue;
                    if (fi.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) continue;
                    if (fi.FieldType.GetGenericArguments()[0] == typeof(string)) return fi;
                }
            return null;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────────

        private static string SafeGetString(ModelObject obj, string name)
        {
            if (obj == null) return null;
            try
            {
                Property p = obj.GetProperty(name);
                if (p == null) return null;
                string s = p.StringValue;
                return string.IsNullOrEmpty(s) ? null : s;
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
