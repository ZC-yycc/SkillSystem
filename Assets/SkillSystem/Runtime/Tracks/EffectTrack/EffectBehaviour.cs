using UnityEngine;
using UnityEngine.Playables;

namespace SkillSystem
{
    public class EffectBehaviour : PlayableBehaviour
    {
        public EffectClip clip;
        public GameObject owner;

        private GameObject effectInstance;
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
            if (clip.effectPrefab == null) return;

            skillPlayer = owner.GetComponent<SkillPlayer>();
            if (skillPlayer == null) return;

            isPlaying = true;
            SpawnEffect();
        }

        private void SpawnEffect()
        {
            Vector3 spawnPos = GetBindPosition();
            Quaternion spawnRot = Quaternion.Euler(clip.rotation);

            effectInstance = Object.Instantiate(clip.effectPrefab, spawnPos, spawnRot);
            effectInstance.transform.localScale = clip.scale;

            skillPlayer.RegisterEffect(effectInstance);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying) return;
            if (!isPlaying || effectInstance == null) return;

            // 如果跟随目标，每帧更新位置
            if (clip.followTarget)
            {
                effectInstance.transform.position = GetBindPosition();
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying) return;
            if (!isPlaying) return;

            isPlaying = false;

            // 如果播放被中断或正常结束且需要自动销毁
            if (clip.autoDestroy && effectInstance != null)
            {
                // 检查是否正常播放完毕
                double currentTime = playable.GetTime();
                if (currentTime >= clipDuration - 0.01f)
                {
                    Object.Destroy(effectInstance);
                }
            }
        }

        private Vector3 GetBindPosition()
        {
            Vector3 basePos = owner.transform.position;

            switch (clip.bindPoint)
            {
                case EffectBindPoint.Caster:
                    return basePos + clip.offset;

                case EffectBindPoint.CasterHead:
                    return basePos + Vector3.up * 1.5f + clip.offset;

                case EffectBindPoint.CasterHand:
                    // 这里可以根据实际情况获取手部位置
                    return basePos + Vector3.up * 1f + owner.transform.forward * 0.5f + clip.offset;

                case EffectBindPoint.Target:
                    return skillPlayer.GetTargetPosition() + clip.offset;

                case EffectBindPoint.World:
                    return clip.offset;

                default:
                    return basePos + clip.offset;
            }
        }
    }
}