using System;
using System.Collections.Generic;
using System.Diagnostics;

using Teamcenter.ClientX;
using TcExplorer.Explore;
using TcExplorer.Model;
using TcExplorer.Output;

using User = Teamcenter.Soa.Client.Model.Strong.User;

namespace TcExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && (args[0].Equals("-help") || args[0].Equals("-h")))
            {
                Console.WriteLine("usage: TcExplorer [-host HostAddress] [-sso SsoURL -appID AppID] [-out OutputFile] [-limit N]");
                Console.WriteLine("Where:");
                Console.WriteLine("   host:   Address of the Teamcenter server, e.g. http://localhost:7001/tc");
                Console.WriteLine("           TCCS: tccs://env_name  or  tccs (query available environments)");
                Console.WriteLine("           Default: http://localhost:7001/tc");
                Console.WriteLine("   sso:    SSO URL (if using Single Sign-On)");
                Console.WriteLine("   appID:  SSO application ID");
                Console.WriteLine("   out:    Output JSON file path (default: tc_explorer_output.json)");
                Console.WriteLine("   limit:  Stop after N classification nodes (0 = unlimited, default: 0)");
                return;
            }

            Dictionary<string, string> arguments = Session.GetConfigurationFromTCCS(args);
            string serverHost = Session.GetOptionalArg(arguments, "-host",  "http://localhost:7001/tc");
            string ssoURL     = Session.GetOptionalArg(arguments, "-sso",   "");
            string appID      = Session.GetOptionalArg(arguments, "-appID", "");
            string outPath    = Session.GetOptionalArg(arguments, "-out",   "tc_explorer_output.json");
            string limitStr   = Session.GetOptionalArg(arguments, "-limit", "0");
            int    nodeLimit  = 0;
            int.TryParse(limitStr, out nodeLimit);

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
                Console.WriteLine($"[TIMING] Login:              {sw.Elapsed.TotalSeconds:F2}s");

                ExplorerResult result = new ExplorerResult();

                // ── Folder tree ──────────────────────────────────────────────────
                // Folder data not required.
                // sw.Restart();
                // result.FolderTree = new FolderExplorer(Session.getConnection()).BuildTree(user);
                // sw.Stop();
                // Console.WriteLine($"[TIMING] Folder tree:        {sw.Elapsed.TotalSeconds:F2}s");

                // ── Classification hierarchy ─────────────────────────────────────
                sw.Restart();
                var classExplorer = new ClassificationExplorer(Session.getConnection());
                if (nodeLimit > 0)
                    Console.WriteLine($"[INFO]   Classification node limit: {nodeLimit}");
                result.ClassificationTree = classExplorer.BuildHierarchy(nodeLimit);
                sw.Stop();
                Console.WriteLine($"[TIMING] Classification:     {sw.Elapsed.TotalSeconds:F2}s");
                classExplorer.PrintCallStats();

                // ── Render + export ──────────────────────────────────────────────
                sw.Restart();
                new ConsoleRenderer().Render(result);
                new JsonExporter().Export(result, outPath);
                sw.Stop();
                Console.WriteLine($"[TIMING] Render + export:    {sw.Elapsed.TotalSeconds:F2}s");

                totalTimer.Stop();
                Console.WriteLine($"[TIMING] Total elapsed:      {totalTimer.Elapsed.TotalSeconds:F2}s");

                session.logout();
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
