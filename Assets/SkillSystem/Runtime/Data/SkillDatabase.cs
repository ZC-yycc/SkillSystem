using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skill System/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillConfig> skills = new List<SkillConfig>();

        public SkillConfig GetSkillById(string skillId)
        {
            return skills.Find(s => s.skillId == skillId);
        }

        public SkillConfig GetSkillByName(string skillName)
        {
            return skills.Find(s => s.skillName == skillName);
        }
    }
}