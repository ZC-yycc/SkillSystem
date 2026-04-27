using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skill System/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillConfig> skills_ = new List<SkillConfig>();

        public SkillConfig GetSkillById(string skill_id)
        {
            return skills_.Find(s => s.skill_id_ == skill_id);
        }

        public SkillConfig GetSkillByName(string skill_name)
        {
            return skills_.Find(s => s.skill_name_ == skill_name);
        }
    }
}