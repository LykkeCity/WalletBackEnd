using System;
using System.Collections.Generic;

namespace Common.RemoteUi
{
    public class RemoteUiTreeNode
    {
        /// <summary>
        /// Id ноды
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Имя ноды
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Иконка
        /// </summary>
        public string Icon { get; set; }

        // Значение ноды
        public string Value { get; set; }
        // Если нода является дочнерней какой то ноде - то указываем ее MasterId
        public string MasterId { get; set; }
        // Если у ноды есть табличное представление - то указываем ее MasterId
        public string TableId { get; set; }


        public static RemoteUiTreeNode Create(string name, string icon, string masterId = null, string tableId = null)
        {
            return new RemoteUiTreeNode
            {
                Name = name,
                Icon = icon,
                MasterId = masterId,
                TableId = tableId
            };
        }

    }


    public static class RemoteUiStorage
    {

        public class RemoteUiTreeNodeItem
        {
            public RemoteUiTreeNode TreeNode { get; set; }

            public Func<string> GetValue { get; set; }

            public static RemoteUiTreeNodeItem Create(RemoteUiTreeNode itm, Func<string> getValue)
            {
                return new RemoteUiTreeNodeItem
                {
                    GetValue = getValue,
                    TreeNode = itm
                };
            }
        }


        private static readonly Dictionary<string, RemoteUiTreeNodeItem> TreeNodes = new Dictionary<string, RemoteUiTreeNodeItem>();

        private static readonly Dictionary<string, IGuiTable> Tables = new Dictionary<string, IGuiTable>(); 

        private const string NodePrefix = "tn";
        private static int _nodeIndex;

        private const string TablePrefix = "tb";
        private static int _tableIndex;

        public static string RegisterTreeNode(RemoteUiTreeNode itm, Func<string> getValue = null,
            IGuiTable tableData = null)
        {


            if (tableData != null)
                lock (Tables)
                {
                    itm.TableId = itm.TableId ?? TablePrefix + (_tableIndex++);
                    Tables.Add(itm.TableId, tableData);
                }

            lock (TreeNodes)
            {
                itm.Id = NodePrefix + (_nodeIndex++);
                TreeNodes.Add(itm.Id, RemoteUiTreeNodeItem.Create(itm, getValue));
                return itm.Id;
            }

        }


        public static void RegisterTable(string id, IGuiTable guiTable)
        {
            lock (Tables)
            {
                Tables.Add(id, guiTable); 
            }
        }

        public static IEnumerable<RemoteUiTreeNode> GetTreeNodes()
        {
            foreach (var item in TreeNodes.Values)
            {
                if (item.GetValue != null)
                  item.TreeNode.Value = item.GetValue();
                yield return item.TreeNode;
            }
        }

        public static IEnumerable<string[]> GetTableData(string id)
        {
            if (!Tables.ContainsKey(id)) yield break;

            var table = Tables[id];

            yield return table.TableData.Headers;
            foreach (var item in table.TableData.Data)
                yield return item;
        }


        public static void WriteTableLine(string id, string[] data)
        {
           if (!Tables.ContainsKey(id))
               return;

           var table = Tables[id] as IGuiTableDataSrc;

            if (table != null)
                table.NewData(data);
        }
    }

}
