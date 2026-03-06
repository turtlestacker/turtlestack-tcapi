using System;
using System.Collections.Generic;
using System.Text;

using SymbolDetective.Model;

namespace SymbolDetective.Output
{
    public class ConsoleRenderer
    {
        private const int LabelWidth = 30;

        public void Render(SymbolReport report)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine();
            Console.WriteLine(new string('═', 70));
            Console.WriteLine($"  SYMBOL DETECTIVE REPORT");
            Console.WriteLine($"  Symbol: {report.SymbolItemId}  Rev: {report.SymbolRevision}");
            Console.WriteLine(new string('═', 70));
            Console.WriteLine($"  UID  : {report.Uid}");
            Console.WriteLine($"  Name : {report.Name}");
            Console.WriteLine($"  Type : {report.Type}");

            // ── Properties ────────────────────────────────────────────────────
            Section("PROPERTIES", report.Properties.Count);
            foreach (var pv in report.Properties)
                Console.WriteLine($"  {Pad(pv.Name)}  {pv.Value}");

            // ── Relations ─────────────────────────────────────────────────────
            Section("RELATIONS", report.Relations.Count);
            if (report.Relations.Count == 0)
            {
                Console.WriteLine("  (none found)");
            }
            else
            {
                string lastRel = null;
                foreach (var ro in report.Relations)
                {
                    if (ro.Relation != lastRel)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"  ── {ro.Relation} ──");
                        lastRel = ro.Relation;
                    }
                    Console.WriteLine($"    [{ro.Type}]  {ro.Name}  (uid={ro.Uid})");
                    foreach (var pv in ro.Properties)
                        if (!string.IsNullOrEmpty(pv.Value))
                            Console.WriteLine($"      {Pad(pv.Name)}  {pv.Value}");
                }
            }

            // ── Classification ────────────────────────────────────────────────
            Section("CLASSIFICATION", report.Classifications.Count);
            if (report.Classifications.Count == 0)
            {
                Console.WriteLine("  (not classified, or IMAN_classification returned no links)");
            }
            else
            {
                foreach (var ce in report.Classifications)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  ICO uid : {ce.IcoUid}");
                    Console.WriteLine($"  Class   : {ce.ClassId}");
                    if (ce.Attributes.Count > 0)
                    {
                        Console.WriteLine($"  Attributes ({ce.Attributes.Count}):");
                        foreach (var a in ce.Attributes)
                            Console.WriteLine($"    {Pad(a.Name)}  {a.Value}");
                    }
                    else
                    {
                        Console.WriteLine("  (no classification attributes returned)");
                    }
                }
            }

            // ── Parent Item ───────────────────────────────────────────────────
            Section("PARENT ITEM", report.ParentItem != null ? 1 : 0);
            if (report.ParentItem == null)
            {
                Console.WriteLine("  (items_tag not populated — parent item not retrieved)");
            }
            else
            {
                var pi = report.ParentItem;
                Console.WriteLine($"  UID  : {pi.Uid}");
                Console.WriteLine($"  Name : {pi.Name}");
                Console.WriteLine($"  Type : {pi.Type}");
                foreach (var pv in pi.Properties)
                    Console.WriteLine($"  {Pad(pv.Name)}  {pv.Value}");
            }

            Console.WriteLine();
            Console.WriteLine(new string('═', 70));
        }

        private static void Section(string title, int count)
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 70));
            Console.WriteLine($"  {title}  ({count})");
            Console.WriteLine(new string('─', 70));
        }

        private static string Pad(string s)
        {
            if (s == null) s = "";
            return s.Length >= LabelWidth ? s : s + new string(' ', LabelWidth - s.Length);
        }
    }
}
