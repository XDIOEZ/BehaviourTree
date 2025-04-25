using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace TheKiwiCoder
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<NodeView> OnNodeSelected;
        public Node node;
        public Port input;
        public Port output;

        private bool isCollapsed;
        private bool skipChildMovement = false;
        private Button collapseButton;

        public NodeView(Node node)
            : base(AssetDatabase.GetAssetPath(BehaviourTreeSettings.GetOrCreateSettings().nodeXml))
        {
            this.node = node;
            this.node.name = node.GetType().Name;
            this.title = node.name.Replace("(Clone)", "").Replace("Node", "");
            this.viewDataKey = node.guid;

            isCollapsed = node.isCollapsed;
            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutputPorts();
            SetupClasses();
            SetupDataBinding();

            AddCollapseButton();
            if (isCollapsed)
                ApplyCollapse(true);
        }
        #region 折叠相关函数

        public override void SetPosition(Rect newPos)
        {
            Rect oldPos = GetPosition();
            base.SetPosition(newPos);

            Undo.RecordObject(node, "Behaviour Tree (Set Position)");
            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;
            EditorUtility.SetDirty(node);

            if (skipChildMovement) return;

            if (isCollapsed && output != null)
            {
                Vector2 delta = newPos.position - oldPos.position;
                foreach (var edge in output.connections.ToList())
                {
                    if (edge.input.node is NodeView child)
                    {
                        child.skipChildMovement = true;
                        Rect cr = child.GetPosition();
                        child.SetPosition(new Rect(cr.x + delta.x, cr.y + delta.y, cr.width, cr.height));
                        // 递归移动所有后代
                        MoveSubtree(child, delta);
                        child.skipChildMovement = false;
                    }
                }
            }
        }

        // 递归移动子树所有节点
        private void MoveSubtree(NodeView parent, Vector2 delta)
        {
            if (parent.output == null) return;
            foreach (var edge in parent.output.connections.ToList())
            {
                if (edge.input.node is NodeView child)
                {
                    child.skipChildMovement = true;
                    Rect cr = child.GetPosition();
                    child.SetPosition(new Rect(cr.x + delta.x, cr.y + delta.y, cr.width, cr.height));
                    MoveSubtree(child, delta);
                    child.skipChildMovement = false;
                }
            }
        }

        private void AddCollapseButton()
        {
            collapseButton = new Button(() =>
            {
                isCollapsed = !isCollapsed;
                node.isCollapsed = isCollapsed;
                collapseButton.text = isCollapsed ? "▲" : "▼";
                ApplyCollapse(isCollapsed);
            })
            {
                text = isCollapsed ? "▲" : "▼",
                tooltip = "折叠/展开 子节点"
            };
            collapseButton.style.width = 32;
            collapseButton.style.height = 16;
            collapseButton.style.minWidth = 16;
            collapseButton.style.minHeight = 16;
            titleContainer.Add(collapseButton);
            UpdateCollapseButtonVisibility();
        }

        private void UpdateCollapseButtonVisibility()
        {
            bool canHaveChildren = output != null;
            collapseButton.style.display = canHaveChildren ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyCollapse(bool collapse)
        {
            if (output == null) return;
            foreach (var edge in output.connections.ToList())
            {
                if (edge.input.node is NodeView child)
                {
                    child.style.visibility = collapse ? Visibility.Hidden : Visibility.Visible;
                    edge.style.display = collapse ? DisplayStyle.None : DisplayStyle.Flex;
                    ApplyCollapseToDescendants(child, collapse);
                }
            }
            UpdateCollapseButtonVisibility();
        }

        private void ApplyCollapseToDescendants(NodeView parent, bool collapse)
        {
            if (parent.output == null) return;
            foreach (var edge in parent.output.connections.ToList())
            {
                if (edge.input.node is NodeView child)
                {
                    child.style.visibility = collapse ? Visibility.Hidden : Visibility.Visible;
                    edge.style.display = collapse ? DisplayStyle.None : DisplayStyle.Flex;
                    ApplyCollapseToDescendants(child, collapse);
                }
            }
        }
        #endregion

        #region 其他方法
        private void SetupDataBinding()
        {
            Label descriptionLabel = this.Q<Label>("description");
            descriptionLabel.bindingPath = "description";
            descriptionLabel.Bind(new SerializedObject(node));
        }

        private void SetupClasses()
        {
            if (node is ActionNode) AddToClassList("action");
            else if (node is CompositeNode) AddToClassList("composite");
            else if (node is DecoratorNode) AddToClassList("decorator");
            else if (node is RootNode) AddToClassList("root");
        }

        private void CreateInputPorts()
        {
            if (node is ActionNode || node is CompositeNode || node is DecoratorNode)
            {
                input = new NodePort(Direction.Input, Port.Capacity.Single)
                {
                    portName = string.Empty,
                    style = { flexDirection = FlexDirection.Column }
                };
                inputContainer.Add(input);
            }
        }

        private void CreateOutputPorts()
        {
            if (node is CompositeNode)
                output = new NodePort(Direction.Output, Port.Capacity.Multi);
            else if (node is DecoratorNode || node is RootNode)
                output = new NodePort(Direction.Output, Port.Capacity.Single);
            if (output != null)
            {
                output.portName = string.Empty;
                output.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(output);
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }

        public void SortChildren()
        {
            if (node is CompositeNode composite)
                composite.children.Sort((l, r) => l.position.x.CompareTo(r.position.x));
        }

        public void UpdateState()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");
            if (Application.isPlaying)
            {
                switch (node.state)
                {
                    case Node.State.Running:
                        if (node.started) AddToClassList("running");
                        break;
                    case Node.State.Failure:
                        AddToClassList("failure");
                        break;
                    case Node.State.Success:
                        AddToClassList("success");
                        break;
                }
            }
        }
        #endregion
    }
}
