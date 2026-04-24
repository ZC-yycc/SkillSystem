using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;

namespace SkillSystem
{
    [Serializable]
    public class CurveClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("目标Transform的路径")]
        public string                               target_trans_path_;

        [Tooltip("X轴曲线")]
        public AnimationCurve                       curve_x_ = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Tooltip("Y轴曲线")]
        public AnimationCurve                       curve_y_ = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Tooltip("Z轴曲线")]
        public AnimationCurve                       curve_z_ = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending | ClipCaps.Extrapolation; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CurveBehaviour>.Create(graph);
            CurveBehaviour behaviour = playable.GetBehaviour();
            behaviour.clip_ = this;
            behaviour.owner_ = owner;

            // 设置数据
            behaviour.curve_x_ = curve_x_;
            behaviour.curve_y_ = curve_y_;
            behaviour.curve_z_ = curve_z_;

            return playable;
        }
    }
}