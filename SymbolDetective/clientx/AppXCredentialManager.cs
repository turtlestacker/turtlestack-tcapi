//==================================================
//
//  Copyright 2022 Siemens Digital Industries Software
//
//==================================================

using System;
using System.IO;

using Teamcenter.Schemas.Soa._2006_03.Exceptions;
using Teamcenter.Soa;
using Teamcenter.Soa.Common;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Exceptions;

namespace Teamcenter.ClientX
{
    public class AppXCredentialManager : CredentialManager
    {
        private String name = null;
        private String password = null;
        private String group = "";
        private String role = "";
        private String discriminator = "SoaAppX";
        private SsoCredentials ssoCred = null;
        private int type = SoaConstants.CLIENT_CREDENTIAL_TYPE_STD;

        public AppXCredentialManager() : this("", "") { }

        public AppXCredentialManager(String ssoURL, String appID)
        {
            if (ssoURL != null && ssoURL.Length > 0 &&
                appID  != null && appID.Length  > 0)
            {
                ssoCred = new SsoCredentials(ssoURL, appID);
                type = SoaConstants.CLIENT_CREDENTIAL_TYPE_SSO;
            }
        }

        public int CredentialType { get { return type; } }

        public string[] GetCredentials(InvalidCredentialsException e)
        {
            Console.WriteLine(e.Message);
            if (type == SoaConstants.CLIENT_CREDENTIAL_TYPE_STD)
                return PromptForCredentials();
            return ssoCred.GetCredentials(e);
        }

        public String[] GetCredentials(InvalidUserException e)
        {
            if (type == SoaConstants.CLIENT_CREDENTIAL_TYPE_STD)
            {
                if (name == null) return PromptForCredentials();
            }
            else
            {
                String[] ssoTokens = ssoCred.GetCredentials(e);
                name     = ssoTokens[0];
                password = ssoTokens[1];
            }
            String[] tokens = { name, password, group, role, discriminator };
            return tokens;
        }

        public void SetGroupRole(String group, String role)
        {
            this.group = group;
            this.role  = role;
        }

        public void SetUserPassword(String user, String password, String discriminator)
        {
            this.name          = user;
            this.password      = password;
            this.discriminator = discriminator;
        }

        public String[] PromptForCredentials()
        {
            if (type == SoaConstants.CLIENT_CREDENTIAL_TYPE_SSO)
                return GetCredentials(new InvalidUserException("User does not have a session."));

            try
            {
                Console.WriteLine("Please enter user credentials (return to quit):");
                Console.Write("User Name: ");
                name = Console.ReadLine();
                if (name.Length == 0)
                    throw new CanceledOperationException("");
                Console.Write("Password:  ");
                password = Console.ReadLine();
            }
            catch (IOException e)
            {
                String message = "Failed to get the name and password.\n" + e.Message;
                Console.WriteLine(message);
                throw new CanceledOperationException(message);
            }
            String[] tokens = { name, password, group, role, discriminator };
            return tokens;
        }
    }
}
