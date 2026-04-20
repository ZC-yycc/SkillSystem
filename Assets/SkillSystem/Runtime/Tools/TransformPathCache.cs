using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    /// <summary>
    /// Transform 路径缓存管理器
    /// 每个角色实例持有一个，用于缓存骨骼/挂点的 Transform 引用
    /// </summary>
    public class TransformPathCache
    {
        /// <summary>
        /// 路径 -> Transform 的缓存字典
        /// </summary>
        private readonly Dictionary<string, Transform>                          path_cache_ = new Dictionary<string, Transform>();

        /// <summary>
        /// 反向缓存：Transform -> 路径（用于快速获取路径）
        /// </summary>
        private readonly Dictionary<Transform, string>                          revers_cache_ = new Dictionary<Transform, string>();

        /// <summary>
        /// 根节点
        /// </summary>
        private readonly Transform                                              root_;

        /// <summary>
        /// 是否使用模糊匹配（忽略大小写、支持通配符）
        /// </summary>
        private readonly bool                                                   fuzzy_match_ = true;

        public TransformPathCache(Transform root, bool use_fuzzy_match = true)
        {
            root_ = root;
            fuzzy_match_ = use_fuzzy_match;
        }

        /// <summary>
        /// 通过路径获取 Transform（带缓存）
        /// </summary>
        public Transform GetTransform(string path)
        {
            if (string.IsNullOrEmpty(path)) return root_;
            if (root_ == null) return null;

            // 标准化路径
            string normalized_path = fuzzy_match_ ? path.ToLower().Replace('\\', '/') : path;

            // 检查缓存
            if (path_cache_.TryGetValue(normalized_path, out Transform cached))
            {
                // 验证缓存是否仍然有效（Transform 可能被销毁）
                if (cached != null)
                    return cached;

                // 缓存失效，移除
                path_cache_.Remove(normalized_path);
            }

            // 执行查找
            Transform found = root_.Find(path);

            if (found != null)
            {
                // 存入缓存
                path_cache_[normalized_path] = found;
                revers_cache_[found] = normalized_path;
            }

            return found;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void Clear()
        {
            path_cache_.Clear();
            revers_cache_.Clear();
        }

        /// <summary>
        /// 检查缓存是否包含路径
        /// </summary>
        public bool HasPath(string path)
        {
            return path_cache_.ContainsKey(fuzzy_match_ ? path.ToLower() : path);
        }
    }
}