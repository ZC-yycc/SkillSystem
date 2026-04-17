using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class AudioClipAsset : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        [Header("音频配置")]
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(-3f, 3f)] public float pitch = 1f;
        public bool loop = false;
        public AudioBindType bindType = AudioBindType.Caster;
        public float spatialBlend = 1f; // 0=2D, 1=3D
        public float minDistance = 1f;
        public float maxDistance = 50f;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AudioBehaviour>.Create(graph);
            AudioBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip = this;
            behaviour.owner = owner;

            return playable;
        }
    }

    public enum AudioBindType
    {
        Caster,     // 绑定在施法者身上
        Target,     // 绑定在目标位置
        World2D     // 2D全局音效
    }
}