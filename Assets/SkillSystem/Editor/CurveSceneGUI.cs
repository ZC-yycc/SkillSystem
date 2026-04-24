using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [CustomEditor(typeof(CurveTrack))]
    public class CurveSceneGUI : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            CurveTrack track = target as CurveTrack;
            TimelineClip[] clips = track.GetClips().ToArray();

            foreach (var clip in clips)
            {
                CurveClipAsset asset = clip.asset as CurveClipAsset;
                if (asset == null) continue;

                // 找到对应的Transform
                Transform target_trans = FindTargetInScene(asset.target_trans_path_);
                if (target_trans == null) continue;

                // 绘制曲线预览
                DrawCurvePreview(target_trans, asset, clip); 

                // 绘制控制点
                DrawCurveHandles(target_trans, asset, clip);
            }
        }

        private Transform FindTargetInScene(string path)
        {
            var selected_go = Selection.activeGameObject;
            if (selected_go == null)
            {
                return null;
            }

            return selected_go.FindTransform(path);
        }

        private void DrawCurvePreview(Transform target, CurveClipAsset asset, TimelineClip clip)
        {
            if (asset.curve_x_ == null && asset.curve_y_ == null && asset.curve_z_ == null)
                return;

            Vector3 startPos = target.position;
            int steps = 50;

            Handles.color = Color.green;

            for (int i = 0; i < steps; i++)
            {
                float t1 = (float)i / steps;
                float t2 = (float)(i + 1) / steps;

                Vector3 p1 = EvaluatePosition(startPos, asset, t1 * (float)clip.duration);
                Vector3 p2 = EvaluatePosition(startPos, asset, t2 * (float)clip.duration);

                Handles.DrawLine(p1, p2, 2f);
            }
        }

        private Vector3 EvaluatePosition(Vector3 origin, CurveClipAsset asset, float time)
        {
            Vector3 pos = origin;

            if (asset.curve_x_ != null && asset.curve_x_.length > 0)
                pos.x = origin.x + asset.curve_x_.Evaluate(time);
            if (asset.curve_y_ != null && asset.curve_y_.length > 0)
                pos.y = origin.y + asset.curve_y_.Evaluate(time);
            if (asset.curve_z_ != null && asset.curve_z_.length > 0)
                pos.z = origin.z + asset.curve_z_.Evaluate(time);

            return pos;
        }

        private void DrawCurveHandles(Transform target, CurveClipAsset asset, TimelineClip clip)
        {
            // 在这里可以添加可交互的控制点
            // 例如使用Handles.PositionHandle来编辑关键点

            Vector3 startPos = target.position;
            float duration = (float)clip.duration;

            // 绘制起点和终点手柄
            Handles.color = Color.yellow;
            Vector3 startPoint = EvaluatePosition(startPos, asset, 0);
            Vector3 endPoint = EvaluatePosition(startPos, asset, duration);

            EditorGUI.BeginChangeCheck();
            startPoint = Handles.PositionHandle(startPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                // 更新曲线
                UpdateCurveFromHandle(asset.curve_x_, 0, startPoint.x - startPos.x);
                UpdateCurveFromHandle(asset.curve_y_, 0, startPoint.y - startPos.y);
                UpdateCurveFromHandle(asset.curve_z_, 0, startPoint.z - startPos.z);
            }
        }

        private void UpdateCurveFromHandle(AnimationCurve curve, float time, float value)
        {
            if (curve == null) return;

            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (Mathf.Approximately(keys[i].time, time))
                {
                    keys[i].value = value;
                    curve.keys = keys;
                    break;
                }
            }

            EditorUtility.SetDirty(target);
        }
    }
}