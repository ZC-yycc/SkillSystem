using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class TriggerClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        [Header("触发器配置")]
        public TriggerType triggerType = TriggerType.Attack;
        public float triggerInterval = 0.1f; // 触发间隔（秒）

        [Header("攻击检测配置（仅Attack类型）")]
        public float attackRange = 2f;
        public float attackAngle = 60f;
        public Vector3 attackOffset = Vector3.zero;
        public LayerMask targetLayer = -1;
        public int damage = 10;

        [Header("事件配置")]
        public string customEventName;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TriggerBehaviour>.Create(graph);
            TriggerBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip = this;
            behaviour.owner = owner;

            return playable;
        }
    }

    public enum TriggerType
    {
        Attack,         // 攻击检测
        Custom,         // 自定义事件
        Invincible,     // 无敌
        MovementLock    // 移动锁定
    }
}