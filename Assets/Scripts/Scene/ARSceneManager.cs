using System;
using System.Text;
using System.Threading;
using AR;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using WebRTC;
using WebRTC.Signaler;

namespace Scene
{
    public sealed class ARSceneManager : MonoBehaviour
    {
        private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

        private void Awake()
        {
            Unity.WebRTC.WebRTC.Initialize();
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
        }

        private void Start()
        {
            this.OnDestroyAsObservable()
                .Subscribe(_ => _compositeDisposable.Dispose());

            Screen.orientation = ScreenOrientation.Portrait;

            Init(this.GetCancellationTokenOnDestroy()).Forget();
        }

        #region UI

        [Header("UI")]
        [SerializeField]
        private InputField inputAddress;

        [SerializeField]
        private Button buttonConnect;

        [SerializeField]
        private Text textStatus;

        private void RegisterUI()
        {
            Observable.EveryFixedUpdate()
                .SubscribeOnMainThread()
                .Subscribe(_ => ShowStatus())
                .AddTo(_compositeDisposable);

            inputAddress.OnValueChangedAsObservable()
                .Subscribe(s => buttonConnect.interactable = !string.IsNullOrEmpty(s))
                .AddTo(_compositeDisposable);

            buttonConnect.interactable = false;

            buttonConnect.OnClickAsObservable()
                .Subscribe(TryConnect)
                .AddTo(_compositeDisposable);

            async void TryConnect(Unit _)
            {
                try
                {
                    await UniTask.SwitchToMainThread();
                    buttonConnect.interactable = false;
                    var url = inputAddress.text;
                    await CreatePeerAsync(url);
                }
                finally
                {
                    buttonConnect.interactable = true;
                }
            }
        }

        private readonly StringBuilder _builder = new StringBuilder();

        private void ShowStatus()
        {
            _builder.Clear();

            if (_signaler != null)
            {
                _builder.AppendLine($"WSS connect {_signaler?.IsConnected}");
            }

            if (_peer?.PeerConnection != null)
            {
                _builder.AppendLine($"{nameof(_peer.PeerConnection.SignalingState)}:{_peer.PeerConnection.SignalingState}");
                _builder.AppendLine($"{nameof(_peer.PeerConnection.ConnectionState)}:{_peer.PeerConnection.ConnectionState}");
                _builder.AppendLine($"{nameof(_peer.PeerConnection.GatheringState)}:{_peer.PeerConnection.GatheringState}");
                _builder.AppendLine($"{nameof(_peer.PeerConnection.IceConnectionState)}:{_peer.PeerConnection.IceConnectionState}");
            }

            textStatus.text = _builder.ToString();
        }

        #endregion

        #region AR

        [Header("AR")]
        [SerializeField]
        private ARSessionOrigin arSessionOrigin;

        [SerializeField]
        private ARCameraManager arCameraManager;

        [SerializeField]
        private ARCameraBackground arCameraBackground;

        [SerializeField]
        private Camera renderingCamera;

        [SerializeField]
        private RawImage renderingBackground;

        private async UniTask Init(CancellationToken token)
        {
            var prepare = await Preparator.Check();
            if (!prepare)
            {
                textStatus.text = $"cannot launch ARFouncation!{Environment.NewLine}SHUTDOWN";
                await UniTask.Delay(TimeSpan.FromSeconds(6), cancellationToken: token);
                Application.Quit();
                return;
            }

            RegisterUI();
        }

        #endregion

        #region WebRTC

        private SignalerWss _signaler;
        private PeerController _peer;

        private async UniTask CreatePeerAsync(string url)
        {
            Disconnect();

            _signaler = new SignalerWss(url);
            await _signaler.Connect();
            if (!_signaler.IsConnected)
            {
                return;
            }

            var cs = renderingCamera.CaptureStream(Screen.width, Screen.height, 1000000);
            _peer = new PeerController(
                true,
                _signaler,
                new[] { cs, },
                new[] { ShareTapPoint.ChannelName }
            );

            var renderingTexture = new RenderTexture(renderingCamera.targetTexture);
            renderingBackground.texture = renderingTexture;
            Observable.FromEvent<ARCameraFrameEventArgs>(
                h => arCameraManager.frameReceived += h,
                h => arCameraManager.frameReceived -= h
            ).Subscribe(args =>
            {
                foreach (var argsTexture in args.textures)
                {
#if UNITY_ANDROID
                    Graphics.Blit(argsTexture, renderingTexture, arCameraBackground.material);
#elif UNITY_IOS
                    Graphics.Blit(argsTexture, renderingTexture);
#endif
                }
            }).AddTo(_compositeDisposable);

            this.OnDestroyAsObservable()
                .Subscribe(_ =>
                {
                    Disconnect();
                    renderingTexture.Release();
                });
        }

        private void Disconnect()
        {
            _signaler?.Dispose();
            _peer?.Dispose();
            _signaler = null;
            _peer = null;
        }

        #endregion
    }
}
