using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SkillSystem
{
    /// <summary>
    /// 技能编辑器主窗口
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        #region 编辑器配置
        private const string                                WINDOW_TITLE = "技能编辑器";
        private string                                      TIMELINE_FOLDER = "Assets/SkillSystem/Timelines";
        private string                                      SKILL_DATA_FOLDER = "Assets/SkillSystem/SkillData";
        private string                                      EDITOR_PREFS_LAST_SKILL = "SkillEditor_LastEditedSkill";

        private string                                      DEFAULT_ANIM_TRACK_NAME = "AnimationTrack";
        private string                                      DEFAULT_AUDIO_TRACK_NAME = "AudioTrack";
        private string                                      DEFAULT_EFFECT_TRACK_NAME = "EffectTrack";
        private string                                      DEFAULT_DETECT_TRACK_NAME = "DetectTrack";
        private string                                      DEFAULT_CURVE_TRACK_NAME = "CurveTrack";

        private string                                      ASSET_EXPORT_PATH = "Assets/SkillSystem/StreamingAssets/SkillJsonData";
        private int                                         EXPORT_FORMAT_INDEX = 0;
        private int                                         IMPORT_FORMAT_INDEX = 0;
        #endregion

        #region UI状态
        private Vector2                                     skill_list_scroll_pos_;
        private Vector2                                     inspector_scroll_pos_;
        private string                                      new_skill_name_ = "NewSkill";
        private string                                      search_filter_ = "";
        private int                                         selected_tab_ = 0;
        private readonly string[]                           tabs_ = { "技能库", "全局设置", "导出工具" };
        private readonly string[]                           export_formats_ = { "Json", "Binary" };
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
        private Editor                                      timeline_editor_;
        private Editor                                      skill_config_editor_;
        #endregion

        [MenuItem("Skill System/Skill Editor", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillEditorWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE, EditorGUIUtility.IconContent("AnimationWindow").image);
            window.minSize = new Vector2(800, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadEditorConfig();
            LoadDatabase();
            EnsureFoldersExist();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            CleanupPreview();
            SaveEditorConfig();
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
            string db_path = $"{SKILL_DATA_FOLDER}/SkillDatabase.asset";
            skill_database_ = AssetDatabase.LoadAssetAtPath<SkillDatabase>(db_path);

            if (skill_database_ == null)
            {
                skill_database_ = CreateInstance<SkillDatabase>();
                AssetDatabase.CreateAsset(skill_database_, db_path);
                AssetDatabase.SaveAssets();
            }

            string last_skill_id = EditorPrefs.GetString(EDITOR_PREFS_LAST_SKILL, "");
            if (string.IsNullOrEmpty(last_skill_id))
            {
                return;
            }

            selected_skill_ = skill_database_.skills_.Find(s => s.skill_id_ == last_skill_id);
            if (selected_skill_ != null)
            {
                LoadTimelineForSkill(selected_skill_);
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

        private void StartPreview()
        {
            if (selected_timeline_ == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个技能Timeline", "确定");
                return;
            }

            CleanupPreview();

            preview_instance_ = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            preview_instance_.name = "[Preview] Skill Caster";
            preview_instance_.transform.position = Vector3.zero;

            var animator = preview_instance_.AddComponent<Animator>();
            var skill_player = preview_instance_.AddComponent<SkillPlayer>();

            preview_director_ = preview_instance_.GetComponent<PlayableDirector>();
            if (preview_director_ == null)
                preview_director_ = preview_instance_.AddComponent<PlayableDirector>();

            preview_director_.playableAsset = selected_timeline_;

            var timeline = selected_timeline_ as TimelineAsset;
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track.name.Contains("AnimationTrack"))
                    preview_director_.SetGenericBinding(track, animator);
                else if (track is AnimationTrack)
                    preview_director_.SetGenericBinding(track, preview_instance_); // ChargeTrack binding to GameObject
                else if (track.name.Contains("EffectTrack") || track.name.Contains("AudioTrack") || track is AttackDetectTrack)
                    preview_director_.SetGenericBinding(track, skill_player);
            }

            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");

            Selection.activeGameObject = preview_instance_;

            preview_director_.Play();
            is_preview_mode_ = true;
        }

        private void StopPreview()
        {
            if (preview_director_ != null)
            {
                preview_director_.Stop();
            }
            is_preview_mode_ = false;
        }

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
        private void DrawSkillLibraryTab()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            float left_width = 200f;
            EditorGUILayout.BeginVertical(GUILayout.Width(left_width));
            DrawSkillList();
            EditorGUILayout.EndVertical();

            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            EditorGUILayout.BeginVertical();
            DrawTimelineArea();
            DrawSkillConfigPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.enabled = selected_skill_ != null;
            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveCurrentSkill();
            }
            GUI.enabled = true;

            if (GUILayout.Button("新建技能", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewSkill();
            }

            Color original_color = GUI.backgroundColor;
            GUI.backgroundColor = is_preview_mode_ ? Color.green : Color.gray;
            if (GUILayout.Button(is_preview_mode_ ? "■ 停止预览" : "▶ 预览", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TogglePreview();
            }
            GUI.backgroundColor = original_color;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSkillList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("技能列表", EditorStyles.boldLabel, GUILayout.Width(90));
            search_filter_ = EditorGUILayout.TextField(search_filter_, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            skill_list_scroll_pos_ = EditorGUILayout.BeginScrollView(skill_list_scroll_pos_);
            if (skill_database_ != null)
            {
                var filtered_skills = skill_database_.skills_
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

        private void DrawSkillListItem(SkillConfig skill)
        {
            bool is_selected = selected_skill_ == skill;

            Color bg_color = GUI.backgroundColor;
            if (is_selected)
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (skill.icon_ != null)
                GUILayout.Label(AssetPreview.GetAssetPreview(skill.icon_), GUILayout.Width(32), GUILayout.Height(32));
            else
                GUILayout.Label(EditorGUIUtility.IconContent("AnimationClip Icon").image, GUILayout.Width(32), GUILayout.Height(32));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(skill.skill_name_, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {skill.skill_id_}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            Rect item_rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && item_rect.Contains(Event.current.mousePosition))
            {
                SelectSkill(skill);
                Event.current.Use();
            }

            GUI.backgroundColor = bg_color;
        }

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

        private void DrawSkillConfigPanel()
        {
            if (selected_skill_ == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("技能属性配置", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            inspector_scroll_pos_ = EditorGUILayout.BeginScrollView(inspector_scroll_pos_);

            if (skill_config_editor_ == null || skill_config_editor_.target != selected_skill_)
            {
                skill_config_editor_ = Editor.CreateEditor(selected_skill_);
            }

            if (skill_config_editor_ != null)
            {
                skill_config_editor_.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();

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

            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Timeline保存路径:", GUILayout.Width(120));
            EditorGUILayout.TextField(TIMELINE_FOLDER);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择Timeline保存路径", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    TIMELINE_FOLDER = PathUtility.ToAssetsRelativePath(path);
                    SaveEditorConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("数据保存路径:", GUILayout.Width(120));
            EditorGUILayout.TextField(SKILL_DATA_FOLDER);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择数据保存路径", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    SKILL_DATA_FOLDER = PathUtility.ToAssetsRelativePath(path);
                    SaveEditorConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("默认轨道配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认动画轨道名称:", GUILayout.Width(150));
            DEFAULT_ANIM_TRACK_NAME = EditorGUILayout.TextField(DEFAULT_ANIM_TRACK_NAME);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认特效轨道名称:", GUILayout.Width(150));
            DEFAULT_EFFECT_TRACK_NAME = EditorGUILayout.TextField(DEFAULT_EFFECT_TRACK_NAME);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认音频轨道名称:", GUILayout.Width(150));
            DEFAULT_AUDIO_TRACK_NAME = EditorGUILayout.TextField(DEFAULT_AUDIO_TRACK_NAME);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("默认检测轨道名称:", GUILayout.Width(150));
            DEFAULT_DETECT_TRACK_NAME = EditorGUILayout.TextField(DEFAULT_DETECT_TRACK_NAME);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        #endregion

        private void DrawExportToolsTab()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("导出工具", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("导出配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出格式:", GUILayout.Width(80));
            EXPORT_FORMAT_INDEX = EditorGUILayout.Popup(EXPORT_FORMAT_INDEX, export_formats_);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出路径:", GUILayout.Width(80));
            EditorGUILayout.TextField(ASSET_EXPORT_PATH);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择导出路径", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    ASSET_EXPORT_PATH = PathUtility.ToAssetsRelativePath(path);
                    SaveEditorConfig();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("导出当前技能", GUILayout.Height(40)))
            {
                if (selected_skill_ != null)
                {
                    if (EXPORT_FORMAT_INDEX == 0)
                    {
                        ExportSkillToJson(selected_skill_);
                    }
                    else if (EXPORT_FORMAT_INDEX == 1)
                    {
                        ExportSkillToBinary(selected_skill_);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先选择一个技能", "确定");
                }
            }

            if (GUILayout.Button("批量导出所有技能", GUILayout.Height(40)))
            {
                if (EXPORT_FORMAT_INDEX == 0)
                {
                    ExportAllSkillsToJson();
                }
                else if (EXPORT_FORMAT_INDEX == 1)
                {
                    ExportAllSkillsToBinary();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("导入工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导入格式:", GUILayout.Width(80));
            IMPORT_FORMAT_INDEX = EditorGUILayout.Popup(IMPORT_FORMAT_INDEX, export_formats_);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("导入技能", GUILayout.Height(30)))
            {
                if (IMPORT_FORMAT_INDEX == 0)
                {
                    string path = EditorUtility.OpenFilePanel("选择 Json 文件", ASSET_EXPORT_PATH, "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        ImportSkillFromJson(path);
                    }
                }
                else if (IMPORT_FORMAT_INDEX == 1)
                {
                    string path = EditorUtility.OpenFilePanel("选择技能 Binary 文件", ASSET_EXPORT_PATH, "skill");
                    if (!string.IsNullOrEmpty(path))
                    {
                        ImportSkillFromBinary(path);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }


        #region 技能操作方法
        private void CreateNewSkill(string skill_name = null)
        {
            string name = string.IsNullOrEmpty(skill_name) ? "NewSkill" : skill_name;

            var new_skill = CreateInstance<SkillConfig>();
            new_skill.skill_id_ = $"SKILL_{System.Guid.NewGuid().ToString()[..8]}";
            new_skill.skill_name_ = name;

            string config_path = $"{SKILL_DATA_FOLDER}/{name}_Config.asset";
            config_path = AssetDatabase.GenerateUniqueAssetPath(config_path);
            AssetDatabase.CreateAsset(new_skill, config_path);

            CreateTimelineForSkill(new_skill);

            skill_database_.skills_.Add(new_skill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(new_skill);
        }

        private void CreateTimelineForSkill(SkillConfig skill)
        {
            string timeline_path = $"{TIMELINE_FOLDER}/{skill.skill_name_}.playable";
            timeline_path = AssetDatabase.GenerateUniqueAssetPath(timeline_path);

            var timeline = CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, timeline_path);

            AddDefaultTracks(timeline);

            skill.timeline_asset_ = timeline;
            EditorUtility.SetDirty(skill);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadTimelineForSkill(skill);
        }

        private void AddDefaultTracks(TimelineAsset timeline)
        {
            timeline.CreateTrack<AnimationTrack>(null, DEFAULT_ANIM_TRACK_NAME);
            timeline.CreateTrack<EffectTrack>(null, DEFAULT_EFFECT_TRACK_NAME);
            timeline.CreateTrack<AudioTrack>(null, DEFAULT_AUDIO_TRACK_NAME);
            timeline.CreateTrack<AttackDetectTrack>(null, DEFAULT_DETECT_TRACK_NAME);
            timeline.CreateTrack<CurveTrack>(null, DEFAULT_CURVE_TRACK_NAME);

            EditorUtility.SetDirty(timeline);
        }

        private void SelectSkill(SkillConfig skill)
        {
            selected_skill_ = skill;
            LoadTimelineForSkill(skill);
            ReleaseEditors();
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
            var new_skill = Instantiate(source);
            new_skill.skill_id_ = $"SKILL_{System.Guid.NewGuid().ToString()[..8]}";
            new_skill.skill_name_ = source.skill_name_ + "_Copy";

            string config_path = $"{SKILL_DATA_FOLDER}/{new_skill.skill_name_}_Config.asset";
            config_path = AssetDatabase.GenerateUniqueAssetPath(config_path);
            AssetDatabase.CreateAsset(new_skill, config_path);

            if (source.timeline_asset_ != null)
            {
                string timeline_path = $"{TIMELINE_FOLDER}/{new_skill.skill_name_}.playable";
                timeline_path = AssetDatabase.GenerateUniqueAssetPath(timeline_path);
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.timeline_asset_), timeline_path);
                new_skill.timeline_asset_ = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timeline_path);
            }

            skill_database_.skills_.Add(new_skill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(new_skill);
        }

        private void DeleteSkill(SkillConfig skill)
        {
            bool should_delete_timeline = false;

            TimelineAsset timeline_to_delete = skill.timeline_asset_;
            string timeline_path = null;
            if (timeline_to_delete != null)
            {
                timeline_path = AssetDatabase.GetAssetPath(timeline_to_delete);

                should_delete_timeline = EditorUtility.DisplayDialog(
                    "删除 Timeline 文件",
                    $"是否同时删除关联的 Timeline 文件?\n{timeline_to_delete.name}",
                    "删除",
                    "保留"
                );
            }

            if (selected_skill_ == skill)
            {
                selected_skill_ = null;
                selected_timeline_ = null;
            }

            skill_database_.skills_.Remove(skill);
            EditorUtility.SetDirty(skill_database_);

            string config_path = AssetDatabase.GetAssetPath(skill);
            if (!string.IsNullOrEmpty(config_path))
            {
                AssetDatabase.DeleteAsset(config_path);
            }

            if (should_delete_timeline && !string.IsNullOrEmpty(timeline_path))
            {
                AssetDatabase.DeleteAsset(timeline_path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ReleaseEditors();
            Repaint();
        }

        private void ReleaseEditors()
        {
            if (timeline_editor_ != null)
            {
                DestroyImmediate(timeline_editor_);
                timeline_editor_ = null;
            }

            if (skill_config_editor_ != null)
            {
                DestroyImmediate(skill_config_editor_);
                skill_config_editor_ = null;
            }
        }
        #endregion

        #region 导出导入方法
        private void ExportSkillToJson(SkillConfig skill)
        {
            if (string.IsNullOrEmpty(ASSET_EXPORT_PATH))
            {
                Debug.LogError("请先设置资源导出路径");
                return;
            }

            string json = SkillConfigFormatter.ToJson(skill);
            File.WriteAllText($"{ASSET_EXPORT_PATH}/{skill.skill_name_}.json", json);

            AssetDatabase.Refresh();
            Debug.Log($"技能 '{skill.skill_name_}' 已导出到: {ASSET_EXPORT_PATH}");
            EditorUtility.DisplayDialog("成功", $"已导出到: {ASSET_EXPORT_PATH}", "确定");
        }

        private void ExportAllSkillsToJson()
        {
            string path = EditorUtility.SaveFolderPanel("选择导出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var skill in skill_database_.skills_)
                {
                    string json = SkillConfigFormatter.ToJson(skill);
                    string file_path = Path.Combine(path, $"{skill.skill_name_}.json");
                    File.WriteAllText(file_path, json);
                }

                AssetDatabase.Refresh();
                Debug.Log($"已导出 {skill_database_.skills_.Count} 个技能到: {path}");
                EditorUtility.DisplayDialog("成功", $"已导出到: {path}", "确定");
            }
        }

        private void ImportSkillFromJson(string path)
        {
            string json = File.ReadAllText(path);
            var skill = CreateInstance<SkillConfig>();
            SkillConfigFormatter.FromJson(json, skill);

            string config_path = $"{SKILL_DATA_FOLDER}/{skill.skill_name_}_Config.asset";
            config_path = AssetDatabase.GenerateUniqueAssetPath(config_path);
            AssetDatabase.CreateAsset(skill, config_path);

            skill_database_.skills_.Add(skill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(skill);
            Debug.Log($"技能 '{skill.skill_name_}' 已导入");
            EditorUtility.DisplayDialog("成功", $"技能 {skill.skill_name_} 已导入", "确定");
        }
        private void ExportSkillToBinary(SkillConfig skill)
        {
            if (string.IsNullOrEmpty(ASSET_EXPORT_PATH))
            {
                Debug.LogError("请先设置资源导出路径");
                return;
            }

            string path = $"{ASSET_EXPORT_PATH}/{skill.skill_name_}.skill";
            SkillConfigFormatter.ToBinary(skill, path);

            AssetDatabase.Refresh();
            Debug.Log($"已导出 {skill_database_.skills_.Count} 个技能到: {path}");
            EditorUtility.DisplayDialog("成功", $"已导出到: {path}", "确定");
        }
        private void ExportAllSkillsToBinary()
        {
            string path = EditorUtility.SaveFolderPanel("选择导出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var skill in skill_database_.skills_)
                {
                    string file_path = Path.Combine(path, $"{skill.skill_name_}.json");
                    SkillConfigFormatter.ToBinary(skill, file_path);
                }

                AssetDatabase.Refresh();
                Debug.Log($"已导出 {skill_database_.skills_.Count} 个技能到: {path}");
                EditorUtility.DisplayDialog("成功", $"已导出到: {path}", "确定");
            }
        }
        private void ImportSkillFromBinary(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("文件不存在");
                return;
            }
            SkillConfig skill = CreateInstance<SkillConfig>();
            SkillConfigFormatter.FromBinary(path, skill);

            string config_path = $"{SKILL_DATA_FOLDER}/{skill.skill_name_}_Config.asset";
            config_path = AssetDatabase.GenerateUniqueAssetPath(config_path);
            AssetDatabase.CreateAsset(skill, config_path);

            skill_database_.skills_.Add(skill);
            EditorUtility.SetDirty(skill_database_);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SelectSkill(skill);
            Debug.Log($"技能 '{skill.skill_name_}' 已导入");
            EditorUtility.DisplayDialog("成功", $"技能 {skill.skill_name_} 已导入", "确定");
        }
        #endregion

        #region 编辑器方法
        private void SaveEditorConfig()
        {
            EditorPrefs.SetString(nameof(EDITOR_PREFS_LAST_SKILL), selected_skill_?.skill_id_ ?? "");
            EditorPrefs.SetString(nameof(SKILL_DATA_FOLDER), SKILL_DATA_FOLDER);
            EditorPrefs.SetString(nameof(TIMELINE_FOLDER), TIMELINE_FOLDER);
            EditorPrefs.SetString(nameof(ASSET_EXPORT_PATH), ASSET_EXPORT_PATH);
            EditorPrefs.SetString(nameof(DEFAULT_ANIM_TRACK_NAME), DEFAULT_ANIM_TRACK_NAME);
            EditorPrefs.SetString(nameof(DEFAULT_AUDIO_TRACK_NAME), DEFAULT_AUDIO_TRACK_NAME);
            EditorPrefs.SetString(nameof(DEFAULT_EFFECT_TRACK_NAME), DEFAULT_EFFECT_TRACK_NAME);
            EditorPrefs.SetString(nameof(DEFAULT_DETECT_TRACK_NAME), DEFAULT_DETECT_TRACK_NAME);
            EditorPrefs.SetString(nameof(DEFAULT_CURVE_TRACK_NAME), DEFAULT_CURVE_TRACK_NAME);
            EditorPrefs.SetInt(nameof(EXPORT_FORMAT_INDEX), EXPORT_FORMAT_INDEX);
            EditorPrefs.SetInt(nameof(IMPORT_FORMAT_INDEX), IMPORT_FORMAT_INDEX);
        }
        private void LoadEditorConfig()
        {
            SKILL_DATA_FOLDER = EditorPrefs.GetString(nameof(SKILL_DATA_FOLDER), SKILL_DATA_FOLDER);
            TIMELINE_FOLDER = EditorPrefs.GetString(nameof(TIMELINE_FOLDER), TIMELINE_FOLDER);
            ASSET_EXPORT_PATH = EditorPrefs.GetString(nameof(ASSET_EXPORT_PATH), ASSET_EXPORT_PATH);
            DEFAULT_ANIM_TRACK_NAME = EditorPrefs.GetString(nameof(DEFAULT_ANIM_TRACK_NAME), DEFAULT_ANIM_TRACK_NAME);
            DEFAULT_AUDIO_TRACK_NAME = EditorPrefs.GetString(nameof(DEFAULT_AUDIO_TRACK_NAME), DEFAULT_AUDIO_TRACK_NAME);
            DEFAULT_EFFECT_TRACK_NAME = EditorPrefs.GetString(nameof(DEFAULT_EFFECT_TRACK_NAME), DEFAULT_EFFECT_TRACK_NAME);
            DEFAULT_DETECT_TRACK_NAME = EditorPrefs.GetString(nameof(DEFAULT_DETECT_TRACK_NAME), DEFAULT_DETECT_TRACK_NAME);
            DEFAULT_CURVE_TRACK_NAME = EditorPrefs.GetString(nameof(DEFAULT_CURVE_TRACK_NAME), DEFAULT_CURVE_TRACK_NAME);
            EXPORT_FORMAT_INDEX = EditorPrefs.GetInt(nameof(EXPORT_FORMAT_INDEX), EXPORT_FORMAT_INDEX);
            IMPORT_FORMAT_INDEX = EditorPrefs.GetInt(nameof(IMPORT_FORMAT_INDEX), IMPORT_FORMAT_INDEX);
        }
        #endregion
    }
}