using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [TrackColor(0.2f, 0.8f, 0.2f)]
    [TrackClipType(typeof(EffectClip))]
    [TrackBindingType(typeof(SkillPlayer))]
    public class EffectTrack : TrackAsset
    {
    }
}