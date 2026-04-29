using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    public enum ESkillType
    {
        Normal,     // 普通技能
        Charge,     // 蓄力技能
        Combo,      // 连击技能
        Ultimate    // 终极技能
    }

    public enum ETargetType
    {
        Self,       // 自身
        Direction,  // 方向
        Target,     // 锁定目标
        Area        // 范围
    }

    [CreateAssetMenu(fileName = "SkillConfig", menuName = "Skill System/Skill Config")]
    public class SkillConfig : ScriptableObject
    {
        [Header("基本信息")]
        public string                                       skill_id_;
        public string                                       skill_name_;
        public Sprite                                       icon_;
        [TextArea(3, 5)]
        public string                                       description_;

        [Header("技能属性")]
        public float                                        cooldown_ = 1f;
        public float                                        mana_cost_ = 10f;
        public ESkillType                                   skill_type_ = ESkillType.Normal;
        public ETargetType                                  target_type_ = ETargetType.Direction;
        public float                                        max_range_ = 5f;

        [Header("Timeline资产")]
        public TimelineAsset                                timeline_asset_;
    }
}