using System;
using System.Collections.Generic;

using Teamcenter.Services.Strong.Core;
using Teamcenter.Services.Strong.Query;
using Teamcenter.Services.Strong.Query._2006_03.SavedQuery;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;

using ImanQuery           = Teamcenter.Soa.Client.Model.Strong.ImanQuery;
using SavedQueriesResponse = Teamcenter.Services.Strong.Query._2007_09.SavedQuery.SavedQueriesResponse;
using QueryInput           = Teamcenter.Services.Strong.Query._2008_06.SavedQuery.QueryInput;

namespace SymbolDetective.Detect
{
    /// <summary>
    /// Locates the target Item Revision in Teamcenter using SavedQueryService.
    /// Tries several common TC query names in priority order and falls back
    /// to printing all available queries if none match.
    /// </summary>
    public class SymbolFinder
    {
        private readonly Connection            _connection;
        private readonly DataManagementService _dmService;

        public SymbolFinder(Connection connection)
        {
            _connection = connection;
            _dmService  = DataManagementService.getService(connection);
        }

        /// <summary>
        /// Returns loaded ModelObjects for the given item ID and revision, or null.
        /// </summary>
        public ModelObject[] Find(string itemId, string revision)
        {
            SavedQueryService queryService = SavedQueryService.getService(_connection);

            Console.WriteLine("[INFO] Loading saved queries from server...");
            GetSavedQueriesResponse savedQueries = queryService.GetSavedQueries();

            if (savedQueries?.Queries == null || savedQueries.Queries.Length == 0)
            {
                Console.WriteLine("[WARN] No saved queries found on this server.");
                return null;
            }

            Console.WriteLine($"[INFO] {savedQueries.Queries.Length} saved quer(ies) available.");

            // ── Try Item Revision queries (preferred — more precise) ───────────────
            ModelObject[] result =
                TryQuery(queryService, savedQueries, "Item Revision...",
                    new[] { "Item ID", "Revision" }, new[] { itemId, revision })
                ??
                TryQuery(queryService, savedQueries, "Item Revision",
                    new[] { "Item ID", "Revision" }, new[] { itemId, revision })
                ??
                // Some sites use "item_id" / "item_revision_id" as entry names
                TryQuery(queryService, savedQueries, "Item Revision...",
                    new[] { "item_id", "item_revision_id" }, new[] { itemId, revision })
                ??
                // ── Fall back to Item-level query (returns all revisions) ──────────
                TryQuery(queryService, savedQueries, "Item...",
                    new[] { "Item ID" }, new[] { itemId })
                ??
                TryQuery(queryService, savedQueries, "Item",
                    new[] { "Item ID" }, new[] { itemId });

            if (result == null)
            {
                Console.WriteLine("[WARN] Could not find a matching saved query. Available queries on this server:");
                foreach (var q in savedQueries.Queries)
                    Console.WriteLine($"  - \"{q.Name}\"");
                Console.WriteLine("[HINT] Re-run with -symbol / -rev matching the entry names of one of the above queries.");
            }

            return result;
        }

        private ModelObject[] TryQuery(
            SavedQueryService          queryService,
            GetSavedQueriesResponse    savedQueries,
            string                     queryName,
            string[]                   entryNames,
            string[]                   entryValues)
        {
            // Find the named query in the list (case-insensitive)
            ImanQuery query = null;
            foreach (var q in savedQueries.Queries)
            {
                if (q.Name.Equals(queryName, StringComparison.OrdinalIgnoreCase))
                {
                    query = q.Query;
                    break;
                }
            }
            if (query == null) return null;

            Console.Write($"[INFO] Trying query \"{queryName}\" with");
            for (int i = 0; i < entryNames.Length; i++)
                Console.Write($"  {entryNames[i]}=\"{entryValues[i]}\"");
            Console.WriteLine();

            try
            {
                var input = new QueryInput
                {
                    Query          = query,
                    MaxNumToReturn = 50,
                    LimitList      = new ModelObject[0],
                    Entries        = entryNames,
                    Values         = entryValues
                };

                SavedQueriesResponse execResult = queryService.ExecuteSavedQueries(new[] { input });

                if (execResult?.ArrayOfResults == null || execResult.ArrayOfResults.Length == 0)
                    return null;

                string[] uids = execResult.ArrayOfResults[0].ObjectUIDS;
                if (uids == null || uids.Length == 0)
                {
                    Console.WriteLine($"[INFO]   → 0 results.");
                    return null;
                }

                Console.WriteLine($"[INFO]   → {uids.Length} UID(s) found. Loading objects...");
                ServiceData sd  = _dmService.LoadObjects(uids);
                var         list = new List<ModelObject>();
                for (int i = 0; i < sd.sizeOfPlainObjects(); i++)
                    list.Add(sd.GetPlainObject(i));

                return list.Count > 0 ? list.ToArray() : null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[WARN] Query \"{queryName}\" execution failed: {e.Message}");
                return null;
            }
        }
    }
}
