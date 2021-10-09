using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Video.Components.AVPro;

namespace HoshinoLabs.IwaSync3
{
    [CustomEditor(typeof(VRCAVProVideoSpeaker))]
    [CanEditMultipleObjects]
    public class VRCAVProVideoSpeakerEditor : Editor
    {
        Editor _editor;

        bool _controlled;

        private void OnEnable()
        {
            var editorType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.GetType("UnityEditor.GenericInspector"))
                .Where(x => x != null)
                .FirstOrDefault();
            _editor = Editor.CreateEditor(target, editorType);

            var avProVideoSpeaker = target as VRCAVProVideoSpeaker;
            var speaker = avProVideoSpeaker?.GetComponentInParent<Speaker>();
            _controlled = speaker?.GetComponentInChildren<VRCAVProVideoSpeaker>() == avProVideoSpeaker;
        }

        private void OnDisable()
        {
            GameObject.DestroyImmediate(_editor);
        }

        public override void OnInspectorGUI()
        {
            if(_controlled)
                EditorGUILayout.HelpBox($"このコンポーネントの一部パラメーターは{Udon.IwaSync3.IwaSync3.APP_NAME}によって制御されています", MessageType.Warning);

            _editor.OnInspectorGUI();
        }
    }
}
