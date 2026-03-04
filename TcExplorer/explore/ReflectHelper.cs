using System;
using System.Reflection;

namespace TcExplorer.Explore
{
    /// <summary>Temporary helper — run with -reflect to dump Classification service method signatures.</summary>
    public static class ReflectHelper
    {
        public static void DumpClassificationServices()
        {
            Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

            string[] typeNames = {
                "Teamcenter.Services.Strong.Classification.ClassificationService",
                "Cls0.Services.Strong.Classificationcore.ClassificationService",
            };

            foreach (string typeName in typeNames)
            {
                Type t = Type.GetType(typeName);
                if (t == null)
                {
                    // search loaded assemblies
                    foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = a.GetType(typeName);
                        if (t != null) break;
                    }
                }

                Console.WriteLine();
                if (t == null) { Console.WriteLine("TYPE NOT FOUND: " + typeName); continue; }
                Console.WriteLine("TYPE: " + t.FullName);

                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.DeclaringType != t) continue;
                    var ps = m.GetParameters();
                    string pstr = string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name + " " + p.Name));
                    Console.WriteLine($"  {m.Name}({pstr}) -> {m.ReturnType.Name}");
                }
            }
        }
    }
}
