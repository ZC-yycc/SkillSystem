using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;
using System.Collections.Generic;

namespace SkillSystem
{
    [Serializable]
    public class CurveClipAsset : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("目标Transform的路径")]
        public string                                   target_trans_path_;

        [Tooltip("曲线关键点列表（相对于起点的偏移）")]
        public List<Vector3>                            key_points_ = new List<Vector3>
        {
            Vector3.zero,          // 起点
            new Vector3(0, 0, 1),  // 终点
        };

        [Tooltip("曲线类型")]
        public CurveType                                curve_type_ = CurveType.CatmullRom;

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
            behaviour.key_points_ = new List<Vector3>(key_points_);
            behaviour.curve_type_ = curve_type_;
            return playable;
        }

        public enum CurveType
        {
            Linear,         // 线性插值
            CatmullRom,     // 平滑曲线
            Bezier,         // 贝塞尔曲线
        }
    }
}