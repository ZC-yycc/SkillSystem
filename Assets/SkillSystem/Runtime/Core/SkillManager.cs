using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        private Dictionary<string, TimelineAsset> skillLibrary = new Dictionary<string, TimelineAsset>();
        private Dictionary<GameObject, SkillPlayer> activePlayers = new Dictionary<GameObject, SkillPlayer>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 注册技能到库中
        /// </summary>
        public void RegisterSkill(string skillId, TimelineAsset timeline)
        {
            skillLibrary[skillId] = timeline;
        }

        /// <summary>
        /// 播放技能
        /// </summary>
        public SkillPlayer PlaySkill(string skillId, GameObject caster)
        {
            if (!skillLibrary.TryGetValue(skillId, out TimelineAsset timeline))
            {
                Debug.LogError($"Skill '{skillId}' not found!");
                return null;
            }

            // 获取或创建SkillPlayer组件
            if (!activePlayers.TryGetValue(caster, out SkillPlayer player))
            {
                player = caster.GetComponent<SkillPlayer>();
                if (player == null)
                    player = caster.AddComponent<SkillPlayer>();
                activePlayers[caster] = player;
            }

            player.Play(timeline);
            return player;
        }

        /// <summary>
        /// 停止技能
        /// </summary>
        public void StopSkill(GameObject caster)
        {
            if (activePlayers.TryGetValue(caster, out SkillPlayer player))
            {
                player.Stop();
            }
        }
    }
}