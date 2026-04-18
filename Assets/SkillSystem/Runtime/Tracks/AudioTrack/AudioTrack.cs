using UnityEngine;
using UnityEngine.Timeline;

namespace SkillSystem
{
    /// <summary>
    /// 技能音频轨道注册
    /// </summary>
    
    [TrackColor(0.8f, 0.4f, 0.2f)]
    [TrackClipType(typeof(AudioClipAsset))]
    [TrackBindingType(typeof(SkillPlayer))]
    public class AudioTrack : TrackAsset
    {
    }
}