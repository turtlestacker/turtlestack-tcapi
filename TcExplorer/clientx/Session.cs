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
        /**
         * Single instance of the Connection object that is shared throughtout
         * the application. This Connection object is needed whenever a Service
         * stub is instantiated.
         */
        private static Connection connection;

        /**
         * The credentialManager is used both by the Session class and the Teamcenter
         * Services Framework to get user credentials.
         *
         */
        private static AppXCredentialManager credentialManager;

        public Session( String host):
            this(host, "", "")
        {
        }

        /**
         * Create an instance of the Session with a connection to the specified
         * server.
         *
         * Add implementations of the ExceptionHandler, PartialErrorListener,
         * ChangeListener, and DeleteListeners.
         *
         * @param host      Address of the host to connect to, http://serverName:port/tc
         * @param ssoURL    SSO Login URL (if emtpy, credentials are prompted for at the command line).
         * @param appID     SSO Application ID.
         */
        public Session(String host, String ssoURL, String appID)
        {
            // Create an instance of the CredentialManager, this is used
            // by the SOA Framework to get the user's credentials when
            // challanged by the server (sesioin timeout on the web tier).
            credentialManager = new AppXCredentialManager(ssoURL, appID);


            // Create the Connection object, no contact is made with the server until a service request is made
            connection = new Connection(host, credentialManager);



            // Add an ExceptionHandler to the Connection, this will handle any
            // InternalServerException, communication errors, xml marshalling errors
            // .etc
            connection.ExceptionHandler = new AppXExceptionHandler();

            // While the above ExceptionHandler is required, all of the following
            // Listeners are optional. Client application can add as many or as few Listeners
            // of each type that they want.

            // Add a Partial Error Listener, this will be notified when ever a
            // a service returns partial errors.
            connection.ModelManager.AddPartialErrorListener(new AppXPartialErrorListener());

            // Add a Change and Delete Listener, this will be notified when ever a
            // a service returns model objects that have been updated or deleted.
            connection.ModelManager.AddModelEventListener(new AppXModelEventListener());

            // Add a Request Listener, this will be notified before and after each 
            // service request is sent to the server.
            Connection.AddRequestListener(new AppXRequestListener());
        }

        /**
         * Get the single Connection object for the application
         *
         * @return  connection
         */
        public static Connection getConnection()
        {
            return connection;
        }

        /**
         * Login to the Teamcenter Server
         *
         */
        public User login()
        {
            // Get the service stub
            SessionService sessionService = SessionService.getService(connection);

            try
            {
                // Prompt for credentials until they are right, or until user cancels
                String[] credentials = credentialManager.PromptForCredentials();
                String locale = "";
                while (true)
                {
                    try
                    {

                        // *****************************
                        // Execute the service operation
                        // *****************************
                        LoginResponse resp = sessionService.Login(credentials[0], credentials[1], credentials[2], credentials[3], locale, credentials[4]);

                        return resp.User;
                    }
                    catch (InvalidCredentialsException e)
                    {
                        credentials = credentialManager.GetCredentials(e);
                    }
                }
            }
            // User canceled the operation, don't need to tell him again
            catch (CanceledOperationException /*e*/) { }

            // Exit the application
            //System.exit(0);
            return null;
        }

        /**
         * Terminate the session with the Teamcenter Server
         *
         */
        public void logout()
        {
            // Get the service stub
            SessionService sessionService = SessionService.getService(connection);
            try
            {
                // *****************************
                // Execute the service operation
                // *****************************
                sessionService.Logout();
            }
            catch (ServiceException /*e*/) { }
        }

        /**
         * Print some basic information for a list of objects
         *
         * @param objects
         */
        public static void printObjects(ModelObject[] objects)
        {
            if (objects == null)
                return;


            // Ensure that the referenced User objects that we will use below are loaded
            getUsers(objects);

            Console.WriteLine("Name\t\tOwner\t\tLast Modified");
            Console.WriteLine("====\t\t=====\t\t=============");
            for (int i = 0; i < objects.Length; i++)
            {
                if (!(objects[i] is WorkspaceObject))
                    continue;

                WorkspaceObject wo = (WorkspaceObject)objects[i];
                try
                {
                    String name = wo.Object_string;
                    User owner = (User)wo.Owning_user;
                    DateTime lastModified = wo.Last_mod_date;

                    Console.WriteLine(name + "\t" + owner.User_name + "\t" + lastModified.ToString());
                }
                catch (NotLoadedException e)
                {
                    // Print out a message, and skip to the next item in the folder
                    // Could do a DataManagementService.getProperties call at this point
                    Console.WriteLine(e.Message);
                    Console.WriteLine("The Object Property Policy ($TC_DATA/soa/policies/Default.xml) is not configured with this property.");
                }
            }

        }


        private static void getUsers(ModelObject[] objects)
        {
            if (objects == null)
                return;

            DataManagementService dmService = DataManagementService.getService(Session.getConnection());
            ArrayList unKnownUsers = new ArrayList();
            for (int i = 0; i < objects.Length; i++)
            {
                if (!(objects[i] is WorkspaceObject))
                    continue;

                WorkspaceObject wo = (WorkspaceObject)objects[i];

                User owner = null;
                try
                {
                    owner = (User)wo.Owning_user;
                    String userName = owner.User_name;
                }
                catch (NotLoadedException /*e*/)
                {
                    if (owner != null)
                        unKnownUsers.Add(owner);
                }
            }
            User[] users = new User[unKnownUsers.Count];
            unKnownUsers.CopyTo(users);
            String[] attributes = { "user_name" };


            // *****************************
            // Execute the service operation
            // *****************************
            dmService.GetProperties(users, attributes);


        }

        public static Dictionary<String, String> GetConfigurationFromTCCS(string[] args)
        {
            Dictionary<String, String> argMap = new Dictionary<String, String>();
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 < args.Length && args[i].StartsWith("-"))
                {
                    argMap[args[i]] = args[i + 1];
                }
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
                    System.Console.WriteLine("Query TCCS for available Teamcenter enviorments to connect to...");
                    IList<TccsEnvInfo> envs = TccsEnvInfo.GetAllEnvironments();
                    env = ChooseEnvironment(envs);
                }
                argMap["-host"] = env.TeamcenterPath;
                if (env.IsSSOEnabled)
                {
                    argMap["-sso"] = env.SSOLoginURL;
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
            {
                throw new Exception("TCCS does not have any configured Teamcenter environments.");
            }
            if (envs.Count == 1)
            {
                TccsEnvInfo envi = envs[0];
                System.Console.WriteLine("Using the default environment " + envi.ToString());
                return envi;
            }

            System.Console.WriteLine("Available Teamcenter environments:");
            System.Console.WriteLine(TccsEnvInfo.ListEnvironments(envs));
            Console.Write("Select Teamcenter environment to connect to (1-" + envs.Count + "): ");
            String index = Console.ReadLine();
            int i = Int32.Parse(index);
            if (i < 1 || i > envs.Count)
                System.Environment.Exit(0);
            TccsEnvInfo env = envs[i - 1];
            return env;
        }

        public static String GetOptionalArg(Dictionary<String, String> arguments, String name, String defaultValue)
        {
            String argValue = defaultValue;
            if (arguments.ContainsKey(name))
            {
                argValue = arguments[name];
            }
            return argValue;

        }
    }
}
