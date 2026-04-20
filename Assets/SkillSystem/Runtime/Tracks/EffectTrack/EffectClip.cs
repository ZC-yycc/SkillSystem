using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class EffectClip : PlayableAsset, ITimelineClipAsset
    {

        [Header("特效配置")]
        public GameObject                                   effect_prefab_;
        public EBindType                                    bind_type_ = EBindType.Target;
        public string                                       bind_trans_path_;
        public Vector3                                      offset_ = Vector3.zero;
        public Vector3                                      rotation = Vector3.zero;
        public Vector3                                      scale = Vector3.one;
        public bool                                         auto_destroy_ = true;



        public ClipCaps clipCaps => ClipCaps.None;



        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<EffectBehaviour>.Create(graph);
            EffectBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip_ = this;
            behaviour.owner_ = owner;

            return playable;
        }
    }

    public enum EBindType
    {
        Target,         // 目标位置
        World           // 世界坐标
    }
}