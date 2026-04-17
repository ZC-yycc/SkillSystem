using UnityEngine;
using UnityEngine.Playables;

namespace SkillSystem
{
    public class AudioBehaviour : PlayableBehaviour
    {
        public AudioClipAsset clip;
        public GameObject owner;

        private GameObject audioObject;
        private AudioSource audioSource;
        private SkillPlayer skillPlayer;
        private double clipDuration;
        private bool isPlaying;

        public override void OnGraphStart(Playable playable)
        {
            clipDuration = clip.duration;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying) return;
            if (clip.audioClip == null) return;

            skillPlayer = owner.GetComponent<SkillPlayer>();
            if (skillPlayer == null) return;

            isPlaying = true;
            CreateAudioSource();
        }

        private void CreateAudioSource()
        {
            audioObject = new GameObject($"Audio_{clip.audioClip.name}");

            // 设置父级和位置
            if (clip.bindType == AudioBindType.Caster)
            {
                audioObject.transform.SetParent(owner.transform);
                audioObject.transform.localPosition = Vector3.zero;
            }
            else if (clip.bindType == AudioBindType.Target)
            {
                audioObject.transform.position = skillPlayer.GetTargetPosition();
            }
            else
            {
                // 2D音效，放在原点
                audioObject.transform.position = Vector3.zero;
            }

            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip.audioClip;
            audioSource.volume = clip.volume;
            audioSource.pitch = clip.pitch;
            audioSource.loop = clip.loop;
            audioSource.spatialBlend = clip.bindType == AudioBindType.World2D ? 0f : clip.spatialBlend;
            audioSource.minDistance = clip.minDistance;
            audioSource.maxDistance = clip.maxDistance;

            audioSource.Play();

            skillPlayer.RegisterAudio(audioSource);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying) return;
            if (!isPlaying || audioObject == null) return;

            // 如果绑定在目标位置且不跟随，则不需要更新
            if (clip.bindType == AudioBindType.Target && audioObject.transform.parent == null)
            {
                // 可选：如果需要跟随移动的目标，在这里更新位置
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying) return;
            if (!isPlaying) return;

            isPlaying = false;

            if (audioSource != null)
            {
                audioSource.Stop();

                // 检查是否正常播放完毕
                double currentTime = playable.GetTime();
                if (currentTime >= clipDuration - 0.01f)
                {
                    Object.Destroy(audioObject);
                }
            }
        }
    }
}