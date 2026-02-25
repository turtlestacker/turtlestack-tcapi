using System;
using System.Collections.Generic;
using System.Text;

using TcExplorer.Model;

namespace TcExplorer.Output
{
    public class ConsoleRenderer
    {
        public void Render(ExplorerResult result)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine();
            Console.WriteLine("=== Folder Tree ===");
            if (result.FolderTree != null)
                RenderFolderNode(result.FolderTree, "", true);
            else
                Console.WriteLine("  (no folder tree)");

            Console.WriteLine();
            Console.WriteLine("=== Classification Hierarchy ===");
            if (result.ClassificationTree == null || result.ClassificationTree.Count == 0)
            {
                Console.WriteLine("  (classification tree not available — see ClassificationExplorer.cs VERIFY_API blocks)");
            }
            else
            {
                for (int i = 0; i < result.ClassificationTree.Count; i++)
                {
                    bool isLast = (i == result.ClassificationTree.Count - 1);
                    RenderClassNode(result.ClassificationTree[i], "", isLast);
                }
            }
        }

        private void RenderFolderNode(FolderNode node, string prefix, bool isLast)
        {
            string connector = isLast ? "\u2514\u2500\u2500 " : "\u251C\u2500\u2500 ";
            string childPrefix = isLast ? "    " : "\u2502   ";

            Console.WriteLine(prefix + connector + "[" + node.Type + "] " + node.Name);

            string itemPrefix = prefix + childPrefix;
            bool hasChildren = node.Children != null && node.Children.Count > 0;

            // Items first
            if (node.Items != null)
            {
                for (int i = 0; i < node.Items.Count; i++)
                {
                    bool itemIsLast = (i == node.Items.Count - 1) && !hasChildren;
                    string itemConnector = itemIsLast ? "\u2514\u2500\u2500 " : "\u251C\u2500\u2500 ";
                    ItemInfo item = node.Items[i];
                    Console.WriteLine(itemPrefix + itemConnector + "[" + item.Type + "] " + item.Name);
                }
            }

            // Then sub-folders
            if (hasChildren)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    bool childIsLast = (i == node.Children.Count - 1);
                    RenderFolderNode(node.Children[i], itemPrefix, childIsLast);
                }
            }
        }

        private void RenderClassNode(ClassNode node, string prefix, bool isLast)
        {
            string connector = isLast ? "\u2514\u2500\u2500 " : "\u251C\u2500\u2500 ";
            string childPrefix = isLast ? "    " : "\u2502   ";

            Console.WriteLine(prefix + connector + "[Class] " + node.Name + " (" + node.Id + ")");

            string innerPrefix = prefix + childPrefix;
            bool hasChildren = node.Children != null && node.Children.Count > 0;

            // Attributes as leaves
            if (node.Attributes != null)
            {
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    bool attrIsLast = (i == node.Attributes.Count - 1) && !hasChildren;
                    string attrConnector = attrIsLast ? "\u2514\u2500\u2500 " : "\u251C\u2500\u2500 ";
                    ClassAttribute attr = node.Attributes[i];
                    string unitPart = string.IsNullOrEmpty(attr.Unit) ? "" : " [" + attr.Unit + "]";
                    Console.WriteLine(innerPrefix + attrConnector + "[Attr] " + attr.Name + " (" + attr.DataType + ")" + unitPart);
                }
            }

            // Then child classes
            if (hasChildren)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    bool childIsLast = (i == node.Children.Count - 1);
                    RenderClassNode(node.Children[i], innerPrefix, childIsLast);
                }
            }
        }
    }
}
