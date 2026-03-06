//==================================================
//
//  Copyright 2022 Siemens Digital Industries Software
//
//==================================================

using System;
using System.Collections;
using System.Collections.Generic;

using Teamcenter.Schemas.Soa._2006_03.Exceptions;
using Teamcenter.Services.Strong.Core;
using Teamcenter.Services.Strong.Core._2006_03.Session;
using Teamcenter.Soa;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;
using Teamcenter.Soa.Exceptions;

using WorkspaceObject = Teamcenter.Soa.Client.Model.Strong.WorkspaceObject;
using User            = Teamcenter.Soa.Client.Model.Strong.User;

namespace Teamcenter.ClientX
{
    public class Session
    {
        private static Connection connection;
        private static AppXCredentialManager credentialManager;

        public Session(String host) : this(host, "", "") { }

        public Session(String host, String ssoURL, String appID)
        {
            credentialManager = new AppXCredentialManager(ssoURL, appID);
            connection = new Connection(host, credentialManager);
            connection.ExceptionHandler = new AppXExceptionHandler();
            connection.ModelManager.AddPartialErrorListener(new AppXPartialErrorListener());
            connection.ModelManager.AddModelEventListener(new AppXModelEventListener());
            Connection.AddRequestListener(new AppXRequestListener());
        }

        public static Connection getConnection() { return connection; }

        public User login()
        {
            SessionService sessionService = SessionService.getService(connection);
            try
            {
                String[] credentials = credentialManager.PromptForCredentials();
                String locale = "";
                while (true)
                {
                    try
                    {
                        LoginResponse resp = sessionService.Login(
                            credentials[0], credentials[1], credentials[2],
                            credentials[3], locale, credentials[4]);
                        return resp.User;
                    }
                    catch (InvalidCredentialsException e)
                    {
                        credentials = credentialManager.GetCredentials(e);
                    }
                }
            }
            catch (CanceledOperationException) { }
            return null;
        }

        public void logout()
        {
            SessionService sessionService = SessionService.getService(connection);
            try { sessionService.Logout(); }
            catch (ServiceException) { }
        }

        public static Dictionary<String, String> GetConfigurationFromTCCS(string[] args)
        {
            Dictionary<String, String> argMap = new Dictionary<String, String>();
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 < args.Length && args[i].StartsWith("-"))
                    argMap[args[i]] = args[i + 1];
            }
            if (!argMap.ContainsKey("-host"))
                return argMap;

            String serverAddress = argMap["-host"];
            if (!serverAddress.StartsWith("tccs"))
                return argMap;

            try
            {
                TccsEnvInfo env = null;
                if (serverAddress.StartsWith("tccs://"))
                {
                    env = TccsEnvInfo.GetEnvironment(serverAddress.Substring(7));
                    System.Console.WriteLine("Using the environment " + env.ToString());
                }
                else
                {
                    System.Console.WriteLine("Query TCCS for available Teamcenter environments...");
                    IList<TccsEnvInfo> envs = TccsEnvInfo.GetAllEnvironments();
                    env = ChooseEnvironment(envs);
                }
                argMap["-host"] = env.TeamcenterPath;
                if (env.IsSSOEnabled)
                {
                    argMap["-sso"]   = env.SSOLoginURL;
                    argMap["-appID"] = env.ApplicationID;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Failed to get a TCCS environment. " + e.Message);
                System.Environment.Exit(0);
            }
            return argMap;
        }

        private static TccsEnvInfo ChooseEnvironment(IList<TccsEnvInfo> envs)
        {
            if (envs.Count == 0)
                throw new Exception("TCCS does not have any configured Teamcenter environments.");
            if (envs.Count == 1)
            {
                TccsEnvInfo envi = envs[0];
                System.Console.WriteLine("Using the default environment " + envi.ToString());
                return envi;
            }
            System.Console.WriteLine("Available Teamcenter environments:");
            System.Console.WriteLine(TccsEnvInfo.ListEnvironments(envs));
            Console.Write("Select environment (1-" + envs.Count + "): ");
            String index = Console.ReadLine();
            int i = Int32.Parse(index);
            if (i < 1 || i > envs.Count) System.Environment.Exit(0);
            return envs[i - 1];
        }

        public static String GetOptionalArg(Dictionary<String, String> arguments, String name, String defaultValue)
        {
            String argValue = defaultValue;
            if (arguments.ContainsKey(name))
                argValue = arguments[name];
            return argValue;
        }
    }
}
