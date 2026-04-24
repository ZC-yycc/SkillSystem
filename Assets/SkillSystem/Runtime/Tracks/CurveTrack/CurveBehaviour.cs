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

        public AnimationCurve                   curve_x_ = new AnimationCurve();
        public AnimationCurve                   curve_y_ = new AnimationCurve();
        public AnimationCurve                   curve_z_ = new AnimationCurve();

        public Transform                        target_trans_;
        private Vector3                         original_position_;
        private bool                            position_saved_ = false;

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
            if (target_trans_ == null)
                return;

            float time = (float)playable.GetTime();
            float duration = (float)playable.GetDuration();

            // 处理循环
            time = Mathf.Clamp(time, 0f, duration);

            // 从曲线中计算位置
            Vector3 newPosition = original_position_;

            if (curve_x_ != null && curve_x_.length > 0)
                newPosition.x = original_position_.x + curve_x_.Evaluate(time);

            if (curve_y_ != null && curve_y_.length > 0)
                newPosition.y = original_position_.y + curve_y_.Evaluate(time);

            if (curve_z_ != null && curve_z_.length > 0)
                newPosition.z = original_position_.z + curve_z_.Evaluate(time);

            target_trans_.position = newPosition;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (target_trans_ != null && info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // 可选：播放结束时恢复原始位置
                // targetTrans.position = originalPosition;
            }
        }
    }
}