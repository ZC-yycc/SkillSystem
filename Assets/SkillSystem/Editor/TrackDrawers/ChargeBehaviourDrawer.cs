using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [InitializeOnLoad]
    public class ChargeBehaviourDrawer
    {
        static ChargeBehaviourDrawer()
        {
            TimelineEditorWindowCallback.OnTimelineSceneGUI += OnTimelineSceneGUI;
        }

        private static void OnTimelineSceneGUI(SceneView sceneView)
        {
            var director = TimelineEditor.masterDirector;
            if (director == null) return;

            double current_time = director.time;

            var timeline_asset = director.playableAsset as TimelineAsset;
            if (timeline_asset == null) return;

            foreach (var track in timeline_asset.GetOutputTracks())
            {
                if (!(track is ChargeTrack charge_track)) continue;

                foreach (var clip in charge_track.GetClips())
                {
                    var charge_clip = clip.asset as ChargeClipAsset;
                    if (charge_clip == null) continue;

                    if (current_time < clip.start || current_time > clip.end) continue;

                    DrawChargeInfo(sceneView, charge_clip, clip, current_time, director);
                }
            }
        }

        private static void DrawChargeInfo(SceneView sceneView, ChargeClipAsset clip,
            TimelineClip timelineClip, double current_time, PlayableDirector director)
        {
            var bound_object = director.GetGenericBinding(timelineClip.GetParentTrack());
            GameObject go = bound_object as GameObject;
            if (go == null) return;

            SkillPlayer player = go.GetComponent<SkillPlayer>();
            if (player == null) return;

            float clip_progress = (float)((current_time - timelineClip.start) / timelineClip.duration);
            clip_progress = Mathf.Clamp01(clip_progress);

            Vector3 world_pos = player.transform.position + Vector3.up * 2.5f;

            Handles.BeginGUI();

            float bar_width = 120f;
            float bar_height = 12f;
            Vector2 gui_pos = HandleUtility.WorldToGUIPoint(world_pos);
            Rect bg_rect = new Rect(gui_pos.x - bar_width / 2, gui_pos.y, bar_width, bar_height);

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            GUI.Box(bg_rect, "", GUI.skin.window);

            GUI.color = Color.Lerp(Color.yellow, Color.red, clip_progress);
            Rect fill_rect = new Rect(bg_rect.x + 2, bg_rect.y + 2, (bar_width - 4) * clip_progress, bar_height - 4);
            GUI.Box(fill_rect, "", GUI.skin.window);

            GUI.color = Color.white;
            var label_style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(bg_rect.x, bg_rect.y - 18, bar_width, 18),
                $"蓄力: {(clip_progress * 100):F0}%", label_style);

            Handles.EndGUI();

            Handles.color = new Color(0.6f, 0.2f, 0.8f, 0.3f);
            Handles.DrawWireDisc(player.transform.position, Vector3.up, 1.5f);

            if (clip_progress > 0.01f)
            {
                Handles.color = new Color(0.6f, 0.2f, 0.8f, 0.5f);
                DrawProgressArc(player.transform.position, 1.5f, clip_progress);
            }
        }

        private static void DrawProgressArc(Vector3 center, float radius, float progress)
        {
            int segments = 32;
            int active_segments = Mathf.CeilToInt(segments * progress);
            float angle_step = 360f / segments;

            for (int i = 0; i < active_segments; i++)
            {
                float angle0 = i * angle_step * Mathf.Deg2Rad;
                float angle1 = (i + 1) * angle_step * Mathf.Deg2Rad;

                Vector3 p0 = center + new Vector3(Mathf.Cos(angle0) * radius, 0, Mathf.Sin(angle0) * radius);
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);

                Handles.DrawLine(p0, p1, 2f);
            }
        }
    }
}