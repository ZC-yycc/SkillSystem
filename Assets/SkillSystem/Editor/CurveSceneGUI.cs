using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [CustomEditor(typeof(CurveTrack))]
    public class CurveSceneGUI : Editor
    {
        private CurveTrack                                  current_track_;
        private bool                                        is_listening_ = false;
        private Vector3                                     origin_pos_;
        private GameObject                                  target_go_;
        
        private void OnEnable()
        {
            current_track_ = target as CurveTrack;
            target_go_ = current_track_.GetBoundGameObject();
            if (target_go_ == null) return;

            origin_pos_ = target_go_.transform.position;

            if (!is_listening_)
            {
                SceneView.duringSceneGui += SceneGUI;
                is_listening_ = true;
                SceneView.RepaintAll();
            }
        }

        private void OnDisable()
        {
            if (is_listening_)
            {
                SceneView.duringSceneGui -= SceneGUI;
                is_listening_ = false;
                SceneView.RepaintAll();
            }
        }

        private void SceneGUI(SceneView scene_view)
        {
            if (current_track_ == null)
            {
                current_track_ = target as CurveTrack;
                if (current_track_ == null) return;
            }

            DrawTrackCurves();
        }

        private void DrawTrackCurves()
        {
            if (Selection.activeObject != current_track_) return;
            if (target_go_ == null) return;

            // 标题
            Handles.BeginGUI();
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 300, 20), "编辑曲线轨道（拖拽控制点调整路径）");
            Handles.EndGUI();

            TimelineClip[] clips = current_track_.GetClips()?.ToArray();
            if (clips == null || clips.Length == 0) return;

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                CurveClipAsset asset = clip.asset as CurveClipAsset;
                if (asset == null) continue;


                Transform target_trans = target_go_.FindTransform(asset.target_trans_path_);
                if (target_trans == null) continue;

                // 转换为世界空间的关键点
                List<Vector3> world_points = new List<Vector3>();
                foreach (var pt in asset.key_points_)
                {
                    world_points.Add(origin_pos_ + pt);
                }

                if (world_points.Count < 2) continue;

                // 绘制曲线
                DrawCurveLine(world_points, asset.curve_type_);

                // 绘制可拖拽的控制点
                DrawKeyPointHandles(asset, world_points);
            }
        }

        private void DrawCurveLine(List<Vector3> world_points, CurveClipAsset.CurveType curve_type)
        {
            Handles.color = Color.green;
            int segments = world_points.Count * 20;

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                Vector3 p0 =  CurveTrackHelper.EvaluateCurve(world_points, t0, curve_type);
                Vector3 p1 = CurveTrackHelper.EvaluateCurve(world_points, t1, curve_type);
                Handles.DrawLine(p0, p1, 2f);
            }
        }

        private void DrawKeyPointHandles(CurveClipAsset asset, List<Vector3> world_points)
        {
            for (int i = 0; i < world_points.Count; i++)
            {
                // 起点绿色，终点红色，中间黄色
                if (i == 0) Handles.color = Color.green;
                else if (i == world_points.Count - 1) Handles.color = Color.red;
                else Handles.color = Color.yellow;

                float handle_size = HandleUtility.GetHandleSize(world_points[i]) * 0.1f;
                Handles.SphereHandleCap(0, world_points[i], Quaternion.identity, handle_size, EventType.Repaint);

                // 标签
                Handles.Label(world_points[i] + Vector3.up * handle_size * 2, $"[{i}]");

                // 可拖拽
                EditorGUI.BeginChangeCheck();
                Vector3 new_pos = Handles.PositionHandle(world_points[i], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Move Curve Key Point");
                    asset.key_points_[i] = new_pos - origin_pos_;
                    EditorUtility.SetDirty(asset);
                }
            }
        }
    }
}