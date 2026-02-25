using System;
using System.Collections.Generic;

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
                Console.WriteLine("usage: TcExplorer [-host HostAddress] [-sso SsoURL -appID AppID] [-out OutputFile]");
                Console.WriteLine("Where:");
                Console.WriteLine("   host:   Address of the Teamcenter server, e.g. http://localhost:7001/tc");
                Console.WriteLine("           TCCS: tccs://env_name  or  tccs (query available environments)");
                Console.WriteLine("           Default: http://localhost:7001/tc");
                Console.WriteLine("   sso:    SSO URL (if using Single Sign-On)");
                Console.WriteLine("   appID:  SSO application ID");
                Console.WriteLine("   out:    Output JSON file path (default: tc_explorer_output.json)");
                return;
            }

            Dictionary<string, string> arguments = Session.GetConfigurationFromTCCS(args);
            string serverHost = Session.GetOptionalArg(arguments, "-host",  "http://localhost:7001/tc");
            string ssoURL     = Session.GetOptionalArg(arguments, "-sso",   "");
            string appID      = Session.GetOptionalArg(arguments, "-appID", "");
            string outPath    = Session.GetOptionalArg(arguments, "-out",   "tc_explorer_output.json");

            try
            {
                Session session = new Session(serverHost, ssoURL, appID);

                User user = session.login();
                if (user == null)
                {
                    Console.WriteLine("Login cancelled or failed. Exiting.");
                    return;
                }

                ExplorerResult result = new ExplorerResult();

                result.FolderTree = new FolderExplorer(Session.getConnection()).BuildTree(user);

                result.ClassificationTree = new ClassificationExplorer(Session.getConnection()).BuildHierarchy();

                new ConsoleRenderer().Render(result);

                new JsonExporter().Export(result, outPath);

                session.logout();
            }
            catch (SystemException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
