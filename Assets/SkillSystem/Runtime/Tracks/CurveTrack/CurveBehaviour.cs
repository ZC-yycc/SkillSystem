using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;



namespace SkillSystem
{
    [System.Serializable]
    public class CurveBehaviour : PlayableBehaviour
    {
        public CurveClipAsset                   clip_;
        public GameObject                       owner_;
        private SkillPlayer                     skill_player_;

        public Transform                        target_trans_;
        private Vector3                         original_position_;
        private bool                            position_saved_ = false;

        public List<Vector3>                    key_points_;
        public CurveClipAsset.CurveType         curve_type_;



        public override void OnGraphStart(Playable playable)
        {
            skill_player_ = owner_.GetComponent<SkillPlayer>();
            if (skill_player_ == null) return;

            target_trans_ = owner_.FindTransform(clip_.target_trans_path_);
            if (target_trans_ != null && !position_saved_)
            {
                original_position_ = target_trans_.position;
                position_saved_ = true;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (target_trans_ == null || key_points_ == null || key_points_.Count < 2)
                return;

            float time = (float)playable.GetTime();
            float duration = (float)playable.GetDuration();

            float t = Mathf.Clamp01(time / duration);

            Vector3 offset = CurveTrackHelper.EvaluateCurve(key_points_, t, curve_type_);
            // 注意：这里 offset 是相对于起点的，需要转换为相对于 origin
            Vector3 origin = target_trans_.position; // 或使用 clip 起始位置
            // 实际项目请根据你的坐标约定调整
        }
    }
}