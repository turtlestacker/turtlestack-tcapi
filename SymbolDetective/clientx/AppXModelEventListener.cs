//==================================================
//
//  Copyright 2022 Siemens Digital Industries Software
//
//==================================================

using System;

using Teamcenter.Soa.Client.Model;
using Teamcenter.Soa.Exceptions;

namespace Teamcenter.ClientX
{
    public class AppXModelEventListener : ModelEventListener
    {
        override public void LocalObjectChange(ModelObject[] objects)
        {
            if (objects.Length == 0) return;
            System.Console.WriteLine("");
            System.Console.WriteLine("Modified Objects handled in AppXModelEventListener.LocalObjectChange");
            System.Console.WriteLine("The following objects have been updated in the client data model:");
            for (int i = 0; i < objects.Length; i++)
            {
                String uid  = objects[i].Uid;
                String type = objects[i].GetType().Name;
                String name = "";
                if (objects[i].GetType().Name.Equals("WorkspaceObject"))
                {
                    try { name = objects[i].GetProperty("object_string").StringValue; }
                    catch (NotLoadedException) { }
                }
                System.Console.WriteLine("    " + uid + " " + type + " " + name);
            }
        }

        override public void LocalObjectDelete(string[] uids)
        {
            if (uids.Length == 0) return;
            System.Console.WriteLine("");
            System.Console.WriteLine("Deleted Objects handled in AppXModelEventListener.LocalObjectDelete");
            for (int i = 0; i < uids.Length; i++)
                System.Console.WriteLine("    " + uids[i]);
        }
    }
}
