using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace HoshinoLabs.Udon.IwaSync3
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DesktopBar : UdonSharpBehaviour
    {
        [Header("Main")]
        [SerializeField]
        VideoCore core;
        [SerializeField]
        VideoController controller;

        [Header("Options")]
        [SerializeField]
        bool desktopOnly = true;

        GameObject _canvas1;
        GameObject _lockOn;
        GameObject _lockOff;
        GameObject _video;
        GameObject _live;
        GameObject _muteOn;
        GameObject _muteOff;
        GameObject _volume;
        Text _volumeText;
        GameObject _pauseOn;
        GameObject _pauseOff;
        GameObject _loopOn;
        GameObject _loopOff;
        GameObject _address;

        GameObject _canvas2;
        RectTransform _canvas2Rect;
        VRCUrlInputField _addressInput;

        GameObject _canvasPIP;
        RawImage _image1;
        RectTransform _image1Rect;
        AspectRatioFitter _image1Aspect;

        GameObject _canvasFullScreen;
        RawImage _image2;
        RectTransform _image2Rect;
        AspectRatioFitter _image2Aspect;

        uint _mode;

        private void Start()
        {
            Debug.Log($"[iwaSync3] Started `{nameof(DesktopBar)}`.");

#if !UNITY_EDITOR
            if(desktopOnly && Networking.LocalPlayer.IsUserInVR())
            {
                Destroy(gameObject);
                return;
            }
#endif

            core.AddListener(this);
            controller.AddListener(this);

            _canvas1 = transform.Find("Canvas").gameObject;
            _lockOn = transform.Find("Canvas/Panel/Help/Group/Lock/Value/Icon/On").gameObject;
            _lockOff = transform.Find("Canvas/Panel/Help/Group/Lock/Value/Icon/Off").gameObject;
            _video = transform.Find("Canvas/Panel/Help/Group/Mode/Value/Icon/Video").gameObject;
            _live = transform.Find("Canvas/Panel/Help/Group/Mode/Value/Icon/Live").gameObject;
            _muteOn = transform.Find("Canvas/Panel/Help/Group/Mute/Value/Icon/On").gameObject;
            _muteOff = transform.Find("Canvas/Panel/Help/Group/Mute/Value/Icon/Off").gameObject;
            _volume = transform.Find("Canvas/Panel/Help/Group/Volume").gameObject;
            _volumeText = _volume.transform.Find("Value").GetComponent<Text>();
            _pauseOn = transform.Find("Canvas/Panel/Help/Group (1)/Pause/Value/Icon/On").gameObject;
            _pauseOff = transform.Find("Canvas/Panel/Help/Group (1)/Pause/Value/Icon/Off").gameObject;
            _loopOn = transform.Find("Canvas/Panel/Help/Group (1)/Loop/Value/Icon/On").gameObject;
            _loopOff = transform.Find("Canvas/Panel/Help/Group (1)/Loop/Value/Icon/Off").gameObject;

            _canvas2 = transform.Find("Canvas (1)").gameObject;
            _canvas2Rect = _canvas2.GetComponent<RectTransform>();
            _address = transform.Find("Canvas (1)/Address").gameObject;
            _addressInput = (VRCUrlInputField)_address.GetComponent(typeof(VRCUrlInputField));

            _canvasPIP = transform.Find("Canvas (PIP)").gameObject;
            _image1 = _canvasPIP.transform.Find("Panel/RawImage").GetComponent<RawImage>();
            _image1Rect = _image1.GetComponent<RectTransform>();
            _image1Aspect = _image1.GetComponent<AspectRatioFitter>();

            _canvasFullScreen = transform.Find("Canvas (FullScreen)").gameObject;
            _image2 = _canvasFullScreen.transform.Find("Panel/RawImage").GetComponent<RawImage>();
            _image2Rect = _image2.GetComponent<RectTransform>();
            _image2Aspect = _image2.GetComponent<AspectRatioFitter>();
            
            ModeVideo();
        }

        #region RoomEvent
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            ValidateView();
        }
        #endregion

        #region VideoEvent
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

        public void OnChangeLoop()
        {
            ValidateView();
        }
        #endregion

        #region VideoControllerEvent
        public void OnChangeLock()
        {
            ValidateView();
        }

        public void OnChangeMute()
        {
            ValidateView();
        }

        public void OnChangeVolume()
        {
            ValidateView();
        }
        #endregion

        public bool isModeVideo => _mode ==
        #if COMPILER_UDONSHARP
        core.MODE_VIDEO
        #else
        VideoCore.MODE_VIDEO
        #endif
        ;
        public bool isModeStream => _mode ==
        #if COMPILER_UDONSHARP
        core.MODE_STREAM
        #else
        VideoCore.MODE_STREAM
        #endif
        ;

        public void ValidateView()
        {
            var locked = controller.locked;

            _lockOn.SetActive(!locked);
            _lockOff.SetActive(locked);
            _video.SetActive(isModeVideo);
            _live.SetActive(isModeStream);
            _muteOn.SetActive(!controller.muted);
            _muteOff.SetActive(controller.muted);
            _volumeText.text = $"Volume ({Mathf.RoundToInt(controller.volume * 100f)}%)";
            _pauseOn.SetActive(core.paused);
            _pauseOff.SetActive(!core.paused);
            _loopOn.SetActive(!core.loop);
            _loopOff.SetActive(core.loop);
            _addressInput.SetUrl(core.url);

            var texture = core.isPlaying ? core.texture : null;
            var ratio = 1f;
            var mode = AspectRatioFitter.AspectMode.None;
            if (core.isPlaying)
            {
                if (texture == null)
                    SendCustomEventDelayedFrames(nameof(ValidateView), 0);
                else
                {
                    ratio = (float)texture.width / (float)texture.height;
                    var screen = _canvas2Rect.sizeDelta;
                    if (ratio < (screen.x / screen.y))
                        mode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                    else
                        mode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                }
            }

            _image1.enabled = core.isPlaying;
            _image1.texture = texture;
            _image1.material.SetInt("_IsAVProVideo", core.isModeVideo ? 0 : 1);
            _image1Rect.localScale = new Vector3(1f, core.isModeVideo ? 1f : -1f, 1f);
            _image1Aspect.aspectMode = mode;
            _image1Aspect.aspectRatio = ratio;

            _image2.enabled = core.isPlaying;
            _image2.texture = texture;
            _image2.material.SetInt("_IsAVProVideo", core.isModeVideo ? 0 : 1);
            _image2Rect.localScale = new Vector3(1f, core.isModeVideo ? 1f : -1f, 1f);
            _image2Aspect.aspectMode = mode;
            _image2Aspect.aspectRatio = ratio;
        }

        private void Update()
        {
            if(/*Input.GetKeyDown(KeyCode.LeftControl) || */Input.GetKeyDown(KeyCode.RightControl))
            {
                _canvas1.SetActive(true);

                _canvas2.SetActive(true);
                _addressInput.ActivateInputField();
                _addressInput.SetUrl(core.url);
            }
            if (/*Input.GetKey(KeyCode.LeftControl) || */Input.GetKey(KeyCode.RightControl))
            {
                var locked = controller.locked;
                var master = Networking.IsMaster || (controller.isAllowInstanceOwner && Networking.LocalPlayer.isInstanceOwner);
                var privilege = (locked && master) || !locked;

                if (master)
                {
                    if (Input.GetKeyDown(KeyCode.U))
                    {
                        if (controller.locked)
                            controller.LockOff();
                        else
                            controller.LockOn();
                    }
                }

                if (privilege)
                {
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        if (isModeVideo)
                            ModeLive();
                        else
                            ModeVideo();
                        ValidateView();
                    }
                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        if(!core.isPrepared)
                            OnURLChanged();
                    }
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    controller.muted = !controller.muted;
                    ValidateView();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    controller.volume = controller.volume + 0.05f;
                    ValidateView();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    controller.volume = controller.volume - 0.05f;
                    ValidateView();
                }

                if (privilege)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        if(core.isPlaying && !core.isLive && !core.isError)
                        {
                            if (core.paused)
                                controller.PauseOn();
                            else
                                controller.PauseOff();
                            ValidateView();
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    if(core.isPlaying || (core.isError && core.errorRetry == 0))
                        controller.Reload();
                }

                if (privilege)
                {
                    if (Input.GetKeyDown(KeyCode.L))
                    {
                        if (core.loop)
                            controller.LoopOff();
                        else
                            controller.LoopOn();
                        ValidateView();
                    }
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    _canvasPIP.SetActive(!_canvasPIP.activeSelf);
                    _canvasFullScreen.SetActive(false);
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    _canvasPIP.SetActive(false);
                    _canvasFullScreen.SetActive(!_canvasFullScreen.activeSelf);
                }

                if (privilege)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        if(core.isPlaying && !core.isLive && !core.isError)
                        {
                            controller.Backward();
                            ValidateView();
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        if (core.isPlaying && !core.isLive && !core.isError)
                        {
                            controller.Forward();
                            ValidateView();
                        }
                    }
                }
            }
            if (/*Input.GetKeyUp(KeyCode.LeftControl) || */Input.GetKeyUp(KeyCode.RightControl))
            {
                _canvas2.SetActive(false);
                _addressInput.DeactivateInputField();

                _canvas1.SetActive(false);
            }
        }

        void ModeVideo()
        {
            Debug.Log($"[iwaSync3] The mode has changed to `MODE_VIDEO`.");
            _mode =
                #if COMPILER_UDONSHARP
                core.MODE_VIDEO
                #else
                VideoCore.MODE_VIDEO
                #endif
            ;
        }

        void ModeLive()
        {
            Debug.Log($"[iwaSync3] The mode has changed to `MODE_STREAM`.");
            _mode =
                #if COMPILER_UDONSHARP
                core.MODE_STREAM
                #else
                VideoCore.MODE_STREAM
                #endif
            ;
        }

        void OnURLChanged()
        {
            var url = _addressInput.GetUrl();
            if (!core.IsValidURL(url.Get()))
                return;
            Debug.Log($"[iwaSync3] The url has changed to `{_addressInput.GetUrl().Get()}`.");

            core.TakeOwnership();
            if (core.IsRTSP(url.Get()))
                ModeLive();
            core.PlayURL(_mode, url);
            core.RequestSerialization();
            ValidateView();
        }
    }
}
