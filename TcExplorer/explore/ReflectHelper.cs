using System;
using System.Reflection;
using Teamcenter.Soa.Client;

using Cls0ClassSvc = Cls0.Services.Strong.Classificationcore.ClassificationService;
using ClassicSvc   = Teamcenter.Services.Strong.Classification.ClassificationService;

namespace TcExplorer.Explore
{
    /// <summary>Temporary helper — run with -reflect to dump Classification service method signatures.</summary>
    public static class ReflectHelper
    {
        public static void DumpClassificationServices(Connection connection)
        {
            Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

            // Instantiate both services so their assemblies are loaded into the AppDomain
            object[] services = {
                Cls0ClassSvc.getService(connection),
                ClassicSvc.getService(connection),
            };

            foreach (object svc in services)
            {
                Type t = svc.GetType();
                Console.WriteLine();
                Console.WriteLine("TYPE: " + t.FullName);
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.DeclaringType != t) continue;
                    string pstr = string.Join(", ", Array.ConvertAll(
                        m.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
                    Console.WriteLine($"  {m.Name}({pstr}) -> {m.ReturnType.Name}");
                }
            }
        }
    }
}
