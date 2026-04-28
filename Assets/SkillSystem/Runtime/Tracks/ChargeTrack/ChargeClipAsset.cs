using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class ChargeClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [Header("蓄力动画")]
        [Tooltip("蓄力期间播放的动画片段")]
        public AnimationClip charge_animation_;

        [Header("蓄力配置")]
        [Tooltip("最小蓄力时间(秒)，小于此时间无法释放")]
        public float min_charge_time_ = 0f;

        [Tooltip("蓄力期间是否允许移动")]
        public bool can_move_during_charge_ = false;

        [Tooltip("蓄力期间移动速度倍率")]
        [Range(0f, 1f)]
        public float move_speed_multiplier_ = 0.5f;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ChargeBehaviour>.Create(graph);
            ChargeBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip_ = this;
            behaviour.owner_ = owner;

            // 像Unity AnimationTrack一样创建AnimationClipPlayable驱动动画
            // 这样蓄力动画通过PlayableGraph驱动，不需要手动Animator.Play()
            if (charge_animation_ != null)
            {
                AnimationClipPlayable anim_playable = AnimationClipPlayable.Create(graph, charge_animation_);
                anim_playable.SetApplyFootIK(false);
                anim_playable.SetApplyPlayableIK(false);
                anim_playable.SetDuration(charge_animation_.length);
                anim_playable.SetSpeed(1f);

                // 将动画Playable输出到混合器的对应输入槽
                playable.SetInputCount(playable.GetInputCount() + 1);
                if (playable.GetInputCount() > 1)
                {
                    playable.DisconnectInput(playable.GetInputCount() - 1);
                }
                playable.ConnectInput(playable.GetInputCount() - 1, anim_playable, 0, 0f);
                playable.SetInputWeight(playable.GetInputCount() - 1, 1f);
            }

            return playable;
        }
    }
}