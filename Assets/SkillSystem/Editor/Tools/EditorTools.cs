#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;

namespace SkillSystem
{
    // Timeline 编辑器窗口回调的辅助类
    public static class TimelineEditorWindowCallback
    {
        public static System.Action<SceneView> OnTimelineSceneGUI;
    }

    // 扩展 Timeline 窗口
    [InitializeOnLoad]
    public static class TimelineWindowExtension
    {
        static TimelineWindowExtension()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            var timeline_window = TimelineEditor.GetWindow();
            if (timeline_window != null)
            {
                // 在 Scene 视图中注册绘制回调
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var timeline_window = TimelineEditor.GetWindow();
            if (timeline_window != null && TimelineEditorWindowCallback.OnTimelineSceneGUI != null)
            {
                TimelineEditorWindowCallback.OnTimelineSceneGUI(sceneView);
            }
        }
    }
}
#endif