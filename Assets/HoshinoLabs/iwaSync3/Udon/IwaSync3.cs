using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace HoshinoLabs.Udon.IwaSync3
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IwaSync3 : UdonSharpBehaviour
    {
        const string _APP_NAME = "iwaSync3";
        public
        #if !COMPILER_UDONSHARP
        static
        #endif
        string APP_NAME => _APP_NAME;
        const string _APP_VERSION = "V3.1.4";
        public
        #if !COMPILER_UDONSHARP
        static
        #endif
        string APP_VERSION => _APP_VERSION;

        [Header("Main")]
        [SerializeField]
        VideoCore core;
        [SerializeField]
        VideoController controller;

        GameObject _canvas1;
        GameObject _lockOn;
        Button _lockOnButton;
        GameObject _lockOff;
        Button _lockOffButton;
        Button _videoButton;
        Button _liveButton;

        GameObject _canvas2;
        GameObject _address;
        VRCUrlInputField _addressInput;
        GameObject _message;
        Text _messageText;
        GameObject _close;
        Button _closeButton;

        bool _local = false;
        uint _mode;
        [UdonSynced, FieldChangeCallback(nameof(on))]
        bool _on = false;

        private void Start()
        {
            Debug.Log($"[iwaSync3] Started `{nameof(IwaSync3)}`.");

            core.AddListener(this);
            controller.AddListener(this);

            _canvas1 = transform.Find("Canvas").gameObject;
            _lockOn = transform.Find("Canvas/Panel/Lock/On").gameObject;
            _lockOnButton = _lockOn.transform.Find("Button").GetComponent<Button>();
            _lockOff = transform.Find("Canvas/Panel/Lock/Off").gameObject;
            _lockOffButton = _lockOff.transform.Find("Button").GetComponent<Button>();
            _videoButton = transform.Find("Canvas/Panel/Video/Button").GetComponent<Button>();
            _liveButton = transform.Find("Canvas/Panel/Live/Button").GetComponent<Button>();

            _canvas2 = transform.Find("Canvas (1)").gameObject;
            _address = transform.Find("Canvas (1)/Panel/Address").gameObject;
            _addressInput = (VRCUrlInputField)_address.GetComponent(typeof(VRCUrlInputField));
            _message = transform.Find("Canvas (1)/Panel/Message").gameObject;
            _messageText = _message.GetComponent<Text>();
            _close = transform.Find("Canvas (1)/Panel/Close").gameObject;
            _closeButton = _close.transform.Find("Button").GetComponent<Button>();
        }

        #region RoomEvent
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            ValidateView();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (isOwner && _on && !_local)
                Close();
            ValidateView();
        }
        #endregion

        #region VideoEvent
        public override void OnVideoEnd()
        {
            ValidateView();
        }

        public override void OnVideoError(VideoError videoError)
        {
            ValidateView();
        }

        public override void OnVideoLoop()
        {
            ValidateView();
        }

        public override void OnVideoReady()
        {
            ValidateView();
        }

        public override void OnVideoStart()
        {
            ValidateView();
        }
        #endregion

        #region VideoCoreEvent
        public void OnPlayerPlay()
        {
            ValidateView();
        }

        public void OnPlayerPause()
        {
            ValidateView();
        }

        public void OnPlayerStop()
        {
            ValidateView();
        }

        public void OnChangeURL()
        {
            ValidateView();
        }
        #endregion

        #region VideoControllerEvent
        public void OnChangeLock()
        {
            ValidateView();
        }
        #endregion

        public void TakeOwnership()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public bool isOwner => Networking.IsOwner(gameObject);

        void ValidateView()
        {
            var locked = controller.locked;
            var master = Networking.IsMaster || (controller.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner);
            var privilege = (locked && master) || !locked;

            _canvas1.SetActive(!core.isPrepared && !core.isPlaying && !core.isError && !_on);
            _lockOn.SetActive(!locked);
            _lockOnButton.interactable = master;
            _lockOff.SetActive(locked);
            _lockOffButton.interactable = master;
            _videoButton.interactable = privilege;
            _liveButton.interactable = privilege;

            _canvas2.SetActive(!core.isPrepared && !core.isPlaying && !core.isError && _on);
            _address.SetActive(isOwner);
            _message.SetActive(!isOwner);
            _messageText.text = $"Entering the URL... ({Networking.GetOwner(gameObject).displayName})";
            _closeButton.interactable = isOwner || privilege;
        }

        public void LockOn()
        {
            controller.LockOn();
        }

        public void LockOff()
        {
            controller.LockOff();
        }

        public bool on
        {
            get => _on;
            private set
            {
                _on = value;
                UpdateOn();
            }
        }

        void UpdateOn()
        {
            ValidateView();
        }

        public void ModeVideo()
        {
            Debug.Log($"[iwaSync3] The mode has changed to `MODE_VIDEO`.");
            TakeOwnership();
            _local = true;
            _mode =
                #if COMPILER_UDONSHARP
                core.MODE_VIDEO
                #else
                VideoCore.MODE_VIDEO
                #endif
            ;
            _on = true;
            RequestSerialization();
            ValidateView();
        }

        public void ModeLive()
        {
            Debug.Log($"[iwaSync3] The mode has changed to `MODE_STREAM`.");
            TakeOwnership();
            _local = true;
            _mode =
                #if COMPILER_UDONSHARP
                core.MODE_STREAM
                #else
                VideoCore.MODE_STREAM
                #endif
            ;
            _on = true;
            RequestSerialization();
            ValidateView();
        }

        public void OnURLChanged()
        {
            Debug.Log($"[iwaSync3] The url has changed to `{_addressInput.GetUrl().Get()}`.");
            core.TakeOwnership();
            core.PlayURL(_mode, _addressInput.GetUrl());
            core.RequestSerialization();
            ValidateView();
        }

        public void ClearURL()
        {
            _addressInput.SetUrl(VRCUrl.Empty);
        }

        public void Close()
        {
            Debug.Log($"[iwaSync3] Trigger a close event.");
            TakeOwnership();
            _local = false;
            _on = false;
            RequestSerialization();
            ValidateView();
        }
    }
}
