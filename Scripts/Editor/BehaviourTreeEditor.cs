using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Callbacks;

namespace TheKiwiCoder {

    public class BehaviourTreeEditor : EditorWindow {

        BehaviourTreeView treeView;
        BehaviourTree tree;
        InspectorView inspectorView;
        IMGUIContainer blackboardView;
        ToolbarMenu toolbarMenu;
        TextField treeNameField;
        TextField locationPathField;
        Button createNewTreeButton;
        VisualElement overlay;
        BehaviourTreeSettings settings;

        SerializedObject treeObject;
        SerializedProperty blackboardProperty;

        [MenuItem("TheKiwiCoder/BehaviourTreeEditor ...")]
        public static void OpenWindow() {
            BehaviourTreeEditor wnd = GetWindow<BehaviourTreeEditor>();
            wnd.titleContent = new GUIContent("BehaviourTreeEditor");
            wnd.minSize = new Vector2(800, 600);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line) {
            if (Selection.activeObject is BehaviourTree) {
                OpenWindow();
                return true;
            }
            return false;
        }

        List<T> LoadAssets<T>() where T : UnityEngine.Object {
            string[] assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            List<T> assets = new List<T>();
            foreach (var assetId in assetIds) {
                string path = AssetDatabase.GUIDToAssetPath(assetId);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                assets.Add(asset);
            }
            return assets;
        }

        public void CreateGUI() {

            settings = BehaviourTreeSettings.GetOrCreateSettings();

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = settings.behaviourTreeXml;
            visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = settings.behaviourTreeStyle;
            root.styleSheets.Add(styleSheet);

            // Main treeview
            treeView = root.Q<BehaviourTreeView>();
            treeView.OnNodeSelected = OnNodeSelectionChanged;

            // Inspector View
            inspectorView = root.Q<InspectorView>();

            // Blackboard view
            blackboardView = root.Q<IMGUIContainer>();
            blackboardView.onGUIHandler = () => {
                if (treeObject != null && treeObject.targetObject != null) {
                    treeObject.Update();
                    EditorGUILayout.PropertyField(blackboardProperty);
                    treeObject.ApplyModifiedProperties();
                }
            };

            // Toolbar assets menu
            toolbarMenu = root.Q<ToolbarMenu>();
            var behaviourTrees = LoadAssets<BehaviourTree>();
            behaviourTrees.ForEach(tree => {
                toolbarMenu.menu.AppendAction($"{tree.name}", (a) => {
                    Selection.activeObject = tree;
                });
            });
            toolbarMenu.menu.AppendSeparator();
            toolbarMenu.menu.AppendAction("新建行为树", (a) => CreateNewTree("NewBehaviourTree"));

            // New Tree Dialog
            treeNameField = root.Q<TextField>("TreeName");
            locationPathField = root.Q<TextField>("LocationPath");
            overlay = root.Q<VisualElement>("Overlay");
            createNewTreeButton = root.Q<Button>("CreateButton");
            createNewTreeButton.clicked += () => CreateNewTree(treeNameField.value);

            if (tree == null) {
                OnSelectionChange();
            } else {
                SelectTree(tree);
            }
        }

        private void OnEnable() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable() {

            if (tree != null)
            {
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj) {
            switch (obj) {
                case PlayModeStateChange.EnteredEditMode:
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void OnSelectionChange() {
            EditorApplication.delayCall += () => {
                BehaviourTree tree = Selection.activeObject as BehaviourTree;
                if (!tree) {
                    if (Selection.activeGameObject) {
                        BehaviourTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();
                        if (runner) {
                            tree = runner.tree;
                        }
                    }
                }

                SelectTree(tree);
            };
        }

        void SelectTree(BehaviourTree newTree) {

            if (treeView == null) {
                return;
            }

            if (!newTree) {
                return;
            }

            this.tree = newTree;

            overlay.style.visibility = Visibility.Hidden;

            if (Application.isPlaying) {
                treeView.PopulateView(tree);
            } else {
                treeView.PopulateView(tree);
            }

            
            treeObject = new SerializedObject(tree);
            blackboardProperty = treeObject.FindProperty("blackboard");

            EditorApplication.delayCall += () => {
                treeView.FrameAll();
            };
        }

        void OnNodeSelectionChanged(NodeView node) {
            inspectorView.UpdateSelection(node);
        }

        private void OnInspectorUpdate() {
            treeView?.UpdateNodeStates();
        }

        void CreateNewTree(string assetName)
        {
            string folderPath = locationPathField.value.Trim().TrimEnd('/', '\\');

            // 默认路径设为 "Assets/BehaviourTrees" 如果为空
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = "Assets/_BehaviourTrees";
            }

            // 如果文件夹不存在，则创建它
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string current = "Assets";
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }

            string path = System.IO.Path.Combine(folderPath, $"{assetName}.asset");

            if (AssetDatabase.LoadAssetAtPath<BehaviourTree>(path) != null)
            {
                Debug.LogWarning($"Asset already exists at: {path}");
                return;
            }
            Debug.Log($"Creating new BehaviourTree at: {path}");
            BehaviourTree tree = ScriptableObject.CreateInstance<BehaviourTree>();
            tree.name = treeNameField.value;

            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = tree;
            EditorGUIUtility.PingObject(tree);
        }

    }
}