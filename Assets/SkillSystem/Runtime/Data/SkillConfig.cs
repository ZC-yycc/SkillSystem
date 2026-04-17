using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Skill System/Skill Config")]
    public class SkillConfig : ScriptableObject
    {
        [Header("基本信息")]
        public string skillId;
        public string skillName;
        public Sprite icon;
        [TextArea(3, 5)]
        public string description;

        [Header("技能属性")]
        public float cooldown = 1f;
        public float manaCost = 10f;
        public SkillType skillType = SkillType.Normal;
        public TargetType targetType = TargetType.Direction;
        public float maxRange = 5f;

        [Header("Timeline资产")]
        public TimelineAsset timelineAsset;

        [Header("高级设置")]
        public bool canInterrupt = true;
        public bool canMoveDuringCast = false;
        public int priority = 0;
        public AnimationClip overrideIdleClip;
        public AnimationClip overrideMoveClip;
    }

    public enum SkillType
    {
        Normal,     // 普通技能
        Charge,     // 蓄力技能
        Combo,      // 连击技能
        Ultimate    // 终极技能
    }

    public enum TargetType
    {
        Self,       // 自身
        Direction,  // 方向
        Target,     // 锁定目标
        Area        // 范围
    }
}