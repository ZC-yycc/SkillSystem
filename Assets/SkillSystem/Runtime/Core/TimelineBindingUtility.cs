using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    public static class TimelineBindingUtility
    {
        /// <summary>
        /// 从 CurveTrack 获取绑定的 GameObject
        /// </summary>
        public static GameObject GetBoundGameObject(this CurveTrack track)
        {
            if (track == null) return null;

            var directors = Object.FindObjectsByType<PlayableDirector>();
            foreach (var director in directors)
            {
                var bound_object = GetBoundObjectFromDirector(track, director);
                if (bound_object != null)
                {
                    return bound_object;
                }
            }

            return null;
        }

        /// <summary>
        /// 从指定的 PlayableDirector 获取 Track 绑定的 GameObject
        /// </summary>
        public static GameObject GetBoundObjectFromDirector(TrackAsset track, PlayableDirector director)
        {
            if (track == null || director == null) return null;

            // 获取绑定对象
            var binding = director.GetGenericBinding(track);

            if (binding == null) return null;

            // 根据不同类型转换
            if (binding is GameObject gameObject)
            {
                return gameObject;
            }
            else if (binding is Component component)
            {
                return component.gameObject;
            }
            else if (binding is Transform transform)
            {
                return transform.gameObject;
            }

            return null;
        }
    }
}