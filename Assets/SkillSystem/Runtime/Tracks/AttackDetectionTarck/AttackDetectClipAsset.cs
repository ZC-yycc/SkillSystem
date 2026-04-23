using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [Serializable]
    public class AttackDetectClipAsset : PlayableAsset, ITimelineClipAsset
    {
        /// <summary>
        /// 检测区域类型
        /// </summary>
        public enum EDetectAreaType
        {
            Box,                // 矩形
            Circle,             // 圆形
            Sector,             // 扇形
            Cone,               // 锥形
        }

        /// <summary>
        /// 检测绑定类型
        /// </summary>
        public enum EDetectBindType
        {
            Target,
            World
        }

        [Header("触发器配置")]
        public EDetectAreaType                      area_type_ = EDetectAreaType.Box;
        public EDetectBindType                      bind_type_ = EDetectBindType.Target;

        [ShowIf("area_type_", EDetectAreaType.Box)]
        public Vector3                              box_size_ = Vector3.one;

        [ShowIf("area_type_", EDetectAreaType.Circle)]
        public float                                circle_radius_ = 1f;

        [ShowIf("area_type_", EDetectAreaType.Sector)]
        public float                                sector_radius_ = 1f;

        [ShowIf("area_type_", EDetectAreaType.Sector)]
        public float                                sector_angle_ = 60f;

        [ShowIf("area_type_", EDetectAreaType.Sector)]
        public Vector3                              sector_direction_ = Vector3.up;

        [ShowIf("area_type_", EDetectAreaType.Sector)]
        public float                                sector_thickness_ = 0.5f;

        [ShowIf("area_type_", EDetectAreaType.Cone)]
        public float                                cone_radius_ = 1f;

        [ShowIf("area_type_", EDetectAreaType.Cone)]
        public float                                cone_angle_ = 60f;

        [ShowIf("area_type_", EDetectAreaType.Cone)]
        public Vector3                              cone_direction_ = Vector3.up;

        [ShowIf("area_type_", EDetectAreaType.Cone)]
        public float                                cone_height_ = 1f;

        public Vector3                              position_offset_ = Vector3.zero;
        public Vector3                              rotation_offset_ = Vector3.zero;


        /// <summary>
        /// 绑定的 Transform 路径
        /// </summary>
        [ShowIf("bind_type_", EDetectBindType.Target)]
        public string                               bind_trans_path_;

        /// <summary>
        /// 检测次数，根据检测次数以及该 clip 的时长，决定触发间隔
        /// </summary>
        public int                                  detect_count_ = 1;
        public LayerMask                            detect_layer_ = -1;




        public ClipCaps clipCaps => ClipCaps.None;



        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<AttackDetectBehaviour>.Create(graph);
            AttackDetectBehaviour behaviour = playable.GetBehaviour();

            behaviour.clip_ = this;
            behaviour.owner_ = owner;

            return playable;
        }
    }
}