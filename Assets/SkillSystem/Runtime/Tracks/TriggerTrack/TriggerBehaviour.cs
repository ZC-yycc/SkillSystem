using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace SkillSystem
{
    public class TriggerBehaviour : PlayableBehaviour
    {
        public TriggerClip clip;
        public GameObject owner;

        private SkillPlayer skillPlayer;
        private bool isActive;
        private float lastTriggerTime;
        private double clipStartTime;

        // 攻击检测相关
        private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
        private Collider[] hitColliders = new Collider[32];

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying) return;

            skillPlayer = owner.GetComponent<SkillPlayer>();
            if (skillPlayer == null) return;

            isActive = true;
            clipStartTime = playable.GetTime();
            lastTriggerTime = 0f;
            hitTargets.Clear();

            OnTriggerStart();
        }

        private void OnTriggerStart()
        {
            switch (clip.triggerType)
            {
                case TriggerType.Invincible:
                    // 设置无敌状态
                    break;

                case TriggerType.MovementLock:
                    // 锁定移动
                    break;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying) return;
            if (!isActive) return;

            float currentTime = (float)(playable.GetTime() - clipStartTime);

            switch (clip.triggerType)
            {
                case TriggerType.Attack:
                    ProcessAttack(currentTime);
                    break;

                case TriggerType.Custom:
                    ProcessCustomEvent(currentTime);
                    break;
            }
        }

        private void ProcessAttack(float currentTime)
        {
            // 按间隔检测
            if (currentTime - lastTriggerTime < clip.triggerInterval)
                return;

            lastTriggerTime = currentTime;
            DetectAttack();
        }

        private void DetectAttack()
        {
            Vector3 attackPos = owner.transform.position +
                owner.transform.forward * clip.attackOffset.z +
                owner.transform.right * clip.attackOffset.x +
                owner.transform.up * clip.attackOffset.y;

            // 球形检测
            int hitCount = Physics.OverlapSphereNonAlloc(attackPos, clip.attackRange,
                hitColliders, clip.targetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                GameObject target = hitColliders[i].gameObject;

                // 检查角度
                Vector3 dirToTarget = (target.transform.position - owner.transform.position).normalized;
                float angle = Vector3.Angle(owner.transform.forward, dirToTarget);

                if (angle <= clip.attackAngle / 2f)
                {
                    // 避免重复命中
                    if (hitTargets.Contains(target))
                        continue;

                    hitTargets.Add(target);
                    OnHit(target);
                }
            }
        }

        private void OnHit(GameObject target)
        {
            Debug.Log($"命中目标: {target.name}, 伤害: {clip.damage}");

            // 这里可以调用目标的伤害接口
            var damageable = target.GetComponent<IDamageable>();
            damageable?.TakeDamage(clip.damage, owner);
        }

        private void ProcessCustomEvent(float currentTime)
        {
            if (currentTime - lastTriggerTime < clip.triggerInterval)
                return;

            lastTriggerTime = currentTime;

            if (!string.IsNullOrEmpty(clip.customEventName))
            {
                // 发送自定义事件
                SkillEventManager.TriggerEvent(clip.customEventName, owner);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying) return;
            if (!isActive) return;

            isActive = false;

            switch (clip.triggerType)
            {
                case TriggerType.Invincible:
                    // 取消无敌状态
                    break;

                case TriggerType.MovementLock:
                    // 解锁移动
                    break;
            }

            hitTargets.Clear();
        }
    }

    // 伤害接口示例
    public interface IDamageable
    {
        void TakeDamage(int damage, GameObject source);
    }

    // 事件管理器
    public static class SkillEventManager
    {
        public static void TriggerEvent(string eventName, GameObject source)
        {
            Debug.Log($"触发技能事件: {eventName}, 来源: {source.name}");
            // 这里可以实现你的事件系统
        }
    }
}