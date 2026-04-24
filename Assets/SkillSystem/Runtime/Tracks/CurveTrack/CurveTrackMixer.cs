using UnityEngine;
using UnityEngine.Playables;

namespace SkillSystem
{
    public class CurveTrackMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Transform track_binding = playerData as Transform;

            if (track_binding == null)
                return;

            int input_count = playable.GetInputCount();

            for (int i = 0; i < input_count; i++)
            {
                float input_weight = playable.GetInputWeight(i);

                if (input_weight > 0)
                {
                    ScriptPlayable<CurveBehaviour> input_playable = (ScriptPlayable<CurveBehaviour>)playable.GetInput(i);
                    CurveBehaviour input = input_playable.GetBehaviour();
                }
            }
        }
    }
}