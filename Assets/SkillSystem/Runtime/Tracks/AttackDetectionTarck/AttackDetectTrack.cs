using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [TrackColor(0.8f, 0.2f, 0.2f)]
    [TrackClipType(typeof(AttackDetectClipAsset))]
    [TrackBindingType(typeof(SkillPlayer))]
    public class AttackDetectTrack : TrackAsset
    {
    }
}