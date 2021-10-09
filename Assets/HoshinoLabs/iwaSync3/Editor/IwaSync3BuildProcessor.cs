using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoshinoLabs.IwaSync3
{
    public class IwaSync3BuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 184;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var x in GameObject.FindObjectsOfType<IwaSync3>())
            {
                var editor = (IwaSync3Editor)Editor.CreateEditor(x);
                editor.ApplyModifiedProperties();
                GameObject.DestroyImmediate(editor);
            }
            foreach(var x in GameObject.FindObjectsOfType<IwaSync3Base>())
                GameObject.DestroyImmediate(x);
        }
    }
}
