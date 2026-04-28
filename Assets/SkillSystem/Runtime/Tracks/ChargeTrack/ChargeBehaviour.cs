using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SkillSystem
{
    /// <summary>
    /// 蓄力行为：通过PlayableGraph中的AnimationClipPlayable驱动动画（类似Unity AnimationTrack机制）。
    /// 不直接调用Animator.Play()，而是通过PlayableGraph让Timeline系统自动混合输出到Animator。
    /// </summary>
    public class ChargeBehaviour : PlayableBehaviour
    {
        public ChargeClipAsset clip_;
        public GameObject owner_;
        public string name_;
        public bool IsActive { get; set; }

        private SkillPlayer skill_player_;
        private Animator animator_;
        private bool is_charging_;
        private bool is_released_;
        private double charge_accumulated_time_;

        public bool IsCharging => is_charging_ && !is_released_;
        public bool IsReleased => is_released_;
        public double ChargeAccumulatedTime => charge_accumulated_time_;

        public float ChargeProgress
        {
            get
            {
                float duration = (float)clip_.duration;
                if (duration <= 0f) return 1f;
                return Mathf.Clamp01((float)charge_accumulated_time_ / duration);
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            skill_player_ = owner_.GetComponent<SkillPlayer>();
            animator_ = owner_.GetComponentInChildren<Animator>();
            is_charging_ = false;
            is_released_ = false;
            charge_accumulated_time_ = 0.0;
            IsActive = false;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (clip_ == null || skill_player_ == null) return;

            is_charging_ = true;
            is_released_ = false;
            charge_accumulated_time_ = 0.0;
            IsActive = true;

            skill_player_.OnChargeStarted(this);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (is_charging_)
            {
                skill_player_.OnChargeEnded(this);
            }

            is_charging_ = false;
            IsActive = false;
        }

        /// <summary>
        /// 由ChargeTrackMixer每帧调用，通过PlayableGraph驱动动画。
        /// 动画已经由AnimationClipPlayable驱动，这里只需要累积时间和更新SkillPlayer回调。
        /// </summary>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            if (!is_charging_ || is_released_) return;
            if (skill_player_ == null) return;
            if (animator_ == null) return;

            // 累积蓄力时间
            charge_accumulated_time_ += info.deltaTime;

            // 通过AnimationClipPlayable驱动动画（循环播放蓄力动画）
            if (clip_.charge_animation_ != null)
            {
                // 动画时间循环：通过PlayableGraph自动驱动，不需要手动Play
                float cycle_time = clip_.charge_animation_.length > 0
                    ? (float)(charge_accumulated_time_ % clip_.charge_animation_.length)
                    : (float)charge_accumulated_time_;

                // AnimationClipPlayable已经由ChargeClipAsset在CreatePlayable中创建并连接到PlayableGraph
                // Timeline系统会自动将PlayableGraph的输出混合到Animator
            }

            skill_player_.OnChargeUpdate(this);
        }

        /// <summary>
        /// 尝试释放蓄力
        /// </summary>
        public bool TryRelease()
        {
            if (!is_charging_ || is_released_) return false;

            if (charge_accumulated_time_ < clip_.min_charge_time_)
                return false;

            is_released_ = true;
            IsActive = false;

            skill_player_.ExecuteChargeRelease(this);
            return true;
        }

        /// <summary>
        /// 取消蓄力
        /// </summary>
        public void CancelCharge()
        {
            if (!is_charging_ || is_released_) return;

            is_released_ = true;
            IsActive = false;

            skill_player_.CancelCharge(this);
        }
    }
}