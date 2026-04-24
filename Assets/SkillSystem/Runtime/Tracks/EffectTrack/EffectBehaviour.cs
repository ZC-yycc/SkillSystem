using UnityEngine;
using UnityEngine.Playables;
using static SkillSystem.EffectClipAsset;

namespace SkillSystem
{
    public class EffectBehaviour : PlayableBehaviour
    {
        public EffectClipAsset                              clip_;
        public GameObject                                   owner_;

        private GameObject                                  effect_instance_;
        private SkillPlayer                                 skill_player_;
        private double                                      clip_duration_;
        private bool                                        is_playing_;
        private Transform                                   bind_trans_;
        private ParticleSystem                              particle_system_;

        public override void OnGraphStart(Playable playable)
        {
            clip_duration_ = clip_.duration;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (clip_.effect_prefab_ == null) return;

            skill_player_ = owner_.GetComponent<SkillPlayer>();
            if (skill_player_ == null) return;

            bind_trans_ = owner_.FindTransform(clip_.bind_trans_path_);
            is_playing_ = true;
            SpawnEffect();
        }

        private void SpawnEffect()
        {
            effect_instance_ = Object.Instantiate(clip_.effect_prefab_);
            if (clip_.bind_type_ == EEffectBindType.Target)
            {
                effect_instance_.transform.SetParent(bind_trans_);
            }

            SetTransformInfo(effect_instance_);
            skill_player_.RegisterEffect(effect_instance_);

            if(effect_instance_.TryGetComponent(out ParticleSystem particle))
            {
                particle_system_ = particle;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object player_data)
        {
            if (!is_playing_ || effect_instance_ == null) return;

            if (clip_.bind_type_ == EEffectBindType.Target)
            {
                SetPosAndRot(effect_instance_);
            }

            if (particle_system_ != null)
            {
                float current_time = (float)playable.GetTime();
                particle_system_.Simulate(current_time);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!is_playing_) return;

            is_playing_ = false;

            // 如果播放被中断或正常结束且需要自动销毁
            if (clip_.auto_destroy_ && effect_instance_ != null)
            {
                Object.DestroyImmediate(effect_instance_);
            }
        }

        private void SetPosAndRot(GameObject instance)
        {
            if (clip_.bind_type_ == EEffectBindType.World)
            {
                instance.transform.SetPositionAndRotation(skill_player_.transform.position + clip_.offset_, Quaternion.Euler(clip_.rotation_));
            }
            else
            {
                instance.transform.SetPositionAndRotation(bind_trans_.position, bind_trans_.rotation);
            }
        }

        private void SetTransformInfo(GameObject instance)
        {
            if (clip_.bind_type_ == EEffectBindType.World)
            {
                instance.transform.position = skill_player_.transform.position + clip_.offset_;
                instance.transform.rotation = Quaternion.Euler(clip_.rotation_);
                instance.transform.localScale = clip_.scale_;
            }
            else
            {
                instance.transform.position = bind_trans_.position;
                instance.transform.rotation = bind_trans_.rotation;
                instance.transform.localScale = bind_trans_.localScale;
            }
        }
    }
}