using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;

namespace HoshinoLabs.Udon.IwaSync3
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class VideoScreen : UdonSharpBehaviour
    {
        [Header("Main")]
        [SerializeField]
        VideoCore core;

        [Header("Options")]
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

        Renderer _renderer;
        MaterialPropertyBlock _properties;

        private void Start()
        {
            Debug.Log($"[iwaSync3] Started `{nameof(VideoScreen)}`.");

            core.AddListener(this);

            _renderer = GetComponentInChildren<Renderer>();
            _properties = new MaterialPropertyBlock();
        }

        #region RoomEvent
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
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
        #endregion

        public void ValidateView()
        {
            _renderer.enabled = !idleScreenOff || core.isPlaying;
            var texture = idleScreenTexture;
            if (core.isPlaying)
            {
                if(core.texture == null)
                    SendCustomEventDelayedFrames(nameof(ValidateView), 0);
                else
                    texture = core.texture;
                _properties.SetInt("_IsAVProVideo", core.isModeVideo ? 0 : 1);
            }
            if (texture == null)
                _properties.Clear();
            else
                _properties.SetTexture(textureProperty, texture);
            _properties.SetFloat("_AspectRatio", aspectRatio);
            _renderer.SetPropertyBlock(_properties, materialIndex);
        }
    }
}
