using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [CustomEditor(typeof(CurveTrack))]
    public class CurveSceneGUI : UnityEditor.Editor
    {
        private CurveTrack                      current_track_;
        private bool                            is_listening_ = false;

        private void OnEnable()
        {
            current_track_ = target as CurveTrack;

            // 注册Scene视图的绘制回调
            if (!is_listening_)
            {
                SceneView.duringSceneGui += SceneGUI;
                is_listening_ = true;

                // 强制刷新Scene视图
                SceneView.RepaintAll();
            }
        }

        private void OnDisable()
        {
            // 移除Scene视图回调
            if (is_listening_)
            {
                SceneView.duringSceneGui -= SceneGUI;
                is_listening_ = false;
                SceneView.RepaintAll();
            }
        }

        private void SceneGUI(SceneView sceneView)
        {
            if (current_track_ == null)
            {
                current_track_ = target as CurveTrack;
                if (current_track_ == null) return;
            }

            DrawTrackCurves(sceneView);
        }

        private void DrawTrackCurves(SceneView scene_view)
        {
            // 检查是否选中了我们的Track
            if (Selection.activeObject != current_track_)
            {
                return;
            }

            // 开始绘制
            Handles.BeginGUI();
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 300, 20), "正在绘制 CurveTrack 曲线");
            Handles.EndGUI();

            // 获取所有clips
            TimelineClip[] clips = current_track_.GetClips()?.ToArray();
            if (clips == null || clips.Length == 0)
            {
                Handles.BeginGUI();
                GUI.color = Color.green;
                GUI.Label(new Rect(10, 30, 300, 20), "没有找到任何 Clips");
                Handles.EndGUI();
                return;
            }

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                CurveClipAsset asset = clip.asset as CurveClipAsset;
                if (asset == null) continue;

                GameObject go = current_track_.GetBoundGameObject();
                if (go == null) continue;

                Transform target_trans = go.FindTransform(asset.target_trans_path_);
                if (target_trans == null)
                {
                    // 显示警告
                    Handles.BeginGUI();
                    GUI.color = Color.green;
                    GUI.Label(new Rect(10, 50, 300, 20), $"未找到目标: {asset.target_trans_path_}");
                    Handles.EndGUI();
                    continue;
                }

                // 绘制曲线预览
                DrawCurvePreview(target_trans, asset, clip);

                // 绘制控制点
                DrawCurveHandles(target_trans, asset, clip);
            }
        }

        private void DrawCurvePreview(Transform target, CurveClipAsset asset, TimelineClip clip)
        {
            if (asset.curve_x_ == null && asset.curve_y_ == null && asset.curve_z_ == null)
                return;

            Vector3 start_pos = target.position;
            int steps = 50;

            Handles.color = Color.green;
            Vector3 previous_point = EvaluatePosition(start_pos, asset, 0);

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                float time = t * (float)clip.duration;
                Vector3 current_point = EvaluatePosition(start_pos, asset, time);

                Handles.DrawLine(previous_point, current_point, 2f);
                previous_point = current_point;
            }

            // 绘制起点和终点标记
            Handles.color = Color.yellow;
            Vector3 start_point = EvaluatePosition(start_pos, asset, 0);
            Vector3 end_point = EvaluatePosition(start_pos, asset, (float)clip.duration);

            Handles.SphereHandleCap(0, start_point, Quaternion.identity, 0.1f, EventType.Repaint);
            Handles.color = Color.red;
            Handles.SphereHandleCap(0, end_point, Quaternion.identity, 0.1f, EventType.Repaint);
        }

        private Vector3 EvaluatePosition(Vector3 origin, CurveClipAsset asset, float time)
        {
            Vector3 pos = origin;

            if (asset.curve_x_ != null && asset.curve_x_.keys.Length > 0)
                pos.x = origin.x + asset.curve_x_.Evaluate(time);
            if (asset.curve_y_ != null && asset.curve_y_.keys.Length > 0)
                pos.y = origin.y + asset.curve_y_.Evaluate(time);
            if (asset.curve_z_ != null && asset.curve_z_.keys.Length > 0)
                pos.z = origin.z + asset.curve_z_.Evaluate(time);

            return pos;
        }

        private void DrawCurveHandles(Transform target, CurveClipAsset asset, TimelineClip clip)
        {
            Vector3 startPos = target.position;
            float duration = (float)clip.duration;

            // 绘制起点手柄
            Vector3 startPoint = EvaluatePosition(startPos, asset, 0);
            Handles.color = Color.yellow;

            EditorGUI.BeginChangeCheck();
            Vector3 newStartPoint = Handles.PositionHandle(startPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newStartPoint - startPoint;
                if (asset.curve_x_ != null) UpdateCurveValue(asset.curve_x_, 0, delta.x);
                if (asset.curve_y_ != null) UpdateCurveValue(asset.curve_y_, 0, delta.y);
                if (asset.curve_z_ != null) UpdateCurveValue(asset.curve_z_, 0, delta.z);
                EditorUtility.SetDirty(asset);
            }

            // 绘制终点手柄
            Vector3 endPoint = EvaluatePosition(startPos, asset, duration);
            Handles.color = Color.red;

            EditorGUI.BeginChangeCheck();
            Vector3 newEndPoint = Handles.PositionHandle(endPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newEndPoint - endPoint;
                if (asset.curve_x_ != null) UpdateCurveValue(asset.curve_x_, duration,
                    asset.curve_x_.Evaluate(duration) + delta.x);
                if (asset.curve_y_ != null) UpdateCurveValue(asset.curve_y_, duration,
                    asset.curve_y_.Evaluate(duration) + delta.y);
                if (asset.curve_z_ != null) UpdateCurveValue(asset.curve_z_, duration,
                    asset.curve_z_.Evaluate(duration) + delta.z);
                EditorUtility.SetDirty(asset);
            }
        }

        private void UpdateCurveValue(AnimationCurve curve, float time, float newValue)
        {
            if (curve == null) return;

            var keysList = curve.keys.ToList();
            int existingIndex = keysList.FindIndex(k => Mathf.Approximately(k.time, time));

            if (existingIndex >= 0)
            {
                Keyframe key = keysList[existingIndex];
                key.value = newValue;
                keysList[existingIndex] = key;
            }
            else
            {
                keysList.Add(new Keyframe(time, newValue));
                keysList.Sort((a, b) => a.time.CompareTo(b.time));
            }

            curve.keys = keysList.ToArray();
        }
    }
}