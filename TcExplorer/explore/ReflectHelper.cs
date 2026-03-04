using System;
using System.Collections;
using System.Reflection;
using Teamcenter.Soa.Client;

using Cls0ClassSvc = Cls0.Services.Strong.Classificationcore.ClassificationService;
using ClassicSvc   = Teamcenter.Services.Strong.Classification.ClassificationService;

namespace TcExplorer.Explore
{
    /// <summary>Temporary helper — run with -reflect to dump Classification service method signatures and key types.</summary>
    public static class ReflectHelper
    {
        public static void DumpClassificationServices(Connection connection)
        {
            Teamcenter.Soa.Client.Model.StrongObjectFactoryClassification.Init();

            object[] services = {
                Cls0ClassSvc.getService(connection),
                ClassicSvc.getService(connection),
            };

            foreach (object svc in services)
            {
                Type t = svc.GetType();
                Console.WriteLine();
                Console.WriteLine("=== SERVICE: " + t.FullName + " ===");
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.DeclaringType != t) continue;
                    string pstr = string.Join(", ", Array.ConvertAll(
                        m.GetParameters(), p => p.ParameterType.Name + " " + p.Name));
                    Console.WriteLine($"  {m.Name}({pstr}) -> {m.ReturnType.Name}");
                }
            }

            // Dump key input/output types from TcSoaClassificationStrong
            Console.WriteLine();
            Console.WriteLine("=== KEY TYPES ===");
            string[] typePatterns = {
                "SearchClassAttributes", "SearchResponse",
                "ClassificationInfoResponse", "ClassificationInfo",
                "IcoInfo", "ClassificationObject",
            };

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.FullName.Contains("Classification")) continue;
                foreach (Type t in asm.GetTypes())
                {
                    bool match = false;
                    foreach (string p in typePatterns)
                        if (t.Name.Equals(p, StringComparison.OrdinalIgnoreCase)) { match = true; break; }
                    if (!match) continue;

                    Console.WriteLine();
                    Console.WriteLine("  TYPE: " + t.FullName);
                    foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        Console.WriteLine($"    PROP  {p.PropertyType.Name} {p.Name}");
                    foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        Console.WriteLine($"    FIELD {f.FieldType.Name} {f.Name}");
                }
            }
        }
    }
}
