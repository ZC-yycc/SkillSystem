using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace SkillSystem
{
    public class AttackDetectBehaviour : PlayableBehaviour
    {
        public AttackDetectClipAsset                        clip_;
        public GameObject                                   owner_;
        private SkillPlayer                                 skill_player_;

        private Transform                                   bind_trans_;
        private float                                       clip_duration_;
        private int                                         current_detect_count_;
        private float                                       detect_interval_;
        private readonly HashSet<GameObject>                already_hit_targets_ = new HashSet<GameObject>();

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
            already_hit_targets_.Clear();
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (clip_ == null || owner_ == null)
            {
                return;
            }

            // 初始化绑定目标
            skill_player_ = owner_.GetComponent<SkillPlayer>();
            if (skill_player_ == null)
            {
                return;
            }

            bind_trans_ = owner_.FindTransform(clip_.bind_trans_path_);

            // 计算检测间隔
            clip_duration_ = (float)playable.GetDuration();
            detect_interval_ = clip_.detect_count_ > 0 ? clip_duration_ / clip_.detect_count_ : clip_duration_;

            // 重置检测状态
            current_detect_count_ = 0;
            already_hit_targets_.Clear();

            // 立即执行第一次检测
            PerformDetection(0);
            current_detect_count_ = 1;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (clip_ == null || owner_ == null)
            {
                return;
            }

            float current_time = (float)playable.GetTime();

            // 是否已检测完毕
            if (current_detect_count_ >= clip_.detect_count_)
            {
                return;
            }

            float next_detect_time = current_detect_count_ * detect_interval_;
            if (current_time >= next_detect_time)
            {
                PerformDetection(current_time);
                current_detect_count_++;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // 清理检测列表
            already_hit_targets_.Clear();
        }

        private void PerformDetection(float current_time)
        {
            // 获取检测中心位置
            Vector3 detect_center = GetDetectCenter();
            Quaternion detect_rotation = GetDetectRotation();

            // 根据区域类型执行检测
            already_hit_targets_.Clear();

            switch (clip_.area_type_)
            {
                case AttackDetectClipAsset.EDetectAreaType.Box:
                    DetectBox(detect_center, detect_rotation);
                    break;
                case AttackDetectClipAsset.EDetectAreaType.Circle:
                    DetectCircle(detect_center);
                    break;
                case AttackDetectClipAsset.EDetectAreaType.Sector:
                    DetectSector(detect_center, detect_rotation);
                    break;
                case AttackDetectClipAsset.EDetectAreaType.Cone:
                    DetectCone(detect_center, detect_rotation);
                    break;
            }

            // 处理检测到的目标
            ProcessDetectedTargets();
        }

        private Vector3 GetDetectCenter()
        {
            Vector3 center = Vector3.zero;

            if (clip_.bind_type_ == AttackDetectClipAsset.EDetectBindType.Target)
            {
                center = bind_trans_.position;
            }

            center += clip_.position_offset_;
            return center;
        }

        private Quaternion GetDetectRotation()
        {
            Quaternion rotation = Quaternion.identity;

            if (clip_.bind_type_ == AttackDetectClipAsset.EDetectBindType.Target)
            {
                rotation = bind_trans_.rotation;
            }
            rotation *= Quaternion.Euler(clip_.rotation_offset_);
            return rotation;
        }

        private void DetectBox(Vector3 center, Quaternion rotation)
        {
            Collider[] colliders = Physics.OverlapBox(
                center,
                clip_.box_size_ * 0.5f,
                rotation,
                clip_.detect_layer_
            );

            foreach (var collider in colliders)
            {
                if (IsValidTarget(collider.gameObject))
                    already_hit_targets_.Add(collider.gameObject);
            }
        }

        private void DetectCircle(Vector3 center)
        {
            Collider[] colliders = Physics.OverlapSphere(
                center,
                clip_.circle_radius_,
                clip_.detect_layer_
            );

            foreach (var collider in colliders)
            {
                if (IsValidTarget(collider.gameObject))
                    already_hit_targets_.Add(collider.gameObject);
            }
        }
        private void DetectSector(Vector3 center, Quaternion rotation)
        {
            // 获取扇形方向（扇形平面的法线方向决定了扇面的朝向）
            Vector3 sector_normal = rotation * clip_.sector_direction_.normalized;

            // 计算扇形平面的基向量
            Vector3 sector_up = rotation * Vector3.up;

            // 如果法线方向与up平行，使用forward作为参考
            if (Mathf.Abs(Vector3.Dot(sector_normal, sector_up)) > 0.99f)
                sector_up = rotation * Vector3.forward;

            Vector3 sector_right = Vector3.Cross(sector_normal, sector_up).normalized;
            sector_up = Vector3.Cross(sector_right, sector_normal).normalized;

            // 先进行球形检测（粗略检测）
            float max_radius = Mathf.Max(clip_.sector_radius_, clip_.sector_thickness_);
            Collider[] colliders = Physics.OverlapSphere(center, max_radius, clip_.detect_layer_);

            float half_angle = clip_.sector_angle_ * 0.5f;
            float half_thickness = clip_.sector_thickness_ * 0.5f;

            foreach (var collider in colliders)
            {
                if (!IsValidTarget(collider.gameObject))
                    continue;

                Vector3 target_pos = GetColliderCenter(collider);
                Vector3 dir_to_target = target_pos - center;

                // 计算到扇形平面的垂直距离（厚度方向）
                float normal_distance = Vector3.Dot(dir_to_target, sector_normal);

                // 检查厚度
                if (Mathf.Abs(normal_distance) > half_thickness)
                    continue;

                // 计算在扇形平面上的投影距离
                Vector3 projected_dir = dir_to_target - sector_normal * normal_distance;
                float projected_distance = projected_dir.magnitude;

                // 检查半径
                if (projected_distance > clip_.sector_radius_)
                    continue;

                // 如果距离为0，说明目标在中心点，直接判定为在扇形内
                if (projected_distance < 0.01f)
                {
                    already_hit_targets_.Add(collider.gameObject);
                    continue;
                }

                // 检查角度
                // 使用扇形平面上的参考方向（默认使用sectorUp作为0度方向）
                Vector3 projected_dir_normalized = projected_dir / projected_distance;

                // 需要检查360度范围内的角度
                float signed_angle = Vector3.SignedAngle(sector_up, projected_dir_normalized, sector_normal);
                float abs_angle = Mathf.Abs(signed_angle);

                if (abs_angle <= half_angle)
                {
                    already_hit_targets_.Add(collider.gameObject);
                }
            }
        }

        // 获取碰撞体的中心位置
        private Vector3 GetColliderCenter(Collider collider)
        {
            if (collider is BoxCollider box_collider)
                return collider.transform.TransformPoint(box_collider.center);
            else if (collider is SphereCollider sphere_collider)
                return collider.transform.TransformPoint(sphere_collider.center);
            else if (collider is CapsuleCollider capsule_collider)
                return collider.transform.TransformPoint(capsule_collider.center);
            else
                return collider.bounds.center;
        }

        private void DetectCone(Vector3 center, Quaternion rotation)
        {
            // 获取扇形方向
            Vector3 direction = rotation * clip_.cone_direction_.normalized;

            // 先进行球形检测（粗略检测）
            float max_radius = Mathf.Max(clip_.cone_radius_, clip_.cone_height_);
            Collider[] colliders = Physics.OverlapSphere(center, max_radius, clip_.detect_layer_);

            foreach (var collider in colliders)
            {
                if (!IsValidTarget(collider.gameObject))
                    continue;

                Vector3 target_pos = collider.transform.position;
                Vector3 dir_to_target = target_pos - center;
                float distance = dir_to_target.magnitude;

                // 检查距离
                if (distance > clip_.cone_height_)
                    continue;

                // 检查角度
                float angle = Vector3.Angle(direction, dir_to_target.normalized);
                if (angle > clip_.cone_angle_ * 0.5f)
                    continue;

                // 检查半径
                Vector3 projected_dir = Vector3.ProjectOnPlane(dir_to_target, direction);
                float projected_distance = projected_dir.magnitude;

                // 根据距离计算当前截面半径
                float t = distance / clip_.cone_height_;
                float current_radius = Mathf.Lerp(0, clip_.cone_radius_, t);

                if (projected_distance <= current_radius)
                {
                    already_hit_targets_.Add(collider.gameObject);
                }
            }
        }

        private bool IsValidTarget(GameObject target)
        {
            // 排除自身
            if (target == owner_)
                return false;

            // 检查是否已经击中过
            if (already_hit_targets_.Contains(target))
                return false;

            return true;
        }

        private void ProcessDetectedTargets()
        {
            foreach (var target in already_hit_targets_)
            {
                // 这里可以触发伤害计算或其他逻辑
                // 建议通过事件系统或接口来通知目标被击中
                OnTargetDetected(target);

                Debug.Log($"[AttackDetection] 检测到目标: {target.name} 在时间: {Time.time}");
            }
        }

        private void OnTargetDetected(GameObject target)
        {
            // TODO: 实现具体的伤害逻辑
            // 例如：
            // var damageable = target.GetComponent<IDamageable>();
            // if (damageable != null)
            //     damageable.TakeDamage(damage);

            // 或者通过消息系统发送攻击命中事件
            // EventManager.TriggerEvent(new AttackHitEvent(owner_, target, clip_));
        }
    }
}
