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
        public EffectBindPoint                              bind_point_ = EffectBindPoint.Caster;
        public Vector3                                      offset_ = Vector3.zero;
        public Vector3                                      rotation_ = Vector3.zero;
        public Vector3                                      scale_ = Vector3.one;
        public bool                                         follow_target_ = true;
        public bool                                         auto_destroy_ = true;



        public ClipCaps clipCaps => ClipCaps.None;



        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<EffectBehaviour>.Create(graph);
            EffectBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip = this;
            behaviour.owner = owner;

            return playable;
        }
    }

    public enum EffectBindPoint
    {
        Caster,         // 施法者中心
        CasterHead,     // 施法者头部
        CasterHand,     // 施法者手部
        Target,         // 目标位置
        World           // 世界坐标
    }
}