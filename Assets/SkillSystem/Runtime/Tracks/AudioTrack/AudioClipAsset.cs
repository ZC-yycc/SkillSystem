using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    public enum EAudioBindType
    {
        Target,     // 绑定在施法者身上
        Position,   // 绑定在目标位置
        World2D     // 2D全局音效
    }

    /// <summary>
    /// 音频轨道资源配置
    /// </summary>

    [Serializable]
    public class AudioClipAsset : PlayableAsset, ITimelineClipAsset
    {

        [Header("音频配置")]
        public AudioClip                                audio_clip_;
        [Range(0f, 1f)] public float                    volume_ = 1f;
        public Vector2                                  random_pitch_range_ = new Vector2(0.8f, 1.2f);
        public EAudioBindType                           bind_type_ = EAudioBindType.Position;
        public bool                                     is_loop_ = false;
        public float                                    spatial_blend_ = 1f; // 0=2D, 1=3D
        public float                                    min_distance_ = 1f;
        public float                                    max_distance_ = 50f;


        public ClipCaps clipCaps => ClipCaps.None;



        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AudioBehaviour>.Create(graph);
            AudioBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip_ = this;
            behaviour.owner_ = owner;

            return playable;
        }
    }
}