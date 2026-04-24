using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    // Timeline 窗口中的可视化绘制
    [InitializeOnLoad]
    public class AttackDetectBehaviourDrawer
    {
        static AttackDetectBehaviourDrawer()
        {
            // 注册 Timeline 场景 GUI 绘制
            TimelineEditorWindowCallback.OnTimelineSceneGUI += OnTimelineSceneGUI;
        }

        private static void OnTimelineSceneGUI(SceneView sceneView)
        {
            // 获取当前正在编辑的 Timeline
            var director = TimelineEditor.masterDirector;
            if (director == null) return;

            // 获取当前播放时间
            double current_time = director.time;

            // 获取 Timeline 资源
            var timeline_asset = director.playableAsset as TimelineAsset;
            if (timeline_asset == null) return;

            // 遍历所有轨道
            foreach (var track in timeline_asset.GetOutputTracks())
            {
                // 检查是否是包含 AttackDetectClip 的轨道
                foreach (var clip in track.GetClips())
                {
                    var attack_clip = clip.asset as AttackDetectClipAsset;
                    if (attack_clip == null)
                    {
                        continue;
                    }

                    // 检查当前时间不在 Clip 范围内
                    if (current_time < clip.start || current_time > clip.end)
                    {
                        continue;
                    }

                    // 获取绑定的对象
                    var bound_object = director.GetGenericBinding(track);
                    SkillPlayer player = bound_object as SkillPlayer;
                    if (player == null)
                    {
                        continue;
                    }

                    // 创建临时的 Behaviour 来获取检测区域数据
                    DrawDetectionArea(player, attack_clip, (float)(current_time - clip.start));
                }
            }
        }

        private static void DrawDetectionArea(SkillPlayer player, AttackDetectClipAsset clip, float clip_time)
        {
            // 计算检测中心位置和旋转
            Vector3 detect_center = GetDetectCenterForEditor(player, clip);
            Quaternion detect_rotation = GetDetectRotationForEditor(player, clip);

            switch (clip.area_type_)
            {
                case AttackDetectClipAsset.EDetectAreaType.Box:
                    DrawBoxGizmo(detect_center, detect_rotation, clip.box_size_);
                    break;

                case AttackDetectClipAsset.EDetectAreaType.Circle:
                    DrawCircleGizmo(detect_center, clip.circle_radius_);
                    break;

                case AttackDetectClipAsset.EDetectAreaType.Sector:
                    DrawSectorGizmo(detect_center, detect_rotation, clip.sector_radius_,
                        clip.sector_angle_, clip.sector_thickness_, clip.sector_direction_);
                    break;

                case AttackDetectClipAsset.EDetectAreaType.Cone:
                    DrawConeGizmo(detect_center, detect_rotation, clip.cone_radius_,
                        clip.cone_angle_, clip.cone_height_, clip.cone_direction_);
                    break;
            }
        }

        private static Vector3 GetDetectCenterForEditor(SkillPlayer player, AttackDetectClipAsset clip)
        {
            Vector3 center = clip.bind_type_ == AttackDetectClipAsset.EDetectBindType.Target ?
                player.gameObject.FindTransform(clip.bind_trans_path_).position : Vector3.zero;

            center += clip.position_offset_;
            return center;
        }

        private static Quaternion GetDetectRotationForEditor(SkillPlayer player, AttackDetectClipAsset clip)
        {
            Quaternion rotation = clip.bind_type_ == AttackDetectClipAsset.EDetectBindType.Target ?
               player.gameObject.FindTransform(clip.bind_trans_path_).rotation : Quaternion.identity;

            rotation *= Quaternion.Euler(clip.rotation_offset_);
            return rotation;
        }

        private static void DrawBoxGizmo(Vector3 center, Quaternion rotation, Vector3 size)
        {
            // 绘制半透明填充
            Handles.color = new Color(1f, 0f, 0f, 0.15f);
            Handles.DrawSolidRectangleWithOutline(
                GetBoxCornerPoints(center, rotation, size),
                new Color(1f, 0f, 0f, 0.1f),
                Color.red
            );

            // 绘制线框
            Matrix4x4 old_matrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, size);
            Handles.matrix = old_matrix;
        }

        private static Vector3[] GetBoxCornerPoints(Vector3 center, Quaternion rotation, Vector3 size)
        {
            Vector3 half_size = size * 0.5f;
            Vector3[] points = new Vector3[4];

            // 计算底部四个角点（用于绘制矩形面）
            points[0] = center + rotation * new Vector3(-half_size.x, -half_size.y, -half_size.z);
            points[1] = center + rotation * new Vector3(half_size.x, -half_size.y, -half_size.z);
            points[2] = center + rotation * new Vector3(half_size.x, -half_size.y, half_size.z);
            points[3] = center + rotation * new Vector3(-half_size.x, -half_size.y, half_size.z);

            return points;
        }

        private static void DrawCircleGizmo(Vector3 center, float radius)
        {
            // 绘制半透明圆盘
            Handles.color = new Color(1f, 0f, 0f, 0.15f);
            Handles.DrawSolidDisc(center, Vector3.up, radius);

            // 绘制线框
            Handles.color = Color.red;
            Handles.DrawWireDisc(center, Vector3.up, radius);

            // 绘制其他轴向的圆圈以显示 3D 效果
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }

        private static void DrawSectorGizmo(Vector3 center, Quaternion rotation, float radius, float angle, float thickness, Vector3 sector_direction)
        {
            // 与检测逻辑完全一致：扇形是在平面上延伸并有厚度
            Vector3 sector_normal = rotation * sector_direction.normalized;

            // 计算扇形平面的基向量（与检测逻辑一致）
            Vector3 sector_up = rotation * Vector3.up;
            if (Mathf.Abs(Vector3.Dot(sector_normal, sector_up)) > 0.99f)
                sector_up = rotation * Vector3.forward;

            Vector3 sector_right = Vector3.Cross(sector_normal, sector_up).normalized;
            sector_up = Vector3.Cross(sector_right, sector_normal).normalized;

            float half_angle = angle * 0.5f;
            float half_thickness = thickness * 0.5f;

            // 计算两个面的中心
            Vector3 front_center = center + sector_normal * half_thickness;
            Vector3 back_center = center - sector_normal * half_thickness;

            // 绘制扇形体积
            Handles.color = new Color(1f, 0f, 0f, 0.15f);

            int segments = 24;

            // 绘制前面和后面
            DrawSectorFace(front_center, sector_up, sector_normal, radius, half_angle, segments);
            DrawSectorFace(back_center, sector_up, sector_normal, radius, half_angle, segments);

            // 绘侧面连接
            DrawSectorEdges(front_center, back_center, sector_up, sector_normal, radius, half_angle, segments);

            // 绘制线框
            Handles.color = Color.red;
            DrawSectorWireframe(front_center, back_center, sector_up, sector_normal, radius, half_angle, segments);

            // 绘制中心方向线
            Handles.color = Color.yellow;
            Handles.DrawLine(center, center + sector_normal * thickness);
        }

        private static void DrawSectorFace(Vector3 face_center, Vector3 up, Vector3 normal, float radius, float half_angle, int segments)
        {
            Vector3[] vertices = new Vector3[segments + 2];
            vertices[0] = face_center;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float current_angle = -half_angle + t * 2 * half_angle;
                Vector3 dir = Quaternion.AngleAxis(current_angle, normal) * up;
                vertices[i + 1] = face_center + dir * radius;
            }

            Handles.DrawAAConvexPolygon(vertices);
        }

        private static void DrawSectorWireframe(Vector3 front_center, Vector3 back_center, Vector3 up, Vector3 normal, float radius, float half_angle, int segments)
        {
            // 绘制前面的圆弧和边界
            Vector3 left_dir = Quaternion.AngleAxis(-half_angle, normal) * up;
            Vector3 right_dir = Quaternion.AngleAxis(half_angle, normal) * up;

            Vector3 prev_front_point = front_center + left_dir * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float current_angle = -half_angle + t * 2 * half_angle;
                Vector3 dir = Quaternion.AngleAxis(current_angle, normal) * up;
                Vector3 point = front_center + dir * radius;

                Handles.DrawLine(prev_front_point, point);
                prev_front_point = point;
            }
            Handles.DrawLine(front_center, front_center + left_dir * radius);
            Handles.DrawLine(front_center, front_center + right_dir * radius);

            // 绘制后面的圆弧和边界
            Vector3 prev_back_point = back_center + left_dir * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float current_angle = -half_angle + t * 2 * half_angle;
                Vector3 dir = Quaternion.AngleAxis(current_angle, normal) * up;
                Vector3 point = back_center + dir * radius;

                Handles.DrawLine(prev_back_point, point);
                prev_back_point = point;
            }
            Handles.DrawLine(back_center, back_center + left_dir * radius);
            Handles.DrawLine(back_center, back_center + right_dir * radius);

            // 绘制连接线
            Handles.DrawLine(front_center + left_dir * radius, back_center + left_dir * radius);
            Handles.DrawLine(front_center + right_dir * radius, back_center + right_dir * radius);
            Handles.DrawLine(front_center, back_center);
        }

        private static void DrawSectorEdges(Vector3 front_center, Vector3 back_center, Vector3 up, Vector3 normal, float radius, float half_angle, int segments)
        {
            for (int i = 0; i <= segments; i += 2)
            {
                float t = i / (float)segments;
                float current_angle = -half_angle + t * 2 * half_angle;
                Vector3 dir = Quaternion.AngleAxis(current_angle, normal) * up;
                Vector3 front_point = front_center + dir * radius;
                Vector3 back_point = back_center + dir * radius;
                Handles.DrawLine(front_point, back_point);
            }
        }

        private static void DrawConeGizmo(Vector3 center, Quaternion rotation, float radius, float angle, float height, Vector3 cone_direction)
        {
            Vector3 direction = rotation * cone_direction.normalized;

            // 计算圆锥的基向量
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;

            Vector3 tip = center;
            Vector3 base_center = center + direction * height;

            // 绘制半透明锥体
            Handles.color = new Color(1f, 0f, 0f, 0.15f);

            int segments = 24;
            Vector3[] base_points = new Vector3[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle_rad = i * 2.0f * Mathf.PI / segments;
                Vector3 offset = (right * Mathf.Cos(angle_rad) + up * Mathf.Sin(angle_rad)) * radius;
                base_points[i] = base_center + offset;
            }

            // 绘制三角形面片
            for (int i = 0; i < segments; i++)
            {
                int nextIndex = (i + 1) % segments;
                Handles.DrawAAConvexPolygon(tip, base_points[i], base_points[nextIndex]);
            }

            // 绘制线框
            Handles.color = Color.red;

            // 绘制底面圆
            Handles.DrawWireDisc(base_center, direction, radius);

            // 绘制从顶点到底面的连线
            for (int i = 0; i < segments; i++)
            {
                int next_index = (i + 1) % segments;
                Handles.DrawLine(tip, base_points[i]);
                Handles.DrawLine(base_points[i], base_points[next_index]);

                // 添加中间截面线（用于可视化半径随距离变化）
                if (i % 2 == 0)
                {
                    float t = 0.5f;
                    Vector3 mid_center = center + direction * (height * t);
                    float current_radius = radius * t; // 线性插值
                    Vector3 mid_point = mid_center + (right * Mathf.Cos(i * 2.0f * Mathf.PI / segments) +
                                                       up * Mathf.Sin(i * 2.0f * Mathf.PI / segments)) * current_radius;
                    Handles.DrawLine(base_points[i], mid_point);
                }
            }

            // 绘制中心轴线（点状线）
            Handles.DrawDottedLine(tip, base_center, 4f);
        }
    }
}