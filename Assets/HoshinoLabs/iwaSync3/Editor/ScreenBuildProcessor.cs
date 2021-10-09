﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoshinoLabs.IwaSync3
{
    public class ScreenBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var x in GameObject.FindObjectsOfType<Screen>())
            {
                var editor = (ScreenEditor)Editor.CreateEditor(x);
                editor.ApplyModifiedProperties();
                GameObject.DestroyImmediate(editor);
            }
        }
    }
}
