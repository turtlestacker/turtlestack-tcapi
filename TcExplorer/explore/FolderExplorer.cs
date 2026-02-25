using System;
using System.Collections.Generic;

using Teamcenter.Services.Strong.Core;
using Teamcenter.Soa.Client;
using Teamcenter.Soa.Client.Model;
using Teamcenter.Soa.Exceptions;
using TcExplorer.Model;

using Folder          = Teamcenter.Soa.Client.Model.Strong.Folder;
using WorkspaceObject = Teamcenter.Soa.Client.Model.Strong.WorkspaceObject;
using User            = Teamcenter.Soa.Client.Model.Strong.User;

namespace TcExplorer.Explore
{
    public class FolderExplorer
    {
        private readonly DataManagementService _dmService;

        public FolderExplorer(Connection connection)
        {
            _dmService = DataManagementService.getService(connection);
        }

        public FolderNode BuildTree(User user)
        {
            Folder homeFolder;
            try
            {
                homeFolder = user.Home_folder;
            }
            catch (NotLoadedException e)
            {
                Console.WriteLine("[WARN] Could not read home_folder: " + e.Message);
                Console.WriteLine("       The Object Property Policy ($TC_DATA/soa/policies/Default.xml) may not include 'home_folder'.");
                FolderNode empty = new FolderNode { Name = "(home folder not available)", Type = "Folder", Uid = "" };
                return empty;
            }

            return WalkFolder(homeFolder);
        }

        private FolderNode WalkFolder(Folder folder)
        {
            // Load the folder's own name/type and its contents in one call
            WorkspaceObject[] contents = LoadContents(folder);

            FolderNode node = new FolderNode
            {
                Name = GetStringProperty(folder, "object_string"),
                Type = GetStringProperty(folder, "object_type"),
                Uid  = folder.Uid
            };
            if (contents == null || contents.Length == 0)
                return node;

            // Batch-load properties for all children in one round-trip
            _dmService.GetProperties(contents, new[] { "object_string", "object_type" });

            foreach (WorkspaceObject child in contents)
            {
                if (child is Folder subFolder)
                {
                    node.Children.Add(WalkFolder(subFolder));
                }
                else
                {
                    node.Items.Add(new ItemInfo
                    {
                        Name = GetStringProperty(child, "object_string"),
                        Type = GetStringProperty(child, "object_type"),
                        Uid  = child.Uid
                    });
                }
            }

            return node;
        }

        private WorkspaceObject[] LoadContents(Folder folder)
        {
            try
            {
                _dmService.GetProperties(new ModelObject[] { folder }, new[] { "contents", "object_string", "object_type" });
                return folder.Contents;
            }
            catch (NotLoadedException e)
            {
                Console.WriteLine("[WARN] Could not load contents of folder " + folder.Uid + ": " + e.Message);
                return null;
            }
        }

        private static string GetStringProperty(ModelObject obj, string propName)
        {
            try
            {
                Property prop = obj.GetProperty(propName);
                return prop != null ? prop.StringValue : "(unknown)";
            }
            catch (NotLoadedException)
            {
                return "(unknown)";
            }
        }
    }
}
