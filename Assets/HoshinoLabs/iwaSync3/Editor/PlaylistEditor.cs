using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace HoshinoLabs.IwaSync3
{
    [CustomEditor(typeof(Playlist))]
    public class PlaylistEditor : IwaSync3EditorBase
    {
        Playlist _target;

        SerializedProperty _iwaSync3Property;

        SerializedProperty _defaultShuffleProperty;
        SerializedProperty _defaultRepeatProperty;

        SerializedProperty _playlistUrlProperty;

        SerializedProperty _tracksProperty;

        ReorderableList _tracksList;

        protected override void FindProperties()
        {
            base.FindProperties();

            _target = target as Playlist;

            _iwaSync3Property = serializedObject.FindProperty("iwaSync3");

            _defaultShuffleProperty = serializedObject.FindProperty("defaultShuffle");
            _defaultRepeatProperty = serializedObject.FindProperty("defaultRepeat");

            _playlistUrlProperty = serializedObject.FindProperty("playlistUrl");

            var tracksProperty = serializedObject.FindProperty("tracks");
            if (_tracksList == null || _tracksProperty.serializedObject != _tracksProperty.serializedObject)
            {
                _tracksProperty = tracksProperty;
                _tracksList = new ReorderableList(serializedObject, tracksProperty)
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, tracksProperty.displayName);
                    },
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        EditorGUI.PropertyField(rect, tracksProperty.GetArrayElementAtIndex(index));
                    },
                    elementHeightCallback = (index) =>
                    {
                        return EditorGUI.GetPropertyHeight(tracksProperty.GetArrayElementAtIndex(index)) + EditorGUIUtility.standardVerticalSpacing;
                    },
                };
            }
        }

        public override void OnInspectorGUI()
        {
            FindProperties();

            base.OnInspectorGUI();

            serializedObject.Update();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Main", _italicStyle);
                var iwaSync3 = GetMainIwaSync3(null);
                if (iwaSync3)
                    EditorGUILayout.LabelField(_iwaSync3Property.displayName, "Automatically set by Script");
                else
                    EditorGUILayout.PropertyField(_iwaSync3Property);
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Tools", _italicStyle);
                EditorGUILayout.HelpBox(string.Join("\n",
                        "試験的な機能です",
                        "使用前にシーンのバックアップを取るなどの対応をお願いします",
                        "現在はYoutubeのみ対応しています",
                        "モードは取得できないので必要に応じて手動設定して下さい",
                        "日本語以外の文字は文字化けする場合があります"
                    ), MessageType.Info
                );
                var exists = YoutubeDLHelper.Exists;
                if (!exists)
                {
                    EditorGUILayout.HelpBox(string.Join("\n",
                            "YoutubeDLが見つからないためこの機能は使用できません",
                            "VRChatの起動と終了を行ってから再度試してみても改善しない場合は作者へ問い合わせをお願いします"
                        ), MessageType.Warning
                    );
                }
                using (new EditorGUI.DisabledGroupScope(!exists))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.LabelField("★プレイリスト(Youtube)から自動取得", _italicStyle);
                        EditorGUILayout.PropertyField(_playlistUrlProperty);
                        if (PlaylistTools.IsCompleted)
                        {
                            var valid = PlaylistTools.IsValidPlayListURL(_playlistUrlProperty.stringValue);
                            if(!valid && !string.IsNullOrEmpty(_playlistUrlProperty.stringValue))
                            {
                                EditorGUILayout.HelpBox(string.Join("\n",
                                        "YoutubeのプレイリストURLではないようです"
                                    ), MessageType.Warning
                                );
                            }
                            using (new EditorGUI.DisabledGroupScope(!valid))
                            {
                                if (GUILayout.Button("Run"))
                                    PlaylistTools.ResolvePlaylist(_target);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Abort"))
                                PlaylistTools.Cancel();
                        }
                    }
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.LabelField("★動画のタイトルを自動取得", _italicStyle);
                        if (PlaylistTools.IsCompleted)
                        {
                            if (GUILayout.Button("Run"))
                                PlaylistTools.ResolveTracks(_target);
                        }
                        else
                        {
                            if (GUILayout.Button("Abort"))
                                PlaylistTools.Cancel();
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Options", _italicStyle);
                EditorGUILayout.PropertyField(_defaultShuffleProperty);
                EditorGUILayout.PropertyField(_defaultRepeatProperty);
            }

            EditorGUILayout.Space();

            _tracksList.DoLayoutList();

            if (serializedObject.ApplyModifiedProperties())
                ApplyModifiedProperties();
        }

        public void ApplyModifiedProperties()
        {
            FindProperties();

            var iwaSync3 = GetMainIwaSync3(_iwaSync3Property);
            if (iwaSync3 == null)
                return;
            var core = iwaSync3.GetUdonComponentInChildren<Udon.IwaSync3.VideoCore>();
            var controller = iwaSync3.GetUdonComponentInChildren<Udon.IwaSync3.VideoController>();

            var self = _target.GetUdonComponentInChildren<Udon.IwaSync3.Playlist>();
            self.SetPublicVariable("core", core);
            self.SetPublicVariable("controller", controller);
            self.SetPublicVariable("defaultShuffle", _defaultShuffleProperty.boolValue);
            self.SetPublicVariable("defaultRepeat", _defaultRepeatProperty.boolValue);

            var content = self.transform.Find("Canvas/Panel/Scroll View/Scroll View/Viewport/Content");
            var template = content.Find("Template");
            template.gameObject.SetActive(false);

            for (var i = content.childCount - 1; 0 < i; i--)
            {
                var item = content.GetChild(i);
                if (item == template)
                    continue;
                DestroyImmediate(item.gameObject);
            }

            for (var i = 0; i < _tracksProperty.arraySize; i++)
            {
                var track = _tracksProperty.GetArrayElementAtIndex(i);

                var obj = Instantiate(template.gameObject, content);
                obj.SetActive(true);
                var buttonText = obj.transform.Find("Button/Text").GetComponent<Text>();
                buttonText.text = track.FindPropertyRelative("title").stringValue;
                var modeText = obj.transform.Find("Mode").GetComponent<Text>();
                modeText.text = (((TrackMode)track.FindPropertyRelative("mode").intValue).ToVideoCoreMode()).ToString();
                var addressInput = (VRCUrlInputField)obj.transform.Find("Address").GetComponent(typeof(VRCUrlInputField));
                addressInput.SetUrl(new VRCUrl(track.FindPropertyRelative("url").stringValue));
                GameObjectUtility.EnsureUniqueNameForSibling(obj);
            }
        }
    }
}
