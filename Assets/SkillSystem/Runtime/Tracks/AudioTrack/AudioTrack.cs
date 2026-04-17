using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    [TrackColor(0.8f, 0.4f, 0.2f)]
    [TrackClipType(typeof(AudioClipAsset))]
    [TrackBindingType(typeof(SkillPlayer))]
    public class AudioTrack : TrackAsset
    {
    }
}