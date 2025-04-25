using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace TheKiwiCoder.EditorTools
{
    public class BTNodeCleaner : EditorWindow
    {

        [MenuItem("TheKiwiCoder/清理无效节点")]
        public static void CleanBrokenNodes()
        {
            string[] guids = AssetDatabase.FindAssets("t:BehaviourTree");

            int totalFixed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BehaviourTree tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);

                if (tree == null) continue;

                int before = tree.nodes.Count;
                tree.nodes.RemoveAll(n => n == null);
                int after = tree.nodes.Count;

                if (before != after)
                {
                    EditorUtility.SetDirty(tree);
                    Debug.Log($"[BTNodeCleaner] 清理文件 {path}：删除了 {before - after} 个无效节点引用。");
                    totalFixed += before - after;
                }
            }

            AssetDatabase.SaveAssets();

            if (totalFixed > 0)
            {
                EditorUtility.DisplayDialog("清理完成", $"共清理 {totalFixed} 个丢失引用的节点。", "好的");
            }
            else
            {
                EditorUtility.DisplayDialog("无清理必要", "所有行为树节点均为有效引用。", "明白");
            }
        }
    }
}
