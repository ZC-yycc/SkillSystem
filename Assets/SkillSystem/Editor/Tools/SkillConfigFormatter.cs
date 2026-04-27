using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace SkillSystem
{
    public static class SkillConfigFormatter
    {
        public static void ToBinary(SkillConfig config, string path)
        {
            if (config == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个 SkillConfig 文件", "确定");
                return;
            }

            if (string.IsNullOrEmpty(path)) return;

            string json = ToJson(config);

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                formatter.Serialize(stream, json);
            }
        }
        public static void FromBinary(string file_path, SkillConfig config)
        {
            if (!File.Exists(file_path))
            {
                Debug.LogError($"文件不存在: {file_path}");
                return;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using FileStream stream = new FileStream(file_path, FileMode.Open);
            string json = formatter.Deserialize(stream) as string;
            FromJson(json, config);
        }





        public static string ToJson(SkillConfig config)
        {
            return JsonUtility.ToJson(config, true);
        }
        public static void FromJson(string json, SkillConfig config)
        {
            JsonUtility.FromJsonOverwrite(json, config);
        }
    }
}