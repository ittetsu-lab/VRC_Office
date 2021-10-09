using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace HoshinoLabs.IwaSync3
{
    [HelpURL("https://docs.google.com/document/d/1AOMawwq9suEgfa0iLCUX4MRhOiSLBNCLvPCnqW9yQ3g/edit#heading=h.hp0a2t9tw8cw")]
    public class Playlist : IwaSync3Base
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(Track))]
        public class TrackDrawer : PropertyDrawer
        {
            SerializedProperty _modeProperty;
            SerializedProperty _titleProperty;
            SerializedProperty _urlProperty;

            void FindProperties(SerializedProperty property)
            {
                _modeProperty = property.FindPropertyRelative("mode");
                _titleProperty = property.FindPropertyRelative("title");
                _urlProperty = property.FindPropertyRelative("url");
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                FindProperties(property);

                return EditorGUI.GetPropertyHeight(_modeProperty)
                    + EditorGUI.GetPropertyHeight(_titleProperty)
                    + EditorGUI.GetPropertyHeight(_urlProperty);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                FindProperties(property);

                using (new EditorGUI.PropertyScope(position, label, property))
                {
                    EditorGUIUtility.labelWidth = 64f;
                    position = PropertyField(position, _modeProperty);
                    position = PropertyField(position, _titleProperty);
                    position = PropertyField(position, _urlProperty);
                }
            }

            Rect PropertyField(Rect position, SerializedProperty property)
            {
                position.height = EditorGUI.GetPropertyHeight(property);
                EditorGUI.PropertyField(position, property);
                position.y += EditorGUI.GetPropertyHeight(property);
                return position;
            }
        }
#endif

#pragma warning disable CS0414
        [SerializeField]
        IwaSync3 iwaSync3;

        [SerializeField]
        bool defaultShuffle = false;
        [SerializeField]
        bool defaultRepeat = true;

        [SerializeField]
        string playlistUrl;

        [SerializeField]
        Track[] tracks;
#pragma warning restore CS0414
    }
}
