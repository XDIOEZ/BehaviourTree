using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheKiwiCoder
{
    public partial class BehaviourTreeView : GraphView
    {
        // 记录当前鼠标在 contentViewContainer 中的位置，用于粘贴
        public new class UxmlFactory : UxmlFactory<BehaviourTreeView, GraphView.UxmlTraits> { }
        private Vector2 lastMouseContentPos;
        public Action<NodeView> OnNodeSelected;
        private BehaviourTree tree;
        private BehaviourTreeSettings settings;

        // 剪贴板单例，用于跨树复制粘贴
        private static class Clipboard
        {
            public static List<Node> Nodes = new List<Node>();
            public static Dictionary<string, List<string>> Connections = new Dictionary<string, List<string>>();
            public static BehaviourTree SourceTree;
        }

        public struct ScriptTemplate
        {
            public TextAsset templateFile;
            public string defaultFileName;
            public string subFolder;
        }


        private void CopyToClipboard(List<NodeView> originals)
        {
            Clipboard.Nodes.Clear();
            Clipboard.Connections.Clear();
            Clipboard.SourceTree = tree;

            // 用于映射旧 GUID 到新节点
            var cloneMap = new Dictionary<string, Node>();

            foreach (var view in originals)
            {
                var clone = tree.CloneANode(view.node);

                // 关键：清除所有子节点引用
                tree.RemoveAllChildren(clone);

                cloneMap[view.node.guid] = clone;
                Clipboard.Nodes.Add(clone);
            }


            // 记录连接关系（新 GUID）
            foreach (var view in originals)
            {
                var originalGuid = view.node.guid;
                if (!cloneMap.ContainsKey(originalGuid)) continue;

                var clone = cloneMap[originalGuid];
                var originalChildren = BehaviourTree.GetChildren(view.node);
                var newChildrenGuids = originalChildren
                    .Where(c => originals.Any(o => o.node == c))
                    .Select(c => cloneMap[c.guid].guid)
                    .ToList();

                Clipboard.Connections[clone.guid] = newChildrenGuids;
            }

            Debug.Log($"Copied {Clipboard.Nodes.Count} cloned nodes to clipboard from tree '{tree.name}'");
        }

        private void PasteFromClipboard(Vector2 pastePosition)
        {
            if (Clipboard.Nodes == null || Clipboard.Nodes.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(tree, "Paste Nodes");

            var map = new Dictionary<string, Node>();
            float minX = Clipboard.Nodes.Min(n => n.position.x);
            float minY = Clipboard.Nodes.Min(n => n.position.y);
            float offsetX = pastePosition.x - minX;
            float offsetY = pastePosition.y - minY;

            // 实例化并移除子引用
            foreach (var orig in Clipboard.Nodes)
            {
                var clone = tree.CloneNode(orig);
                clone.position = new Vector2(orig.position.x + offsetX, orig.position.y + offsetY);

                // 清除原始子节点结构，避免连接混乱
                tree.RemoveAllChildren(clone);

                map[orig.guid] = clone;
                CreateNodeView(clone);
            }

            // 重建连接
            foreach (var kv in Clipboard.Connections)
            {
                if (!map.ContainsKey(kv.Key))
                    continue;

                var parentClone = map[kv.Key];
                foreach (var childGuid in kv.Value)
                {
                    if (!map.ContainsKey(childGuid))
                        continue;

                    var childClone = map[childGuid];

                    tree.AddChild(parentClone, childClone);

                    // 重建连线
                    var pv = FindNodeView(parentClone);
                    var cv = FindNodeView(childClone);
                    if (pv != null && cv != null)
                    {
                        var edge = pv.output.ConnectTo(cv.input);
                        AddElement(edge);
                    }
                }
            }

            // 选中粘贴的新节点
            ClearSelection();
            foreach (var clone in map.Values)
            {
                var nv = FindNodeView(clone);
                if (nv != null)
                    AddToSelection(nv);
            }

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();

            Debug.Log($"Pasted {map.Count} nodes into tree '{tree.name}'");
        }



        public ScriptTemplate[] scriptFileAssets = {
            new ScriptTemplate {
                templateFile = BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateActionNode,
                defaultFileName = "NewActionNode.cs",
                subFolder = "Actions"
            },
            new ScriptTemplate {
                templateFile = BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateCompositeNode,
                defaultFileName = "NewCompositeNode.cs",
                subFolder = "Composites"
            },
            new ScriptTemplate {
                templateFile = BehaviourTreeSettings.GetOrCreateSettings().scriptTemplateDecoratorNode,
                defaultFileName = "NewDecoratorNode.cs",
                subFolder = "Decorators"
            },
        };

        public BehaviourTreeView()
        {
            settings = BehaviourTreeSettings.GetOrCreateSettings();
            Insert(0, new GridBackground());
            AddManipulators();
            styleSheets.Add(settings.behaviourTreeStyle);
            Undo.undoRedoPerformed += OnUndoRedo;

            // 支持键盘快捷键
            focusable = true;
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            RegisterCallback<PointerMoveEvent>(e =>
            {
                lastMouseContentPos = this.ChangeCoordinatesTo(contentViewContainer, e.localPosition);
            });
            void AddManipulators()
            {
                this.AddManipulator(new ContentZoomer());
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new DoubleClickSelection());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());
            }
        }


        private void OnKeyDown(KeyDownEvent e)
        {
            bool ctrl = Application.platform == RuntimePlatform.OSXEditor ? e.commandKey : e.ctrlKey;
            if (!ctrl) return;

            // Ctrl+C 复制
            if (e.keyCode == KeyCode.C)
            {
                var selected = selection.OfType<NodeView>().ToList();
                if (selected.Any())
                {
                    CopyToClipboard(selected);
                    e.StopPropagation();
                }
            }
            // Ctrl+V 粘贴
            else if (e.keyCode == KeyCode.V)
            {
                if (Clipboard.Nodes.Any())
                {
                    PasteFromClipboard(lastMouseContentPos);
                    e.StopPropagation();
                }
            }
            // Ctrl+X 剪切（复制 + 删除）
            else if (e.keyCode == KeyCode.X)
            {
                var selected = selection.OfType<NodeView>().ToList();
                if (selected.Any())
                {
                    CopyToClipboard(selected);
                    e.StopPropagation();
                }
            }
            // Ctrl+A 全选
            else if (e.keyCode == KeyCode.A)
            {
                ClearSelection();
                foreach (var node in nodes.OfType<NodeView>())
                {
                    AddToSelection(node);
                }
                e.StopPropagation();
            }
        }


        private void OnUndoRedo()
        {
            PopulateView(tree);
            AssetDatabase.SaveAssets();
        }

        public NodeView FindNodeView(Node node)
        {
            return GetNodeByGuid(node.guid) as NodeView;
        }

        internal void PopulateView(BehaviourTree tree)
        {
            this.tree = tree;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;

            if (tree.rootNode == null)
            {
                tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }

            // 创建节点视图
            tree.nodes.ForEach(n => CreateNodeView(n));

            // Create edges（建议加个 null 检查）
            tree.nodes.ForEach(n => {
                var children = BehaviourTree.GetChildren(n);
                children.ForEach(c => {
                    if (n == null || c == null) return;

                    NodeView parentView = FindNodeView(n);
                    NodeView childView = FindNodeView(c);

                    if (parentView == null || childView == null) return;

                    Edge edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge);
                });
            });

        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                graphViewChange.elementsToRemove.ForEach(elem => {
                    NodeView nodeView = elem as NodeView;
                    if (nodeView != null)
                    {
                        tree.DeleteNode(nodeView.node);
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        tree.RemoveChild(parentView.node, childView.node);
                    }
                });
            }

            if (graphViewChange.edgesToCreate != null)
            {
                graphViewChange.edgesToCreate.ForEach(edge => {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    tree.AddChild(parentView.node, childView.node);
                });
            }

            nodes.ForEach((n) => {
                NodeView view = n as NodeView;
                view.SortChildren();
            });

            return graphViewChange;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePos = evt.localMousePosition;
            Vector2 contentPos = this.ChangeCoordinatesTo(contentViewContainer, mousePos);

            /*   // 单节点克隆
               var clicked = nodes.OfType<NodeView>().FirstOrDefault(nv => nv.GetPosition().Contains(contentPos));
               if (clicked != null)
               {
                   evt.menu.AppendAction("Clone Node", _ => CloneNode(clicked));
                   evt.menu.AppendSeparator();
               }*/

            // 复制/粘贴菜单
            var selected = selection.OfType<NodeView>().ToList();
            if (selected.Any())
                evt.menu.AppendAction("复制选中的节点", _ => CopyToClipboard(selected));
            // 允许跨树和同树粘贴
            if (Clipboard.Nodes.Any())
                evt.menu.AppendAction(Clipboard.SourceTree == tree ? "Paste Nodes" : "Paste Nodes from Other Tree", _ => PasteFromClipboard(contentPos));
            evt.menu.AppendAction("Paste Nodes from Other Tree", _ => PasteFromClipboard(contentPos));
            if (selected.Count > 1)
            {
                evt.menu.AppendAction("Clone Selected Nodes", _ => CloneSelectedNodes(selected));
                evt.menu.AppendSeparator();
            }


            // 原有创建脚本和节点菜单
            evt.menu.AppendAction("Create Script.../New Action Node", _ => CreateNewScript(scriptFileAssets[0]));
            evt.menu.AppendAction("Create Script.../New Composite Node", _ => CreateNewScript(scriptFileAssets[1]));
            evt.menu.AppendAction("Create Script.../New Decorator Node", _ => CreateNewScript(scriptFileAssets[2]));
            evt.menu.AppendSeparator();

            foreach (var type in TypeCache.GetTypesDerivedFrom<ActionNode>())
                evt.menu.AppendAction($"[Action]/{type.Name}", _ => CreateNode(type, contentPos));
            foreach (var type in TypeCache.GetTypesDerivedFrom<CompositeNode>())
                evt.menu.AppendAction($"[Composite]/{type.Name}", _ => CreateNode(type, contentPos));
            foreach (var type in TypeCache.GetTypesDerivedFrom<DecoratorNode>())
                evt.menu.AppendAction($"[Decorator]/{type.Name}", _ => CreateNode(type, contentPos));
        }
        private void CloneSelectedNodes(List<NodeView> originals)
        {
            Undo.RegisterCompleteObjectUndo(tree, "Clone Selected Nodes");

            Dictionary<Node, Node> map = new Dictionary<Node, Node>();

            foreach (var view in originals)
            {
                var clone = tree.CloneNode(view.node);
                clone.position = view.node.position + new Vector2(30, 30);
                CreateNodeView(clone);
                map[view.node] = clone;
            }

            foreach (var view in originals)
            {
                var parent = view.node;
                if (!map.ContainsKey(parent)) continue;

                var cloneParent = map[parent];
                var children = BehaviourTree.GetChildren(parent);

                foreach (var child in children)
                {
                    if (map.ContainsKey(child))
                    {
                        var cloneChild = map[child];
                        tree.AddChild(cloneParent, cloneChild);

                        var parentView = FindNodeView(cloneParent);
                        var childView = FindNodeView(cloneChild);
                        var edge = parentView.output.ConnectTo(childView.input);
                        AddElement(edge);
                    }
                }
            }

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }


        /// <summary>
        /// 克隆指定的节点（不含子连接）
        /// </summary>
        private void CloneNode(NodeView original)
        {
            Undo.RegisterCompleteObjectUndo(tree, "Clone Node");

            var clone = tree.CloneNode(original.node);
            clone.position = original.node.position + new Vector2(30, 30);
            var cloneView = CreateNodeView(clone);

            ClearSelection();
            AddToSelection(cloneView);

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }


        void SelectFolder(string path)
        {
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        void CreateNewScript(ScriptTemplate template)
        {
            SelectFolder($"{settings.newNodeBasePath}/{template.subFolder}");
            var templatePath = AssetDatabase.GetAssetPath(template.templateFile);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, template.defaultFileName);
        }

        void CreateNode(System.Type type, Vector2 position)
        {
            Node node = tree.CreateNode(type);
            node.position = position;
            CreateNodeView(node);
        }


        // 修改 CreateNodeView 返回 NodeView 以便引用
        private NodeView CreateNodeView(Node node)
        {
            var nodeView = new NodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
            return nodeView;
        }


        public void UpdateNodeStates()
        {
            nodes.ForEach(n => {
                NodeView view = n as NodeView;
                view.UpdateState();
            });
        }
    }
}
