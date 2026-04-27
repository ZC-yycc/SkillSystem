using UnityEngine;

namespace SkillSystem
{
    public class PathUtility
    {
        /// <summary>
        /// 将绝对路径转换为相对于 Unity 项目 Assets 目录的路径
        /// </summary>
        /// <param name="absolute_path">绝对路径，如 "D:/Unity_Projects/SkillSystem/Assets/SkillSystem/Timelines"</param>
        /// <returns>相对路径，如 "Assets/SkillSystem/Timelines"</returns>
        public static string ToAssetsRelativePath(string absolute_path)
        {
            if (string.IsNullOrEmpty(absolute_path))
                return absolute_path;

            // 统一使用正斜杠，避免路径分隔符不一致的问题
            string normalized_path = absolute_path.Replace("\\", "/");
            string data_path = Application.dataPath.Replace("\\", "/");

            if (normalized_path.StartsWith(data_path))
            {
                // 去掉 dataPath 部分，并加上 "Assets"
                string relative = normalized_path.Substring(data_path.Length).TrimStart('/');
                return "Assets/" + relative;
            }

            Debug.LogWarning($"路径不在 Unity 项目内: {absolute_path}");
            return absolute_path;
        }
    }
}