using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

using TcExplorer.Model;

namespace TcExplorer.Output
{
    public class JsonExporter
    {
        public void Export(ExplorerResult result, string filePath)
        {
            try
            {
                var serializer = new JavaScriptSerializer
                {
                    MaxJsonLength  = int.MaxValue,
                    RecursionLimit = 500
                };

                string raw    = serializer.Serialize(result);
                string pretty = PrettyPrint(raw);

                File.WriteAllText(filePath, pretty, Encoding.UTF8);
                Console.WriteLine();
                Console.WriteLine("JSON output written to: " + filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ERROR] Failed to write JSON output: " + e.Message);
            }
        }

        // Simple state-machine pretty-printer.
        // Handles { [ } ] , : with indent tracking and an inString flag.
        // Note: does not handle escaped quotes inside string values (e.g. \")
        // but TC object names and UIDs will not trigger this edge case in practice.
        private static string PrettyPrint(string json)
        {
            var sb      = new StringBuilder(json.Length * 2);
            int indent  = 0;
            bool inStr  = false;
            const string indentStr = "  ";

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inStr = !inStr;

                if (inStr)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        // Peek: if next non-whitespace is the closing bracket, keep on one line
                        int peek = i + 1;
                        while (peek < json.Length && json[peek] == ' ') peek++;
                        if (peek < json.Length && (json[peek] == '}' || json[peek] == ']'))
                            break;
                        indent++;
                        sb.AppendLine();
                        sb.Append(Indent(indent, indentStr));
                        break;

                    case '}':
                    case ']':
                        indent--;
                        sb.AppendLine();
                        sb.Append(Indent(indent, indentStr));
                        sb.Append(c);
                        break;

                    case ',':
                        sb.Append(c);
                        sb.AppendLine();
                        sb.Append(Indent(indent, indentStr));
                        break;

                    case ':':
                        sb.Append(": ");
                        break;

                    default:
                        if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        private static string Indent(int level, string unit)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < level; i++) sb.Append(unit);
            return sb.ToString();
        }
    }
}
