﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HoshinoLabs.Udon.IwaSync3
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CustomEventInvoker : UdonSharpBehaviour
    {
        CustomEventInvoker _invoker;

        UdonSharpBehaviour _udon;
        string _eventName;

        public void Invoke(UdonSharpBehaviour udon, string eventName, float delaySeconds)
        {
            _invoker = VRCInstantiate(gameObject).GetComponent<CustomEventInvoker>();
            _invoker._Invoke(udon, eventName, delaySeconds);
        }

        public void _Invoke(UdonSharpBehaviour udon, string eventName, float delaySeconds)
        {
            _udon = udon;
            _eventName = eventName;
            SendCustomEventDelayedSeconds(nameof(_Execute), delaySeconds);
        }

        public void ClearInvoke()
        {
            if (_invoker == null)
                return;
            _invoker._ClearInvoke();
            DestroyImmediate(_invoker.gameObject);
        }

        public void _ClearInvoke()
        {
            _udon = null;
        }

        public void _Execute()
        {
            if (_udon != null)
                _udon.SendCustomEvent(_eventName);
            _ClearInvoke();
        }
    }
}
