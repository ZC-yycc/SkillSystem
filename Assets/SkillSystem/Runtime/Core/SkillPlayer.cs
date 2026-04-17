using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    public class SkillPlayer : MonoBehaviour
    {
        public PlayableDirector Director { get; private set; }

        [SerializeField] private Animator animator;

        // 轨道绑定标识
        private const string ANIMATOR_TRACK_NAME = "AnimationTrack";
        private const string EFFECT_TRACK_NAME = "EffectTrack";
        private const string AUDIO_TRACK_NAME = "AudioTrack";

        // 动态生成的特效和音频实例
        private List<GameObject> spawnedEffects = new List<GameObject>();
        private List<AudioSource> spawnedAudios = new List<AudioSource>();

        // 当前技能的目标位置
        private Vector3 targetPosition;

        private void Awake()
        {
            Director = GetComponent<PlayableDirector>();
            if (Director == null)
                Director = gameObject.AddComponent<PlayableDirector>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 播放技能Timeline
        /// </summary>
        public void Play(TimelineAsset timeline, Vector3 targetPos)
        {
            targetPosition = targetPos;

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
        /// </summary>
        private void BindTracks()
        {
            var timeline = Director.playableAsset as TimelineAsset;
            if (timeline == null) return;

            foreach (var track in timeline.GetOutputTracks())
            {
                // 根据轨道类型或名称绑定
                if (track.name.Contains(ANIMATOR_TRACK_NAME) && animator != null)
                {
                    Director.SetGenericBinding(track, animator);
                }
                else if (track.name.Contains(EFFECT_TRACK_NAME))
                {
                    Director.SetGenericBinding(track, this);
                }
                else if (track.name.Contains(AUDIO_TRACK_NAME))
                {
                    Director.SetGenericBinding(track, this);
                }
                else if (track is TriggerTrack)
                {
                    Director.SetGenericBinding(track, this);
                }
            }
        }

        /// <summary>
        /// 添加特效到追踪列表
        /// </summary>
        public void RegisterEffect(GameObject effect)
        {
            spawnedEffects.Add(effect);
        }

        /// <summary>
        /// 添加音频到追踪列表
        /// </summary>
        public void RegisterAudio(AudioSource audio)
        {
            spawnedAudios.Add(audio);
        }

        /// <summary>
        /// 获取目标位置
        /// </summary>
        public Vector3 GetTargetPosition() => targetPosition;

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            foreach (var effect in spawnedEffects)
            {
                if (effect != null) Destroy(effect);
            }
            spawnedEffects.Clear();

            foreach (var audio in spawnedAudios)
            {
                if (audio != null) Destroy(audio.gameObject);
            }
            spawnedAudios.Clear();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}