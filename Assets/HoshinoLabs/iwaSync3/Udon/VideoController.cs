using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;

namespace HoshinoLabs.Udon.IwaSync3
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VideoController : UdonSharpBehaviour
    {
        [Header("Main")]
        [SerializeField]
        VideoCore core;
        [SerializeField]
        AudioSource[] speaker;

        [Header("Options")]
        [SerializeField, UdonSynced, FieldChangeCallback(nameof(locked))]
        bool defaultLock = false;
        [SerializeField]
        bool allowSeeking = true;
        [SerializeField]
        float seekTimeSeconds = 10f;
        [SerializeField]
        string timeFormat = @"hh\:mm\:ss\:ff";
        [SerializeField]
        bool allowInstanceOwner = true;
        [SerializeField]
        bool defaultMute = false;
        [SerializeField]
        [Range(0f, 1f)]
        float defaultMinVolume = 0f;
        [SerializeField]
        [Range(0f, 1f)]
        float defaultMaxVolume = 0.5f;
        [SerializeField]
        [Range(0f, 1f)]
        float defaultVolume = 0.184f;

        GameObject _canvas1;
        GameObject _progress;
        Slider _progressSlider;
        GameObject _progressSliderHandle;
        GameObject _lockOn;
        Button _lockOnButton;
        GameObject _lockOff;
        Button _lockOffButton;
        GameObject _backward;
        Button _backwardButton;
        GameObject _pauseOn;
        Button _pauseOnButton;
        GameObject _pauseOff;
        Button _pauseOffButton;
        GameObject _stop;
        Button _stopButton;
        GameObject _forward;
        Button _forwardButton;
        GameObject _message;
        Text _messageText;
        GameObject _muteOn;
        GameObject _muteOff;
        GameObject _volume;
        Slider _volumeSlider;
        GameObject _reload;
        GameObject _loopOn;
        Button _loopOnButton;
        GameObject _loopOff;
        Button _loopOffButton;
        GameObject _optionsOn;
        GameObject _optionsOff;

        GameObject _canvas2;
        Text _masterText;
        Text _offsetTimeText;
        Slider _minVolumeSlider;
        Slider _maxVolumeSlider;

        VRCPlayerApi _master;
        bool _progressDrag = false;
        bool _volumeDrag = false;
        bool _minVolumeDrag = false;
        bool _maxVolumeDrag = false;
        bool _options = false;

        private void Start()
        {
            Debug.Log($"[iwaSync3] Started `{nameof(VideoController)}`.");

            core.AddListener(this);

            _canvas1 = transform.Find("Canvas").gameObject;
            _progress = transform.Find("Canvas/Panel/Progress").gameObject;
            _progressSlider = _progress.GetComponent<Slider>();
            _progressSliderHandle = _progressSlider.handleRect.gameObject;
            _lockOn = transform.Find("Canvas/Panel/Lock/On").gameObject;
            _lockOnButton = _lockOn.transform.Find("Button").GetComponent<Button>();
            _lockOff = transform.Find("Canvas/Panel/Lock/Off").gameObject;
            _lockOffButton = _lockOff.transform.Find("Button").GetComponent<Button>();
            _backward = transform.Find("Canvas/Panel/Backward").gameObject;
            _backwardButton = _backward.transform.Find("Button").GetComponent<Button>();
            _pauseOn = transform.Find("Canvas/Panel/Pause/On").gameObject;
            _pauseOnButton = _pauseOn.transform.Find("Button").GetComponent<Button>();
            _pauseOff = transform.Find("Canvas/Panel/Pause/Off").gameObject;
            _pauseOffButton = _pauseOff.transform.Find("Button").GetComponent<Button>();
            _stop = transform.Find("Canvas/Panel/Stop").gameObject;
            _stopButton = _stop.transform.Find("Button").GetComponent<Button>();
            _forward = transform.Find("Canvas/Panel/Forward").gameObject;
            _forwardButton = _forward.transform.Find("Button").GetComponent<Button>();
            _message = transform.Find("Canvas/Panel/Message").gameObject;
            _messageText = _message.transform.Find("Text").GetComponent<Text>();
            _muteOn = transform.Find("Canvas/Panel/Mute/On").gameObject;
            _muteOff = transform.Find("Canvas/Panel/Mute/Off").gameObject;
            _volume = transform.Find("Canvas/Panel/Volume").gameObject;
            _volumeSlider = _volume.GetComponent<Slider>();
            _reload = transform.Find("Canvas/Panel/Reload").gameObject;
            _loopOn = transform.Find("Canvas/Panel/Loop/On").gameObject;
            _loopOnButton = _loopOn.transform.Find("Button").GetComponent<Button>();
            _loopOff = transform.Find("Canvas/Panel/Loop/Off").gameObject;
            _loopOffButton = _loopOff.transform.Find("Button").GetComponent<Button>();
            _optionsOn = transform.Find("Canvas/Panel/Options/On").gameObject;
            _optionsOff = transform.Find("Canvas/Panel/Options/Off").gameObject;

            _canvas2 = transform.Find("Canvas (1)").gameObject;
            _masterText = transform.Find("Canvas (1)/Panel/Layout/Layout/Value/Layout/Master").GetComponent<Text>();
            _offsetTimeText = transform.Find("Canvas (1)/Panel/Layout/Layout/Value/Layout/OffsetTime/Time/Text").GetComponent<Text>();
            _minVolumeSlider = transform.Find("Canvas (1)/Panel/Layout/Layout/Value/Layout/MinVolume").GetComponent<Slider>();
            _maxVolumeSlider = transform.Find("Canvas (1)/Panel/Layout/Layout/Value/Layout/MaxVolume").GetComponent<Slider>();

            UpdateSpeaker();
        }

        #region EventListener
        VideoControllerEventListener[] _listeners;

        public void AddListener(UdonSharpBehaviour listener)
        {
            if (_listeners == null)
                _listeners = new VideoControllerEventListener[0];
            var array = new VideoControllerEventListener[_listeners.Length + 1];
            _listeners.CopyTo(array, 0);
            array[_listeners.Length] = (VideoControllerEventListener)listener;
            _listeners = array;
        }
        #endregion

        #region RoomEvent
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _master = FindMaster();
            ValidateView();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _master = FindMaster();
            ValidateView();
        }
        #endregion

        VRCPlayerApi FindMaster()
        {
            foreach (var x in VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]))
            {
                if (x.isMaster)
                    return x;
            }
            return Networking.LocalPlayer;
        }

        #region VideoEvent
        public override void OnVideoEnd()
        {
            if (core.isOwner)
                core.RequestSerialization();
            ValidateView();
        }

        public override void OnVideoError(VideoError videoError)
        {
            switch (core.error)
            {
                case VideoError.Unknown:
                    _messageText.text = $"Playback failed due to unknown error.";
                    break;
                case VideoError.InvalidURL:
                    _messageText.text = $"Invalid URL.";
                    break;
                case VideoError.AccessDenied:
                    _messageText.text = $"Not allowed untrusted url.";
                    break;
                case VideoError.PlayerError:
                    _messageText.text = $"Video loading failed.";
                    if (0 < core.errorRetry)
                        _messageText.text = $"{_messageText.text} Retry 3 times.";
                    break;
                case VideoError.RateLimited:
                    _messageText.text = $"Rate limited.";
                    if (0 < core.errorRetry)
                        _messageText.text = $"{_messageText.text} Retry 3 times.";
                    break;
            }
            ValidateView();
        }

        public override void OnVideoLoop()
        {
            if(core.isOwner)
                core.RequestSerialization();
            ValidateView();
        }

        public override void OnVideoReady()
        {
            ValidateView();
        }

        public override void OnVideoStart()
        {
            if (core.isOwner)
                core.RequestSerialization();
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
            _messageText.text = $"Loading Now";
            if (core.isOwner)
                core.RequestSerialization();
            ValidateView();
        }

        public void OnChangeLoop()
        {
            ValidateView();
        }
        #endregion

        public void TakeOwnership()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void ValidateView()
        {
            var master = Networking.IsMaster || (isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner);
            var privilege = (locked && master) || !locked;

            _canvas1.SetActive(core.isPrepared || core.isPlaying || core.isError);
            _progress.SetActive(core.isPlaying && !core.isError);
            _progressSlider.interactable = privilege && !core.isLive && allowSeeking;
            _progressSliderHandle.SetActive(!core.isLive);
            _lockOn.SetActive(!locked);
            _lockOnButton.interactable = master;
            _lockOff.SetActive(locked);
            _lockOffButton.interactable = master;
            _backward.SetActive(core.isPlaying && !core.isLive && !core.isError);
            _backwardButton.interactable = privilege;
            _pauseOn.SetActive((core.isPlaying && core.paused) && !core.isLive && !core.isError);
            _pauseOnButton.interactable = privilege;
            _pauseOff.SetActive((core.isPlaying && !core.paused) && !core.isLive && !core.isError);
            _pauseOffButton.interactable = privilege;
            _stop.SetActive((core.isPrepared || core.isPlaying) || core.isError);
            _stopButton.interactable = privilege;
            _forward.SetActive(core.isPlaying && !core.isLive && !core.isError);
            _forwardButton.interactable = privilege;
            _message.SetActive((core.isPrepared || core.isPlaying) || core.isError);
            _muteOn.SetActive(!muted);
            _muteOff.SetActive(muted);
            _volumeSlider.minValue = defaultMinVolume;
            _volumeSlider.maxValue = defaultMaxVolume;
            _volumeSlider.value = defaultVolume;
            _reload.SetActive(core.isPlaying || (core.isError && core.errorRetry == 0));
            _loopOn.SetActive(!core.loop);
            _loopOnButton.interactable = privilege;
            _loopOff.SetActive(core.loop);
            _loopOffButton.interactable = privilege;
            _optionsOn.SetActive(!_options);
            _optionsOff.SetActive(_options);

            _canvas2.SetActive((core.isPrepared || core.isPlaying || core.isError) && _options);
            _masterText.text = _master == null ? string.Empty : $"{_master.displayName}({_master.playerId})";
            _offsetTimeText.text = $"{core.offsetTime / 1000f}";
            _minVolumeSlider.value = defaultMinVolume;
            _maxVolumeSlider.value = defaultMaxVolume;
        }

        private void Update()
        {
            if (!core.isPlaying)
                return;

            if (core.isLive)
            {
                _messageText.text = "Live";
                _progressSlider.value = 1f;
            }
            else
            {
                var time = core.time;
                _messageText.text = $"{TimeSpan.FromSeconds(time).ToString(timeFormat)}";
                var duration = core.duration;
                _messageText.text = $"{_messageText.text}/{TimeSpan.FromSeconds(duration).ToString(timeFormat)}";
                if (!_progressDrag)
                    _progressSlider.value = Mathf.Clamp(time / duration, 0f, 1f);
            }
        }

        void UpdateTime()
        {
            core.TakeOwnership();
            core.RequestSerialization();
        }

        public void OnProgressBeginDrag()
        {
            Debug.Log($"[iwaSync3] Progress has started dragging.");
            _progressDrag = true;
        }

        public void OnProgressEndDrag()
        {
            Debug.Log($"[iwaSync3] Progress drag is finished.");
            _progressDrag = false;
            UpdateTime();
        }

        public void OnProgressChanged()
        {
            if (!_progressDrag)
                return;
            core.clockTime = Networking.GetServerTimeInMilliseconds();
            core.time = core.duration * _progressSlider.value;
        }

        public bool locked
        {
            get => defaultLock;
            private set
            {
                defaultLock = value;
                UpdateLocked();
            }
        }

        public bool isAllowInstanceOwner => allowInstanceOwner;

        void UpdateLocked()
        {
            // event emit
            if (_listeners != null)
            {
                Debug.Log($"[iwaSync3] Emit event of change lock.");
                foreach (var x in _listeners)
                    x.OnChangeLock();
            }
            ValidateView();
        }

        public void LockOn()
        {
            Debug.Log($"[iwaSync3] Trigger a lock on event.");
            TakeOwnership();
            locked = true;
            RequestSerialization();
        }

        public void LockOff()
        {
            Debug.Log($"[iwaSync3] Trigger a lock off event.");
            TakeOwnership();
            locked = false;
            RequestSerialization();
        }

        public void Backward()
        {
            Debug.Log($"[iwaSync3] Trigger a backward event.");
            core.TakeOwnership();
            core.clockTime = Networking.GetServerTimeInMilliseconds();
            core.Seek(-seekTimeSeconds);
            core.RequestSerialization();
        }

        public void PauseOn()
        {
            Debug.Log($"[iwaSync3] Trigger a unpause event.");
            core.TakeOwnership();
            core.Play();
            core.clockTime = Networking.GetServerTimeInMilliseconds();
            core.Seek(0f);
            core.RequestSerialization();
        }

        public void PauseOff()
        {
            Debug.Log($"[iwaSync3] Trigger a pause event.");
            core.TakeOwnership();
            core.Pause();
            core.clockTime = Networking.GetServerTimeInMilliseconds();
            core.Seek(0f);
            core.RequestSerialization();
        }

        public void Stop()
        {
            Debug.Log($"[iwaSync3] Trigger a stop event.");
            core.TakeOwnership();
            core.Stop();
            core.RequestSerialization();
        }

        public void Forward()
        {
            Debug.Log($"[iwaSync3] Trigger a forward event.");
            core.TakeOwnership();
            core.clockTime = Networking.GetServerTimeInMilliseconds();
            core.Seek(seekTimeSeconds);
            core.RequestSerialization();
        }

        public bool muted
        {
            get => defaultMute;
            set
            {
                defaultMute = value;
                UpdateMuted();
            }
        }

        void UpdateMuted()
        {
            UpdateSpeaker();
            // event emit
            if (_listeners != null)
            {
                Debug.Log($"[iwaSync3] Emit event of change mute.");
                foreach (var x in _listeners)
                    x.OnChangeMute();
            }
            ValidateView();
        }

        void UpdateSpeaker()
        {
            foreach (var x in speaker)
            {
                x.mute = defaultMute;
                x.volume = Mathf.Lerp(defaultMinVolume, defaultMaxVolume, defaultVolume);
            }
        }

        public void MuteOn()
        {
            Debug.Log($"[iwaSync3] Trigger a mute on event.");
            muted = true;
        }

        public void MuteOff()
        {
            Debug.Log($"[iwaSync3] Trigger a mute off event.");
            muted = false;
        }

        public float volume
        {
            get => defaultVolume * (1f / (defaultMaxVolume - defaultMinVolume));
            set
            {
                defaultVolume = defaultMinVolume + (Mathf.Clamp(value, 0f, 1f) * (defaultMaxVolume - defaultMinVolume));
                UpdateVolume();
            }
        }

        void UpdateVolume()
        {
            UpdateSpeaker();
            if (_volumeDrag)
                return;
            // event emit
            if (_listeners != null)
            {
                foreach (var x in _listeners)
                    x.OnChangeVolume();
            }
            ValidateView();
        }

        public void OnVolumeBeginDrag()
        {
            Debug.Log($"[iwaSync3] Volume has started dragging.");
            _volumeDrag = true;
        }

        public void OnVolumeEndDrag()
        {
            Debug.Log($"[iwaSync3] Volume drag is finished.");
            _volumeDrag = false;
            UpdateVolume();
        }

        public void OnVolumeChanged()
        {
            if (!_volumeDrag)
                return;
            volume = _volumeSlider.value * (1f / (defaultMaxVolume - defaultMinVolume));
        }

        public void Reload()
        {
            Debug.Log($"[iwaSync3] Trigger a reload event.");
            core.Reload();
        }

        public void LoopOn()
        {
            Debug.Log($"[iwaSync3] Trigger a loop on event.");
            core.TakeOwnership();
            core.loop = true;
            core.RequestSerialization();
            ValidateView();
        }

        public void LoopOff()
        {
            Debug.Log($"[iwaSync3] Trigger a loop off event.");
            core.TakeOwnership();
            core.loop = false;
            core.RequestSerialization();
            ValidateView();
        }

        public void OptionsOn()
        {
            _options = true;
            ValidateView();
        }

        public void OptionsOff()
        {
            _options = false;
            ValidateView();
        }

        public void OffsetTimeLL()
        {
            core.offsetTime -= 100;
            ValidateView();
        }

        public void OffsetTimeL()
        {
            core.offsetTime -= 10;
            ValidateView();
        }

        public void OffsetTimeR()
        {
            core.offsetTime += 10;
            ValidateView();
        }

        public void OffsetTimeRR()
        {
            core.offsetTime += 100;
            ValidateView();
        }

        public void OffsetTimeClear()
        {
            core.offsetTime = 0;
            ValidateView();
        }

        public float minVolume
        {
            get => defaultMinVolume;
            set
            {
                defaultMinVolume = Mathf.Clamp(value, 0f, defaultMaxVolume);
                defaultMaxVolume = Mathf.Clamp(defaultMaxVolume, defaultMinVolume, 1f);
                defaultVolume = Mathf.Clamp(defaultVolume, defaultMinVolume, defaultMaxVolume);
                UpdateVolume();
            }
        }

        public void OnMinVolumeChanged()
        {
            if (!_volumeDrag)
                return;
            minVolume = _minVolumeSlider.value;
        }

        public float maxVolume
        {
            get => defaultMaxVolume;
            set
            {
                defaultMinVolume = Mathf.Clamp(defaultMinVolume, 0f, defaultMaxVolume);
                defaultMaxVolume = Mathf.Clamp(value, defaultMinVolume, 1f);
                defaultVolume = Mathf.Clamp(defaultVolume, defaultMinVolume, defaultMaxVolume);
                UpdateVolume();
            }
        }

        public void OnMaxVolumeChanged()
        {
            if (!_volumeDrag)
                return;
            maxVolume = _maxVolumeSlider.value;
        }
    }
}
