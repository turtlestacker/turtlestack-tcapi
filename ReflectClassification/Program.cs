using System;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        string root = @"C:\TurtleStack\RR-SMR-TC-API\TcExplorer\bin\Debug";
        string[] dlls = {
            @"TcSoaClassificationStrong.dll",
            @"Cls0SoaClassificationCoreStrong.dll",
        };

        foreach (string rel in dlls)
        {
            string path = Path.Combine(root, rel);
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => {
                string name = new AssemblyName(a.Name).Name + ".dll";
                string candidate = Path.Combine(root, name);
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };
            Console.WriteLine();
            Console.WriteLine("=== " + Path.GetFileName(path) + " ===");
            Assembly asm;
            try { asm = Assembly.LoadFrom(path); }
            catch (Exception e) { Console.WriteLine("LOAD FAILED: " + e.Message); continue; }

            foreach (Type t in asm.GetTypes())
            {
                if (!t.Name.Contains("ClassificationService")) continue;
                Console.WriteLine("  TYPE: " + t.FullName);
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.DeclaringType != t) continue;
                    var ps = m.GetParameters();
                    string pstr = string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name + " " + p.Name));
                    Console.WriteLine($"    {m.Name}({pstr}) -> {m.ReturnType.Name}");
                }
            }
        }
    }
}
