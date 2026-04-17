using UnityEngine;

namespace SkillSystem
{
    public class SkillAnimationHelper : MonoBehaviour
    {
        private Animator animator;
        private SkillPlayer skillPlayer;

        // 动画事件回调
        public System.Action<string> OnAnimationEvent;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            skillPlayer = GetComponent<SkillPlayer>();
        }

        /// <summary>
        /// 由Animation Event调用
        /// </summary>
        public void OnSkillEvent(string eventName)
        {
            OnAnimationEvent?.Invoke(eventName);
        }

        /// <summary>
        /// 启用根运动
        /// </summary>
        public void EnableRootMotion(bool enable)
        {
            if (animator != null)
                animator.applyRootMotion = enable;
        }

        /// <summary>
        /// 设置动画速度
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator != null)
                animator.speed = speed;
        }
    }
}