using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [RequireComponent(typeof(PlayableDirector))]
    public class SkillPlayer : MonoBehaviour
    {
        private Animator                                    animator_;

        // 轨道绑定标识
        private const string                                ANIMATOR_TRACK_NAME = "AnimationTrack";
        private const string                                EFFECT_TRACK_NAME = "EffectTrack";
        private const string                                AUDIO_TRACK_NAME = "AudioTrack";

        // 动态生成的特效和音频实例
        private readonly List<GameObject>                   spawned_effects_ = new List<GameObject>();
        private readonly List<AudioSource>                  spawned_audios_ = new List<AudioSource>();

        // 蓄力相关
        private ChargeBehaviour                             current_charge_behaviour_;


        public PlayableDirector Director { get; private set; }

        /// <summary>
        /// 当前是否正在蓄力
        /// </summary>
        public bool IsCharging => current_charge_behaviour_ != null && current_charge_behaviour_.IsCharging;

        /// <summary>
        /// 当前蓄力进度（0~1），未蓄力时返回0
        /// </summary>
        public float ChargeProgress => current_charge_behaviour_ != null ? current_charge_behaviour_.ChargeProgress : 0f;

        private void Awake()
        {
            Director = GetComponent<PlayableDirector>();

            if (animator_ == null)
                animator_ = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 播放技能Timeline
        /// </summary>
        public void Play(TimelineAsset timeline)
        {
            // 清理上一次播放的资源
            Cleanup();

            // 设置Timeline资产
            Director.playableAsset = timeline;

            // 绑定通用轨道
            BindTracks();

            // 开始播放
            Director.Play();
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            Director.Stop();
            Cleanup();
        }

        /// <summary>
        /// 绑定所有轨道到当前GameObject
        /// ChargeTrack绑定到this（GameObject），允许通过GetComponent获取Animator驱动动画
        /// </summary>
        private void BindTracks()
        {
            var timeline = Director.playableAsset as TimelineAsset;
            if (timeline == null) return;

            foreach (var track in timeline.GetOutputTracks())
            {
                // 根据轨道类型或名称绑定
                if (track.name.Contains(ANIMATOR_TRACK_NAME) && animator_ != null)
                {
                    Director.SetGenericBinding(track, animator_);
                }
                else if (track.name.Contains(EFFECT_TRACK_NAME))
                {
                    Director.SetGenericBinding(track, this);
                }
                else if (track.name.Contains(AUDIO_TRACK_NAME))
                {
                    Director.SetGenericBinding(track, this);
                }
                else if (track is AttackDetectTrack)
                {
                    Director.SetGenericBinding(track, this);
                }
                else if (track is ChargeTrack)
                {
                    // ChargeTrack绑定到GameObject，通过getters获取Animator和SkillPlayer
                    Director.SetGenericBinding(track, gameObject);
                }
            }
        }

        /// <summary>
        /// 添加特效到追踪列表
        /// </summary>
        public void RegisterEffect(GameObject effect)
        {
            spawned_effects_.Add(effect);
        }

        /// <summary>
        /// 添加音频到追踪列表
        /// </summary>
        public void RegisterAudio(AudioSource audio)
        {
            spawned_audios_.Add(audio);
        }

        #region 蓄力输入接口

        /// <summary>
        /// 尝试释放当前蓄力。由外部输入系统调用。
        /// </summary>
        public void TryReleaseCharge()
        {
            if (current_charge_behaviour_ == null) return;
            if (!current_charge_behaviour_.TryRelease())
            {
                Debug.Log("蓄力时间不足，无法释放");
            }
        }

        /// <summary>
        /// 取消当前蓄力。由外部输入系统调用。
        /// </summary>
        public void CancelCharge()
        {
            if (current_charge_behaviour_ == null) return;
            current_charge_behaviour_.CancelCharge();
        }

        /// <summary>
        /// 由ChargeBehaviour回调：蓄力开始
        /// </summary>
        internal void OnChargeStarted(ChargeBehaviour behaviour)
        {
            current_charge_behaviour_ = behaviour;
        }

        /// <summary>
        /// 由ChargeBehaviour回调：每帧更新蓄力状态
        /// </summary>
        internal void OnChargeUpdate(ChargeBehaviour behaviour)
        {
            current_charge_behaviour_ = behaviour;
        }

        /// <summary>
        /// 由ChargeBehaviour回调：蓄力结束
        /// </summary>
        internal void OnChargeEnded(ChargeBehaviour behaviour)
        {
            if (current_charge_behaviour_ == behaviour)
            {
                current_charge_behaviour_ = null;
            }
        }

        /// <summary>
        /// 由ChargeBehaviour回调：执行蓄力释放（跳转Timeline时间到ChargeClip之后）
        /// </summary>
        internal void ExecuteChargeRelease(ChargeBehaviour behaviour)
        {
            var timeline = Director.playableAsset as TimelineAsset;
            if (timeline == null) return;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is ChargeTrack charge_track)
                {
                    foreach (var clip in charge_track.GetClips())
                    {
                        var charge_asset = clip.asset as ChargeClipAsset;
                        if (charge_asset != null)
                        {
                            double jump_time = clip.start + clip.duration;
                            Director.time = jump_time;
                            Director.Evaluate();
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 由ChargeBehaviour回调：取消蓄力（停止整个Timeline）
        /// </summary>
        internal void CancelCharge(ChargeBehaviour behaviour)
        {
            Director.Stop();
            Cleanup();
            current_charge_behaviour_ = null;
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            current_charge_behaviour_ = null;

            foreach (var effect in spawned_effects_)
            {
                if (effect != null) Destroy(effect);
            }
            spawned_effects_.Clear();

            foreach (var audio in spawned_audios_)
            {
                if (audio != null) Destroy(audio.gameObject);
            }
            spawned_audios_.Clear();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}