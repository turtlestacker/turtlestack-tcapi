//==================================================
// 
//  Copyright 2022 Siemens Digital Industries Software
//
//==================================================

using System;
using System.Collections.Generic;
using Teamcenter.ClientX;

using User = Teamcenter.Soa.Client.Model.Strong.User;

namespace Teamcenter.Vendor
{
    /**
     * This sample client application demonstrates some of the basic features of the
     * Teamcenter Services framework and a few of the services.
     * 
     * An instance of the Connection object is created with implementations of the
     * ExceptionHandler, PartialErrorListener, ChangeListener, and DeleteListeners
     * intefaces. This client application performs the following functions: 
     * 1. Establishes a session with the Teamcenter server 
     * 2. Display the contents of the Home Folder 
     * 3. Performs a simple query of the database 
     * 4. Create, revise, and delete an Item
     * 
     */
    public class Vendor
    {


        /**
        * @param args   -help or -h will print out a Usage statement
        */
        public static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                if (args[0].Equals("-help") || args[0].Equals("-h"))
                {
                    System.Console.Out.WriteLine("usage: Vendor [-host HostAdress] [-sso SsoURL  -appID AppID]");
                    System.Console.Out.WriteLine("Where:");
                    System.Console.Out.WriteLine("   host:        The address of the Teacmenter server to conect to, supported protocols:");
                    System.Console.Out.WriteLine("                HTTP(S):  http://localhost:7001/tc");
                    System.Console.Out.WriteLine("                TCCS:     tccs://env_name  Will connect to Teamcenter using the specified environment name");
                    System.Console.Out.WriteLine("                TCCS:     tccs             Will query the TCCS module for available environments");
                    System.Console.Out.WriteLine("                                           TCCS options require the TCCS module to be installed (FMS_HOME environment variable set).");
                    System.Console.Out.WriteLine("                                           If the given TCCS environment is configured with SSO those settings will be used.");
                    System.Console.Out.WriteLine("                If this option is not provided, the client will default to http://localhost:7001/tc.");
                    System.Console.Out.WriteLine("   sso:         The SSO URL, login prompt will be through SSO");
                    System.Console.Out.WriteLine("   appID:       The SSO application ID.");
                    System.Console.Out.WriteLine("                If the SSO arguments are not provided, the client will prompt for credentials at the console.");
                    return;

                }
            }
            Dictionary<String, String> arguments = Session.GetConfigurationFromTCCS(args);
            String serverHost = Session.GetOptionalArg(arguments, "-host", "http://localhost:7001/tc");
            String ssoURL     = Session.GetOptionalArg(arguments, "-sso", "");
            String appID      = Session.GetOptionalArg(arguments, "-appID", "");


            String sname = "";

            Session session = new Session(serverHost, ssoURL, appID);
            VendorManagement vm = new VendorManagement();



            // Establish a session with the Teamcenter Server
            User user = session.login();

            while (!(sname.Equals("7")))
            {

                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Services Available for .NET SOA VendorManagement Testing are :");
                Console.WriteLine("1.createVendors ");
                Console.WriteLine("2.createBidPackages ");
                Console.WriteLine("3.createLineItems ");
                Console.WriteLine("4.deleteVendorRoles ");
                Console.WriteLine("5.deleteVendors ");
                Console.WriteLine("6.createParts ");
                Console.WriteLine("7.Exit ");
                Console.WriteLine("");

                Console.WriteLine("Please enter service number from the above list :");
                Console.WriteLine("");
                sname = Console.ReadLine();

                if (sname.Equals("1"))
                    vm.createVendors();
                if (sname.Equals("2"))
                    vm.createBidPackages();
                if (sname.Equals("3"))
                    vm.createLineItems();
                if (sname.Equals("4"))
                    vm.deleteVendorRoles();
                if (sname.Equals("5"))
                    vm.deleteVendors();
                if (sname.Equals("6"))
                    vm.createParts();
            }

            Console.WriteLine("");

            Console.WriteLine("You have chosen to Exit from services,logging out.....");

            // Terminate the session with the Teamcenter server
            session.logout();

        }
    }
}
    
    
