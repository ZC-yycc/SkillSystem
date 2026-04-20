using UnityEngine;
using SkillSystem;
using UnityEngine.Timeline;

public class SkillTest : MonoBehaviour
{
    public TimelineAsset test_skill_;

    private void Start()
    {
        // 注册技能
        SkillManager.Instance.RegisterSkill("TestSkill", test_skill_);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 播放技能
            SkillManager.Instance.PlaySkill("TestSkill", gameObject);
        }
    }
}