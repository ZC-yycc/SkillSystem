using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace SkillSystem
{
    /// <summary>
    /// 蓄力轨道Mixer：像Unity AnimationTrack一样通过AnimationClipPlayable驱动Animator，
    /// 而不是手动调用Animator.Play()。
    /// </summary>
    public class ChargeTrackMixer : PlayableBehaviour
    {
        public override void OnPlayableCreate(Playable playable)
        {
            // Mixer负责将子Playable（ChargeBehaviour）的输出混合后传递给Animator
            // 每个ChargeClipAsset会创建一个ChargeBehaviour Playable
            // 在这里我们动态创建AnimationClipPlayable来实际驱动动画
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            int inputCount = playable.GetInputCount();
            if (inputCount == 0) return;
            
            float totalWeight = 0f;
            
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<ChargeBehaviour> inputPlayable = 
                    (ScriptPlayable<ChargeBehaviour>)playable.GetInput(i);
                
                if (inputPlayable.IsValid())
                {
                    ChargeBehaviour behaviour = inputPlayable.GetBehaviour();
                    if (behaviour != null && behaviour.IsActive)
                    {
                        totalWeight += inputWeight;
                        // 让Behaviour自己处理动画Playable
                        behaviour.ProcessFrame(inputPlayable, info, playerData);
                    }
                }
            }
            
            // 设置总权重来驱动输出
            playable.GetGraph().GetRootPlayable(0).SetSpeed(totalWeight > 0f ? 1f : 0f);
        }
    }
}