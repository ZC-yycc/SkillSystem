using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [TrackColor(0.6f, 0.2f, 0.8f)]
    [TrackClipType(typeof(ChargeClipAsset))]
    [TrackBindingType(typeof(GameObject))]
    public class ChargeTrack : TrackAsset
    {
    }
}