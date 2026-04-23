using UnityEngine;
using UnityEngine.Playables;
using static SkillSystem.AudioClipAsset;

namespace SkillSystem
{
    /// <summary>
    /// 音频播放行为
    /// </summary>
    public class AudioBehaviour : PlayableBehaviour
    {
        public AudioClipAsset                       clip_;
        public GameObject                           owner_;

        private GameObject                          audio_target_;
        private AudioSource                         audio_source_;
        private SkillPlayer                         skill_player_;
        private double                              clip_duration_;
        private bool                                is_playing_;

        public override void OnGraphStart(Playable playable)
        {
            clip_duration_ = clip_.duration;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (clip_.audio_clip_ == null) return;

            skill_player_ = owner_.GetComponent<SkillPlayer>();
            if (skill_player_ == null) return;

            is_playing_ = true;
            CreateAudioSource();
        }

        private void CreateAudioSource()
        {
            audio_target_ = new GameObject($"Audio_{clip_.audio_clip_.name}");

            // 设置父级和位置
            if (clip_.bind_type_ == EAudioBindType.Target)
            {
                audio_target_.transform.SetParent(owner_.transform);
                audio_target_.transform.localPosition = Vector3.zero;
            }
            else if (clip_.bind_type_ == EAudioBindType.Position)
            {
                audio_target_.transform.position = skill_player_.transform.position;
            }
            else
            {
                // 2D音效，放在原点
                audio_target_.transform.position = Vector3.zero;
            }

            audio_source_ = audio_target_.AddComponent<AudioSource>();
            audio_source_.clip = clip_.audio_clip_;
            audio_source_.volume = clip_.volume_;
            audio_source_.pitch = Random.Range(clip_.random_pitch_range_.x, clip_.random_pitch_range_.y);
            audio_source_.loop = clip_.is_loop_;
            audio_source_.spatialBlend = clip_.bind_type_ == EAudioBindType.World2D ? 0f : clip_.spatial_blend_;
            audio_source_.minDistance = clip_.min_distance_;
            audio_source_.maxDistance = clip_.max_distance_;

            audio_source_.Play();

            skill_player_.RegisterAudio(audio_source_);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!is_playing_) return;
            if (audio_source_ == null) return;
            
            is_playing_ = false;
            audio_source_.Stop();

            Object.DestroyImmediate(audio_target_);
        }
    }
}