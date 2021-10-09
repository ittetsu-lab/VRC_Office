using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HoshinoLabs.Udon.IwaSync3
{
    public abstract class VideoControllerEventListener : UdonSharpBehaviour
    {
        #region VideoControllerEvent
        public void OnChangeLock() { }
        public void OnChangeMute() { }
        public void OnChangeVolume() { }
        #endregion
    }
}
