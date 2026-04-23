using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem.Editor
{
    /// <summary>
    /// 技能编辑器主窗口
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        #region 常量定义
        private const string                                WINDOW_TITLE = "技能编辑器";
        private const string                                TIMELINE_FOLDER = "Assets/SkillSystem/Timelines";
        private const string                                SKILL_DATA_FOLDER = "Assets/SkillSystem/Data";
        private const string                                EDITOR_PREFS_LAST_SKILL = "SkillEditor_LastEditedSkill";
        #endregion

        #region UI状态
        private Vector2                                     skill_list_scroll_pos_;
        private Vector2                                     inspector_scroll_pos_;
        private string                                      new_skill_name_ = "NewSkill";
        private string                                      search_filter_ = "";
        private int                                         selected_tab_ = 0;
        private readonly string[]                           tabs_ = { "技能库", "全局设置", "导出工具" };
        #endregion

        #region 技能数据
        private SkillDatabase                               skill_database_;
        private SkillConfig                                 selected_skill_;
        private TimelineAsset                               selected_timeline_;
        private PlayableDirector                            preview_director_;
        private GameObject                                  preview_instance_;
        private bool                                        is_preview_mode_ = false;
        #endregion

        #region 编辑器实例
        private UnityEditor.Editor                          timeline_editor_;
        private UnityEditor.Editor                          skill_config_editor_;
        #endregion

        [MenuItem("Skill System/技能编辑器 %#S", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("AnimationWindow").image);
            window.minSize = new Vector2(800, 400);
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
            if (selected_skill_ != null)
            {
                EditorPrefs.SetString(EDITOR_PREFS_LAST_SKILL, selected_skill_.skill_id_);
            }
        }

        /// <summary>
        /// 编辑器状态变更, 用于清理预览
        /// </summary>
        /// <param name="state"></param>
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CleanupPreview();
            }
        }

        /// <summary>
        /// 加载技能数据库, 并恢复上次编辑的技能
        /// </summary>
        private void LoadDatabase()
        {
            // 查找或创建技能数据库
            string db_path = $"{SKILL_DATA_FOLDER}/SkillDatabase.asset";
            skill_database_ = AssetDatabase.LoadAssetAtPath<SkillDatabase>(db_path);

            if (skill_database_ == null)
            {
                skill_database_ = CreateInstance<SkillDatabase>();
                AssetDatabase.CreateAsset(skill_database_, db_path);
                AssetDatabase.SaveAssets();
            }

            // 恢复上次编辑的技能
            string last_skill_id = EditorPrefs.GetString(EDITOR_PREFS_LAST_SKILL, "");
            if (string.IsNullOrEmpty(last_skill_id))
            {
                return;
            }

            selected_skill_ = skill_database_.skills.Find(s => s.skill_id_ == last_skill_id);
            if (selected_skill_ != null)
            {
                LoadTimelineForSkill(selected_skill_);
            }
        }

        /// <summary>
        /// 创建技能文件夹
        /// </summary>
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

            selected_tab_ = GUILayout.Toolbar(selected_tab_, tabs_, GUILayout.Height(30));

            switch (selected_tab_)
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
        /// <summary>
        /// 绘制工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 新建技能按钮
            if (GUILayout.Button("新建技能", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewSkill();
            }

            // 保存按钮
            GUI.enabled = selected_skill_ != null;
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveCurrentSkill();
            }
            GUI.enabled = true;

            // 预览模式开关
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = is_preview_mode_ ? Color.green : Color.gray;
            if (GUILayout.Button(is_preview_mode_ ? "■ 停止预览" : "▶ 预览", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TogglePreview();
            }
            GUI.backgroundColor = originalColor;

            GUILayout.FlexibleSpace();

            // 搜索框
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(35));
            search_filter_ = EditorGUILayout.TextField(search_filter_, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 切换预览模式
        /// </summary>
        private void TogglePreview()
        {
            if (is_preview_mode_)
            {
                StopPreview();
            }
            else
            {
                StartPreview();
            }
        }

        /// <summary>
        /// 开始预览
        /// </summary>
        private void StartPreview()
        {
            if (selected_timeline_ == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个技能Timeline", "确定");
                return;
            }

            // 在场景中创建预览角色
            CleanupPreview();

            preview_instance_ = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            preview_instance_.name = "[Preview] Skill Caster";
            preview_instance_.transform.position = Vector3.zero;

            var animator = preview_instance_.AddComponent<Animator>();
            var skill_player = preview_instance_.AddComponent<SkillPlayer>();

            // 设置Timeline
            preview_director_ = preview_instance_.GetComponent<PlayableDirector>();
            if (preview_director_ == null)
                preview_director_ = preview_instance_.AddComponent<PlayableDirector>();

            preview_director_.playableAsset = selected_timeline_;

            // 绑定轨道
            var timeline = selected_timeline_ as TimelineAsset;
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track.name.Contains("AnimationTrack"))
                    preview_director_.SetGenericBinding(track, animator);
                else if (track.name.Contains("EffectTrack") || track.name.Contains("AudioTrack") || track is AttackDetectTrack)
                    preview_director_.SetGenericBinding(track, skill_player);
            }

            // 定位Timeline窗口
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");

            // 选中预览对象
            Selection.activeGameObject = preview_instance_;

            // 开始播放
            preview_director_.Play();
            is_preview_mode_ = true;
        }

        /// <summary>
        /// 停止预览
        /// </summary>
        private void StopPreview()
        {
            if (preview_director_ != null)
            {
                preview_director_.Stop();
            }
            is_preview_mode_ = false;
        }

        /// <summary>
        /// 清理预览
        /// </summary>
        private void CleanupPreview()
        {
            StopPreview();

            if (preview_instance_ != null)
            {
                DestroyImmediate(preview_instance_);
                preview_instance_ = null;
            }

            preview_director_ = null;
        }
        #endregion

        #region 技能库标签页
        /// <summary>
        /// 绘制技能库标签页
        /// </summary>
        private void DrawSkillLibraryTab()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧：技能列表（固定宽度）
            float left_width = 200f;
            EditorGUILayout.BeginVertical(GUILayout.Width(left_width));
            DrawSkillList();
            EditorGUILayout.EndVertical();

            // 第一条竖直分割线
            DrawVerticalSeparator();

            // 右侧：Timeline编辑器区域 以及 技能配置面板
            EditorGUILayout.BeginVertical();
            DrawTimelineArea();
            DrawSkillConfigPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制竖直分割线
        /// </summary>
        private void DrawVerticalSeparator()
        {
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));
        }

        /// <summary>
        /// 绘制技能列表区域
        /// </summary>
        private void DrawSkillList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            EditorGUILayout.LabelField("技能列表", EditorStyles.boldLabel);

            // 绘制技能列表
            skill_list_scroll_pos_ = EditorGUILayout.BeginScrollView(skill_list_scroll_pos_);
            if (skill_database_ != null)
            {
                var filtered_skills = skill_database_.skills
                    .Where(s => string.IsNullOrEmpty(search_filter_) ||
                           s.skill_name_.Contains(search_filter_) ||
                           s.skill_id_.Contains(search_filter_))
                    .ToList();

                foreach (var skill in filtered_skills)
                {
                    DrawSkillListItem(skill);
                }
            }
            EditorGUILayout.EndScrollView();



            // 绘制新建技能快速入口
            EditorGUILayout.BeginHorizontal();
            new_skill_name_ = EditorGUILayout.TextField(new_skill_name_);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                CreateNewSkill(new_skill_name_);
                new_skill_name_ = "NewSkill";
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制技能列表项
        /// </summary>
        /// <param name="skill"></param>
        private void DrawSkillListItem(SkillConfig skill)
        {
            bool is_selected = selected_skill_ == skill;

            Color bg_color = GUI.backgroundColor;
            if (is_selected)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 技能图标
            if (skill.icon_ != null)
                GUILayout.Label(AssetPreview.GetAssetPreview(skill.icon_), GUILayout.Width(32), GUILayout.Height(32));
            else
                GUILayout.Label(EditorGUIUtility.IconContent("AnimationClip Icon").image, GUILayout.Width(32), GUILayout.Height(32));

            // 技能信息
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(skill.skill_name_, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {skill.skill_id_}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // 点击选中
            Rect item_rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && item_rect.Contains(Event.current.mousePosition))
            {
                SelectSkill(skill);
                Event.current.Use();
            }

            GUI.backgroundColor = bg_color;
        }

        /// <summary>
        /// 绘制Timeline区域
        /// </summary>
        private void DrawTimelineArea()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("技能编辑器", EditorStyles.boldLabel);

            if (selected_skill_ != null && GUILayout.Button("定位Timeline", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (selected_timeline_ != null)
                {
                    EditorGUIUtility.PingObject(selected_timeline_);
                    Selection.activeObject = selected_timeline_;
                }
            }

            if (GUILayout.Button("打开Timeline窗口", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            }
            EditorGUILayout.EndHorizontal();

            // Timeline预览区域
            if (selected_timeline_ != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Timeline 配置", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"当前Timeline: {selected_timeline_.name}");

                if (timeline_editor_ == null || timeline_editor_.target != selected_timeline_)
                {
                    timeline_editor_ = UnityEditor.Editor.CreateEditor(selected_timeline_);
                }

                if (timeline_editor_ != null)
                {
                    timeline_editor_.OnInspectorGUI();
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("选择或创建一个技能以编辑Timeline", MessageType.Info);

                if (selected_skill_ != null && GUILayout.Button("创建新Timeline", GUILayout.Height(40)))
                {
                    CreateTimelineForSkill(selected_skill_);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制技能配置面板
        /// </summary>
        private void DrawSkillConfigPanel()
        {
            if (selected_skill_ == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("技能属性配置", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // 折叠按钮
            EditorGUILayout.EndHorizontal();

            inspector_scroll_pos_ = EditorGUILayout.BeginScrollView(inspector_scroll_pos_);

            if (skill_config_editor_ == null || skill_config_editor_.target != selected_skill_)
            {
                skill_config_editor_ = UnityEditor.Editor.CreateEditor(selected_skill_);
            }

            if (skill_config_editor_ != null)
            {
                skill_config_editor_.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("复制技能"))
            {
                DuplicateSkill(selected_skill_);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("删除技能"))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除技能 '{selected_skill_.skill_name_}' 吗？", "删除", "取消"))
                {
                    DeleteSkill(selected_skill_);
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
                if (selected_skill_ != null)
                {
                    ExportSkill(selected_skill_);
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
            newSkill.skill_id_ = $"SKILL_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newSkill.skill_name_ = name;

            // 保存配置资产
            string configPath = $"{SKILL_DATA_FOLDER}/{name}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(newSkill, configPath);

            // 创建Timeline
            CreateTimelineForSkill(newSkill);

            // 添加到数据库
            skill_database_.skills.Add(newSkill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(newSkill);
        }

        private void CreateTimelineForSkill(SkillConfig skill)
        {
            string timelinePath = $"{TIMELINE_FOLDER}/{skill.skill_name_}.playable";
            timelinePath = AssetDatabase.GenerateUniqueAssetPath(timelinePath);

            var timeline = CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, timelinePath);

            // 添加默认轨道
            AddDefaultTracks(timeline);

            skill.timeline_asset_ = timeline;
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
            var triggerTrack = timeline.CreateTrack<AttackDetectTrack>(null, "TriggerTrack");

            EditorUtility.SetDirty(timeline);
        }

        private void SelectSkill(SkillConfig skill)
        {
            selected_skill_ = skill;
            LoadTimelineForSkill(skill);

            // 刷新编辑器
            timeline_editor_ = null;
            skill_config_editor_ = null;

            Repaint();
        }

        private void LoadTimelineForSkill(SkillConfig skill)
        {
            selected_timeline_ = skill.timeline_asset_;
        }

        private void SaveCurrentSkill()
        {
            if (selected_skill_ != null)
            {
                EditorUtility.SetDirty(selected_skill_);
                if (selected_timeline_ != null)
                {
                    EditorUtility.SetDirty(selected_timeline_);
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"技能 '{selected_skill_.skill_name_}' 已保存");
            }
        }

        private void DuplicateSkill(SkillConfig source)
        {
            var newSkill = Instantiate(source);
            newSkill.skill_id_ = $"SKILL_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            newSkill.skill_name_ = source.skill_name_ + "_Copy";

            string configPath = $"{SKILL_DATA_FOLDER}/{newSkill.skill_name_}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(newSkill, configPath);

            // 复制Timeline
            if (source.timeline_asset_ != null)
            {
                string timelinePath = $"{TIMELINE_FOLDER}/{newSkill.skill_name_}.playable";
                timelinePath = AssetDatabase.GenerateUniqueAssetPath(timelinePath);
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.timeline_asset_), timelinePath);
                newSkill.timeline_asset_ = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            }

            skill_database_.skills.Add(newSkill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(newSkill);
        }

        private void DeleteSkill(SkillConfig skill)
        {
            skill_database_.skills.Remove(skill);

            // 删除资产文件
            string configPath = AssetDatabase.GetAssetPath(skill);
            if (!string.IsNullOrEmpty(configPath))
            {
                AssetDatabase.DeleteAsset(configPath);
            }

            if (skill.timeline_asset_ != null)
            {
                string timelinePath = AssetDatabase.GetAssetPath(skill.timeline_asset_);
                if (!string.IsNullOrEmpty(timelinePath))
                {
                    AssetDatabase.DeleteAsset(timelinePath);
                }
            }

            EditorUtility.SetDirty(skill_database_);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (selected_skill_ == skill)
            {
                selected_skill_ = null;
                selected_timeline_ = null;
            }

            Repaint();
        }
        #endregion

        #region 导出导入方法
        private void ExportSkill(SkillConfig skill)
        {
            string path = EditorUtility.SaveFilePanel("导出技能", "Assets", skill.skill_name_, "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(skill, true);
                File.WriteAllText(path, json);
                Debug.Log($"技能 '{skill.skill_name_}' 已导出到: {path}");
            }
        }

        private void ExportAllSkills()
        {
            string folderPath = EditorUtility.SaveFolderPanel("选择导出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                foreach (var skill in skill_database_.skills)
                {
                    string json = JsonUtility.ToJson(skill, true);
                    string filePath = Path.Combine(folderPath, $"{skill.skill_name_}.json");
                    File.WriteAllText(filePath, json);
                }
                Debug.Log($"已导出 {skill_database_.skills.Count} 个技能到: {folderPath}");
            }
        }

        private void ImportSkillFromJson(string path)
        {
            string json = File.ReadAllText(path);
            var skill = CreateInstance<SkillConfig>();
            JsonUtility.FromJsonOverwrite(json, skill);

            // 保存资产
            string configPath = $"{SKILL_DATA_FOLDER}/{skill.skill_name_}_Config.asset";
            configPath = AssetDatabase.GenerateUniqueAssetPath(configPath);
            AssetDatabase.CreateAsset(skill, configPath);

            skill_database_.skills.Add(skill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(skill);
            Debug.Log($"技能 '{skill.skill_name_}' 已导入");
        }
        #endregion
    }
}