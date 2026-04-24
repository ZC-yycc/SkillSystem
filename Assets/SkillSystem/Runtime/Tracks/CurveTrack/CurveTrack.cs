using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;


namespace SkillSystem
{
    [TrackColor(0.2f, 0.8f, 0.4f)]
    [TrackClipType(typeof(CurveClipAsset))]
    [TrackBindingType(typeof(SkillPlayer))]
    public class CurveTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int input_count)
        {
            return ScriptPlayable<CurveTrackMixer>.Create(graph, input_count);
        }
    }
}