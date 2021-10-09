using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoshinoLabs.IwaSync3
{
    [HelpURL("https://docs.google.com/document/d/1AOMawwq9suEgfa0iLCUX4MRhOiSLBNCLvPCnqW9yQ3g/edit#heading=h.v8xuael90eyq")]
    public class Screen : IwaSync3Base
    {
#pragma warning disable CS0414
        [SerializeField]
        IwaSync3 iwaSync3;

        [SerializeField]
        int materialIndex = 0;
        [SerializeField]
        string textureProperty = "_MainTex";
        [SerializeField]
        bool idleScreenOff = false;
        [SerializeField]
        Texture idleScreenTexture = null;
        [SerializeField]
        float aspectRatio = 1.777778f;
#pragma warning restore CS0414
    }
}
