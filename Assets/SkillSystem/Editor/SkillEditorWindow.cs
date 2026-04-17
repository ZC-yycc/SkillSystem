using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEditor.Timeline;
using System.IO;
using System.Linq;

namespace SkillSystem.Editor
{
    /// <summary>
    /// 技能编辑器主窗口
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        #region 常量定义
        private const string WINDOW_TITLE = "技能编辑器";
        private const string TIMELINE_FOLDER = "Assets/SkillSystem/Timelines";
        private const string SKILL_DATA_FOLDER = "Assets/SkillSystem/Data";
        private const string EDITOR_PREFS_LAST_SKILL = "SkillEditor_LastEditedSkill";
        #endregion

        #region UI状态
        private Vector2 skillListScrollPos;
        private Vector2 inspectorScrollPos;
        private string newSkillName = "NewSkill";
        private string searchFilter = "";
        private int selectedTab = 0;
        private readonly string[] tabs = { "技能库", "全局设置", "导出工具" };
        #endregion

        #region 技能数据
        private SkillDatabase skillDatabase;
        private SkillConfig selectedSkill;
        private TimelineAsset selectedTimeline;
        private PlayableDirector previewDirector;
        private GameObject previewInstance;
        private bool isPreviewMode = false;
        #endregion

        #region 编辑器实例
        private UnityEditor.Editor timelineEditor;
        private UnityEditor.Editor skillConfigEditor;
        #endregion

        [MenuItem("Skill System/技能编辑器 %#S", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("AnimationWindow").image);
            window.minSize = new Vector2(1200, 700);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabase();
            EnsureFoldersExist();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            CleanupPreview();

            // 保存最后编辑的技能
            if (selectedSkill != null)
            {
                EditorPrefs.SetString(EDITOR_PREFS_LAST_SKILL, selectedSkill.skillId);
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CleanupPreview();
            }
        }

        private void LoadDatabase()
        {
            // 查找或创建技能数据库
            string dbPath = $"{SKILL_DATA_FOLDER}/SkillDatabase.asset";
            skillDatabase = AssetDatabase.LoadAssetAtPath<SkillDatabase>(dbPath);

            if (skillDatabase == null)
            {
                skillDatabase = CreateInstance<SkillDatabase>();
                AssetDatabase.CreateAsset(skillDatabase, dbPath);
                AssetDatabase.SaveAssets();
            }

            // 恢复上次编辑的技能
            string lastSkillId = EditorPrefs.GetString(EDITOR_PREFS_LAST_SKILL, "");
            if (!string.IsNullOrEmpty(lastSkillId))
            {
                selectedSkill = skillDatabase.skills.Find(s => s.skillId == lastSkillId);
                if (selectedSkill != null)
                {
                    LoadTimelineForSkill(selectedSkill);
                }
            }
        }

        private void EnsureFoldersExist()
        {
            if (!AssetDatabase.IsValidFolder(TIMELINE_FOLDER))
            {
                Directory.CreateDirectory(TIMELINE_FOLDER);
                AssetDatabase.Refresh();
            }

            if (!AssetDatabase.IsValidFolder(SKILL_DATA_FOLDER))
            {
                Directory.CreateDirectory(SKILL_DATA_FOLDER);
                AssetDatabase.Refresh();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(30));

            switch (selectedTab)
            {
                case 0:
                    DrawSkillLibraryTab();
                    break;
                case 1:
                    DrawGlobalSettingsTab();
                    break;
                case 2:
                    DrawExportToolsTab();
                    break;
            }
        }

        #region 工具栏
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 新建技能按钮
            if (GUILayout.Button("新建技能", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewSkill();
            }

            // 保存按钮
            GUI.enabled = selectedSkill != null;
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveCurrentSkill();
            }
            GUI.enabled = true;

            // 预览模式开关
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = isPreviewMode ? Color.green : Color.gray;
            if (GUILayout.Button(isPreviewMode ? "■ 停止预览" : "▶ 预览", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TogglePreview();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.FlexibleSpace();

            // 搜索框
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(35));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }

        private void TogglePreview()
        {
            if (isPreviewMode)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        }

        private void StartPreview()
        {
            if (selectedTimeline == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个技能Timeline", "确定");
                return;
            }

            // 在场景中创建预览角色
            CleanupPreview();

            previewInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            previewInstance.name = "[Preview] Skill Caster";
            previewInstance.transform.position = Vector3.zero;

            var animator = previewInstance.AddComponent<Animator>();
            var skillPlayer = previewInstance.AddComponent<SkillPlayer>();

            // 设置Timeline
            previewDirector = previewInstance.GetComponent<PlayableDirector>();
            if (previewDirector == null)
                previewDirector = previewInstance.AddComponent<PlayableDirector>();

            previewDirector.playableAsset = selectedTimeline;

            // 绑定轨道
            var timeline = selectedTimeline as TimelineAsset;
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track.name.Contains("AnimationTrack"))
                    previewDirector.SetGenericBinding(track, animator);
                else if (track.name.Contains("EffectTrack") || track.name.Contains("AudioTrack") || track is TriggerTrack)
                    previewDirector.SetGenericBinding(track, skillPlayer);
            }

            // 定位Timeline窗口
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");

            // 选中预览对象
            Selection.activeGameObject = previewInstance;

            // 开始播放
            previewDirector.Play();
            isPreviewMode = true;
        }

        private void StopPreview()
        {
            if (previewDirector != null)
            {
                previewDirector.Stop();
            }
            isPreviewMode = false;
        }

        private void CleanupPreview()
        {
            StopPreview();

            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
            }

            previewDirector = null;
        }
        #endregion

        #region 技能库标签页
        private void DrawSkillLibraryTab()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧：技能列表
            DrawSkillList();

            // 右侧：Timeline编辑器区域
            DrawTimelineArea();

            EditorGUILayout.EndHorizontal();

            // 底部：技能配置面板
            DrawSkillConfigPanel();
        }

        private void DrawSkillList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            EditorGUILayout.LabelField("技能列表", EditorStyles.boldLabel);

            skillListScrollPos = EditorGUILayout.BeginScrollView(skillListScrollPos);

            if (skillDatabase != null)
            {
                var filteredSkills = skillDatabase.skills
                    .Where(s => string.IsNullOrEmpty(searchFilter) ||
                           s.skillName.Contains(searchFilter) ||
                           s.skillId.Contains(searchFilter))
                    .ToList();

                foreach (var skill in filteredSkills)
                {
                    DrawSkillListItem(skill);
                }
            }

            EditorGUILayout.EndScrollView();

            // 新建技能快速入口
            EditorGUILayout.BeginHorizontal();
            newSkillName = EditorGUILayout.TextField(newSkillName);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                CreateNewSkill(newSkillName);
                newSkillName = "NewSkill";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillListItem(SkillConfig skill)
        {
            bool isSelected = selectedSkill == skill;

            Color bgColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 技能图标
            if (skill.icon != null)
                GUILayout.Label(AssetPreview.GetAssetPreview(skill.icon), GUILayout.Width(32), GUILayout.Height(32));
            else
                GUILayout.Label(EditorGUIUtility.IconContent("AnimationClip Icon").image, GUILayout.Width(32), GUILayout.Height(32));

            // 技能信息
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(skill.skillName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {skill.skillId}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 点击选中
            Rect itemRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                SelectSkill(skill);
                Event.current.Use();
            }

            GUI.backgroundColor = bgColor;
        }

        private void DrawTimelineArea()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Timeline 编辑器", EditorStyles.boldLabel);

            if (selectedSkill != null && GUILayout.Button("定位Timeline", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (selectedTimeline != null)
                {
                    EditorGUIUtility.PingObject(selectedTimeline);
                    Selection.activeObject = selectedTimeline;
                }
            }

            if (GUILayout.Button("打开Timeline窗口", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            }
            EditorGUILayout.EndHorizontal();

            // Timeline预览区域
            if (selectedTimeline != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(200));
                EditorGUILayout.LabelField($"当前Timeline: {selectedTimeline.name}");

                if (timelineEditor == null || timelineEditor.target != selectedTimeline)
                {
                    timelineEditor = UnityEditor.Editor.CreateEditor(selectedTimeline);
                }

                if (timelineEditor != null)
                {
                    timelineEditor.OnInspectorGUI();
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("选择或创建一个技能以编辑Timeline", MessageType.Info);

                if (selectedSkill != null && GUILayout.Button("创建新Timeline", GUILayout.Height(40)))
                {
                    CreateTimelineForSkill(selectedSkill);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillConfigPanel()
        {
            if (selectedSkill == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("技能配置", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 折叠按钮
            EditorGUILayout.EndHorizontal();

            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos, GUILayout.Height(250));

            if (skillConfigEditor == null || skillConfigEditor.target != selectedSkill)
            {
                skillConfigEditor = UnityEditor.Editor.CreateEditor(selectedSkill);
            }

            if (skillConfigEditor != null)
            {
                skillConfigEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("复制技能"))
            {
                DuplicateSkill(selectedSkill);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("删除技能"))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除技能 '{selectedSkill.skillName}' 吗？", "删除", "取消"))
                {
                    DeleteSkill(selectedSkill);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 全局设置标签页
        private void DrawGlobalSettingsTab()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("全局设置", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            // 路径设置
            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Timeline保存路径:", GUILayout.Width(120));
            EditorGUILayout.TextField(TIMELINE_FOLDER);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择Timeline保存路径", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对路径
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数据保存路径:", GUILayout.Width(120));
            EditorGUILayout.TextField(SKILL_DATA_FOLDER);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择数据保存路径", "Assets", "");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 默认配置
            EditorGUILayout.LabelField("默认轨道配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认动画轨道名称:", GUILayout.Width(150));
            EditorGUILayout.TextField("AnimationTrack");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认特效轨道名称:", GUILayout.Width(150));
            EditorGUILayout.TextField("EffectTrack");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认音频轨道名称:", GUILayout.Width(150));
            EditorGUILayout.TextField("AudioTrack");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认触发器轨道名称:", GUILayout.Width(150));
            EditorGUILayout.TextField("TriggerTrack");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 调试选项
            EditorGUILayout.LabelField("调试选项", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("显示调试信息", false);
            EditorGUILayout.Toggle("自动刷新技能列表", true);

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 导出工具标签页
        private void DrawExportToolsTab()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("导出工具", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            // 导出选项
            EditorGUILayout.LabelField("导出配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出格式:", GUILayout.Width(80));
            string[] formats = { "JSON", "Binary", "ScriptableObject" };
            int formatIndex = EditorGUILayout.Popup(0, formats);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出路径:", GUILayout.Width(80));
            EditorGUILayout.TextField("Assets/StreamingAssets/Skills/");
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择导出路径", "Assets", "");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 导出按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("导出当前技能", GUILayout.Height(40)))
            {
                if (selectedSkill != null)
                {
                    ExportSkill(selectedSkill);
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择一个技能", "确定");
                }
            }

            if (GUILayout.Button("批量导出所有技能", GUILayout.Height(40)))
            {
                ExportAllSkills();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 导入选项
            EditorGUILayout.LabelField("导入工具", EditorStyles.boldLabel);

            if (GUILayout.Button("从JSON导入技能", GUILayout.Height(30)))
            {
                string path = EditorUtility.OpenFilePanel("选择技能JSON文件", "Assets", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    ImportSkillFromJson(path);
                }
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        #region 技能操作方法
        private void CreateNewSkill(string skillName = null)
        {
            string name = string.IsNullOrEmpty(skillName) ? "NewSkill" : skillName;

            // 创建技能配置
            var newSkill = CreateInstance<SkillConfig>();
            newSkill.skillId = $"SKILL_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newSkill.skillName = name;

            // 保存配置资产
            string configPath = $"{SKILL_DATA_FOLDER}/{name}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(newSkill, configPath);

            // 创建Timeline
            CreateTimelineForSkill(newSkill);

            // 添加到数据库
            skillDatabase.skills.Add(newSkill);
            EditorUtility.SetDirty(skillDatabase);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(newSkill);
        }

        private void CreateTimelineForSkill(SkillConfig skill)
        {
            string timelinePath = $"{TIMELINE_FOLDER}/{skill.skillName}.playable";
            timelinePath = AssetDatabase.GenerateUniqueAssetPath(timelinePath);

            var timeline = CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, timelinePath);

            // 添加默认轨道
            AddDefaultTracks(timeline);

            skill.timelineAsset = timeline;
            EditorUtility.SetDirty(skill);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadTimelineForSkill(skill);
        }

        private void AddDefaultTracks(TimelineAsset timeline)
        {
            // 添加动画轨道
            var animTrack = timeline.CreateTrack<AnimationTrack>(null, "AnimationTrack");

            // 添加特效轨道
            var effectTrack = timeline.CreateTrack<EffectTrack>(null, "EffectTrack");

            // 添加音频轨道
            var audioTrack = timeline.CreateTrack<AudioTrack>(null, "AudioTrack");

            // 添加触发器轨道
            var triggerTrack = timeline.CreateTrack<TriggerTrack>(null, "TriggerTrack");

            EditorUtility.SetDirty(timeline);
        }

        private void SelectSkill(SkillConfig skill)
        {
            selectedSkill = skill;
            LoadTimelineForSkill(skill);

            // 刷新编辑器
            timelineEditor = null;
            skillConfigEditor = null;

            Repaint();
        }

        private void LoadTimelineForSkill(SkillConfig skill)
        {
            selectedTimeline = skill.timelineAsset;
        }

        private void SaveCurrentSkill()
        {
            if (selectedSkill != null)
            {
                EditorUtility.SetDirty(selectedSkill);
                if (selectedTimeline != null)
                {
                    EditorUtility.SetDirty(selectedTimeline);
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"技能 '{selectedSkill.skillName}' 已保存");
            }
        }

        private void DuplicateSkill(SkillConfig source)
        {
            var newSkill = Instantiate(source);
            newSkill.skillId = $"SKILL_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newSkill.skillName = source.skillName + "_Copy";

            string configPath = $"{SKILL_DATA_FOLDER}/{newSkill.skillName}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(newSkill, configPath);

            // 复制Timeline
            if (source.timelineAsset != null)
            {
                string timelinePath = $"{TIMELINE_FOLDER}/{newSkill.skillName}.playable";
                timelinePath = AssetDatabase.GenerateUniqueAssetPath(timelinePath);
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.timelineAsset), timelinePath);
                newSkill.timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            }

            skillDatabase.skills.Add(newSkill);
            EditorUtility.SetDirty(skillDatabase);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(newSkill);
        }

        private void DeleteSkill(SkillConfig skill)
        {
            skillDatabase.skills.Remove(skill);

            // 删除资产文件
            string configPath = AssetDatabase.GetAssetPath(skill);
            if (!string.IsNullOrEmpty(configPath))
            {
                AssetDatabase.DeleteAsset(configPath);
            }

            if (skill.timelineAsset != null)
            {
                string timelinePath = AssetDatabase.GetAssetPath(skill.timelineAsset);
                if (!string.IsNullOrEmpty(timelinePath))
                {
                    AssetDatabase.DeleteAsset(timelinePath);
                }
            }

            EditorUtility.SetDirty(skillDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (selectedSkill == skill)
            {
                selectedSkill = null;
                selectedTimeline = null;
            }

            Repaint();
        }
        #endregion

        #region 导出导入方法
        private void ExportSkill(SkillConfig skill)
        {
            string path = EditorUtility.SaveFilePanel("导出技能", "Assets", skill.skillName, "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(skill, true);
                File.WriteAllText(path, json);
                Debug.Log($"技能 '{skill.skillName}' 已导出到: {path}");
            }
        }

        private void ExportAllSkills()
        {
            string folderPath = EditorUtility.SaveFolderPanel("选择导出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                foreach (var skill in skillDatabase.skills)
                {
                    string json = JsonUtility.ToJson(skill, true);
                    string filePath = Path.Combine(folderPath, $"{skill.skillName}.json");
                    File.WriteAllText(filePath, json);
                }
                Debug.Log($"已导出 {skillDatabase.skills.Count} 个技能到: {folderPath}");
            }
        }

        private void ImportSkillFromJson(string path)
        {
            string json = File.ReadAllText(path);
            var skill = CreateInstance<SkillConfig>();
            JsonUtility.FromJsonOverwrite(json, skill);

            // 保存资产
            string configPath = $"{SKILL_DATA_FOLDER}/{skill.skillName}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(skill, configPath);

            skillDatabase.skills.Add(skill);
            EditorUtility.SetDirty(skillDatabase);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(skill);
            Debug.Log($"技能 '{skill.skillName}' 已导入");
        }
        #endregion
    }
}