using UnityEditor;
using UnityEngine;

namespace SkillSystem
{
    /// <summary>
    /// Hierarchy 窗口右键菜单扩展 - 复制 Transform 路径
    /// </summary>
    public class TransformPathMenuExtension
    {
        private const string                                    MENU_COPY_FULL_PATH = "GameObject/Copy Path/Full Path";
        private const string                                    MENU_COPY_NAME_ONLY = "GameObject/Copy Path/Name Only";


        [MenuItem(MENU_COPY_FULL_PATH, false)]
        private static void CopyFullPath()
        {
            if (Selection.activeTransform == null) return;

            string path = GetFullPath(Selection.activeTransform);
            GUIUtility.systemCopyBuffer = path;
            Debug.Log($"已复制完整路径: {path}");
        }

        [MenuItem(MENU_COPY_FULL_PATH, true)]
        private static bool ValidateCopyFullPath()
        {
            return Selection.activeTransform != null;
        }

        [MenuItem(MENU_COPY_NAME_ONLY, false)]
        private static void CopyNameOnly()
        {
            if (Selection.activeTransform == null) return;

            string name = Selection.activeTransform.name;
            GUIUtility.systemCopyBuffer = name;
            Debug.Log($"已复制名称: {name}");
        }

        [MenuItem(MENU_COPY_NAME_ONLY, true)]
        private static bool ValidateCopyNameOnly()
        {
            return Selection.activeTransform != null;
        }

        private static string GetFullPath(Transform target)
        {
            if (target == null) return "";

            string path = target.name;
            Transform parent = target.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}