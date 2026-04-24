using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    /// <summary>
    /// Transform 路径缓存管理器
    /// 每个角色实例持有一个，用于缓存骨骼/挂点的 Transform 引用
    /// </summary>
    public static class TransformPathCache
    {
        /// <summary>
        /// 路径 -> Transform 的缓存字典
        /// </summary>
        private static readonly Dictionary<GameObject, Dictionary<string, Transform>>      path_cache_ = new();

        /// <summary>
        /// 是否使用模糊匹配（忽略大小写、支持通配符）
        /// </summary>
        private static readonly bool                                                        fuzzy_match_ = true;

        /// <summary>
        /// 通过路径获取 Transform（带缓存）, 如果路径不存在则返回根节点
        /// </summary>
        public static Transform FindTransform(this GameObject root_obj, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return root_obj.transform;
            }

            // 标准化路径
            string normalized_path = fuzzy_match_ ? path.ToLower().Replace('\\', '/') : path;

            // 检查对象缓存
            if (!path_cache_.TryGetValue(root_obj, out var pairs))
            {
                // 没有缓存，查找一次
                Transform finded = root_obj.transform.Find(path);
                if (finded == null)
                {
                    // 没有找到，返回根节点
                    return root_obj.transform;
                }

                // 存入缓存并返回
                pairs = new Dictionary<string, Transform>();
                pairs.Add(normalized_path, finded);
                path_cache_[root_obj] = pairs;
                return finded;
            }


            // 检查 transform 缓存
            if (pairs.TryGetValue(normalized_path, out Transform cached))
            {
                // 验证缓存是否仍然有效
                if (cached != null) return cached;

                // 缓存失效，移除
                pairs.Remove(normalized_path);
            }

            // 执行查找
            Transform found = root_obj.transform.Find(path);
            if (found == null)
            {
                return root_obj.transform;
            }

            // 存入缓存
            pairs[normalized_path] = found;
            return found;
        }
    }
}