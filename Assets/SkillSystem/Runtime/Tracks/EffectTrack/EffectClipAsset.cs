using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class EffectClipAsset : PlayableAsset, ITimelineClipAsset
    {
        public enum EEffectBindType
        {
            Target,         // 目标位置
            World           // 世界坐标
        }

        [Header("特效配置")]
        public GameObject                                           effect_prefab_;
        public bool                                                 auto_destroy_ = true;


        public EEffectBindType                                      bind_type_ = EEffectBindType.Target;

        [ShowIf("bind_type_", EEffectBindType.Target)]
        public string                                               bind_trans_path_;

        [ShowIf("bind_type_", EEffectBindType.World)]
        public Vector3                                              offset_ = Vector3.zero;
        [ShowIf("bind_type_", EEffectBindType.World)]
        public Vector3                                              rotation_ = Vector3.zero;
        [ShowIf("bind_type_", EEffectBindType.World)]
        public Vector3                                              scale_ = Vector3.one;





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
}