using System;
using System.Collections.Generic;
using System.Diagnostics;

using Teamcenter.ClientX;

using SymbolDetective.Detect;
using SymbolDetective.Model;
using SymbolDetective.Output;

using Teamcenter.Soa.Client.Model;
using User = Teamcenter.Soa.Client.Model.Strong.User;

namespace SymbolDetective
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && (args[0].Equals("-help") || args[0].Equals("-h")))
            {
                Console.WriteLine("usage: SymbolDetective [-host HostAddress] [-sso SsoURL -appID AppID]");
                Console.WriteLine("                       [-symbol SymbolId] [-rev Revision] [-out OutputFile]");
                Console.WriteLine("Where:");
                Console.WriteLine("   host:    TC server URL, e.g. https://tcweb03.dev.rolls-royce-smr.com:3000/tc");
                Console.WriteLine("            TCCS: tccs://env_name  or  tccs");
                Console.WriteLine("   sso:     SSO URL (if using Single Sign-On)");
                Console.WriteLine("   appID:   SSO application ID");
                Console.WriteLine("   symbol:  Item ID of the symbol to investigate");
                Console.WriteLine("   name:    Object name to search for (used when -symbol not given)");
                Console.WriteLine("            (default: Control_3Way_dist_mode_General)");
                Console.WriteLine("   rev:     Revision to search for (default: A)");
                Console.WriteLine("   out:     Output JSON file path");
                Console.WriteLine("            (default: output/symbol_detective_output.json)");
                return;
            }

            Dictionary<string, string> arguments = Session.GetConfigurationFromTCCS(args);
            string serverHost = Session.GetOptionalArg(arguments, "-host",   "http://localhost:7001/tc");
            string ssoURL     = Session.GetOptionalArg(arguments, "-sso",    "");
            string appID      = Session.GetOptionalArg(arguments, "-appID",  "");
            string symbolId   = Session.GetOptionalArg(arguments, "-symbol", "");
            string symbolName = Session.GetOptionalArg(arguments, "-name",   "Control_3Way_dist_mode_General");
            string revision   = Session.GetOptionalArg(arguments, "-rev",    "A");
            string outPath    = Session.GetOptionalArg(arguments, "-out",    "output/symbol_detective_output.json");

            var totalTimer = Stopwatch.StartNew();

            try
            {
                // ── Login ────────────────────────────────────────────────────────
                var sw = Stopwatch.StartNew();
                Session session = new Session(serverHost, ssoURL, appID);
                User user = session.login();
                sw.Stop();

                if (user == null)
                {
                    Console.WriteLine("Login cancelled or failed. Exiting.");
                    return;
                }
                Console.WriteLine($"[TIMING] Login: {sw.Elapsed.TotalSeconds:F2}s");

                // ── Find the symbol ──────────────────────────────────────────────
                sw.Restart();
                var finder = new SymbolFinder(Session.getConnection());
                ModelObject[] foundObjects;
                string searchLabel;
                if (!string.IsNullOrEmpty(symbolId))
                {
                    Console.WriteLine($"\n[INFO] Searching by item_id=\"{symbolId}\"  revision=\"{revision}\"");
                    foundObjects = finder.Find(symbolId, revision);
                    searchLabel  = symbolId;
                }
                else
                {
                    Console.WriteLine($"\n[INFO] Searching by name=\"{symbolName}\"  revision=\"{revision}\"");
                    foundObjects = finder.FindByName(symbolName, revision);
                    searchLabel  = symbolName;
                }
                sw.Stop();
                Console.WriteLine($"[TIMING] Search: {sw.Elapsed.TotalSeconds:F2}s");

                if (foundObjects == null || foundObjects.Length == 0)
                {
                    Console.WriteLine($"[WARN] No objects found. Check query names printed above, or adjust -symbol / -name / -rev.");
                    session.logout();
                    return;
                }

                Console.WriteLine($"[INFO] Found {foundObjects.Length} object(s) — inspecting all.");

                // ── Inspect all ───────────────────────────────────────────────────
                sw.Restart();
                var inspector = new SymbolInspector(Session.getConnection());
                var reports   = new List<SymbolReport>();
                for (int i = 0; i < foundObjects.Length; i++)
                {
                    Console.WriteLine($"\n[INFO] ── Result {i + 1}/{foundObjects.Length} ──────────────────────────");
                    reports.Add(inspector.Inspect(foundObjects[i], searchLabel, revision));
                }
                sw.Stop();
                Console.WriteLine($"[TIMING] Inspect: {sw.Elapsed.TotalSeconds:F2}s");

                // ── Render + export ──────────────────────────────────────────────
                sw.Restart();
                var renderer = new ConsoleRenderer();
                foreach (var report in reports) renderer.Render(report);
                new JsonExporter().Export(reports, outPath);
                sw.Stop();
                Console.WriteLine($"[TIMING] Render + export: {sw.Elapsed.TotalSeconds:F2}s");

                totalTimer.Stop();
                Console.WriteLine($"[TIMING] Total elapsed:   {totalTimer.Elapsed.TotalSeconds:F2}s");

                session.logout();
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
