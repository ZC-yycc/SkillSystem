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

        public PlayableDirector Director { get; private set; }

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
                else if (track is AnimationTrack)
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

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
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