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

        private const string KeyAddress = nameof(KeyAddress);

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
            inputAddress.text = PlayerPrefs.GetString(KeyAddress);

            buttonConnect.interactable = !string.IsNullOrEmpty(inputAddress.text);

            buttonConnect.OnClickAsObservable()
                .Subscribe(TryConnect)
                .AddTo(_compositeDisposable);

            async void TryConnect(Unit _)
            {
                try
                {
                    await UniTask.SwitchToMainThread();
                    buttonConnect.interactable = false;
                    var url = inputAddress.text.Trim();

                    PlayerPrefs.SetString(KeyAddress, url);
                    PlayerPrefs.Save();

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

        #region DataChannel

        private ShareTapPoint _shareTapPoint;

        [SerializeField]
        private Rigidbody throwCubePrefab;

        private void ChannelCreated(RTCDataChannel channel)
        {
            _shareTapPoint = new ShareTapPoint(channel);
            var width = Screen.width;
            var height = Screen.height;
            var span = TimeSpan.FromSeconds(1);
            Observable.Interval(span)
                .Subscribe(_ =>
                {
                    _shareTapPoint.SendMessage(
                        ShareTapPoint.MessageType.ScreenSize,
                        (short)width, (short)height
                    );
                })
                .AddTo(_compositeDisposable);

            _shareTapPoint.OnTap
                .Subscribe(tuple => OnReceiveTap(tuple).Forget())
                .AddTo(_compositeDisposable);
        }

        private async UniTaskVoid OnReceiveTap((short, short) msg)
        {
            await UniTask.SwitchToMainThread();
            var (w, h) = msg;
            var screenToWorldPoint = arSessionOrigin.camera.ScreenToWorldPoint(
                new Vector3(
                    (w / 100f) * Screen.width,
                    (h / 100f) * Screen.height,
                    1f)
            );

            var forward = screenToWorldPoint - arSessionOrigin.camera.transform.position;
            forward.y = 0;
            forward = forward.normalized;
            var throwCube = GameObject.Instantiate(throwCubePrefab);
            throwCube.position = screenToWorldPoint;
            throwCube.rotation = UnityEngine.Random.rotation;
            throwCube.velocity = forward * 2 + Vector3.up;
            Destroy(throwCube.gameObject, 6f);
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

            var cs = arSessionOrigin.camera.CaptureStream(Screen.width, Screen.height, 1000000);
            _peer = new PeerController(
                true,
                _signaler,
                new[] { cs, },
                new[] { ShareTapPoint.ChannelName }
            );
            var targetTexture = arSessionOrigin.camera.targetTexture;
            arSessionOrigin.camera.targetTexture = null;

            _peer.DataChannels
                .ObserveAdd()
                .Where(e => string.Equals(e.Key, ShareTapPoint.ChannelName))
                .Select(e => e.Value)
                .Subscribe(ChannelCreated)
                .AddTo(_compositeDisposable);

            Observable.EveryUpdate()
                .Subscribe(_ => Graphics.Blit(null, targetTexture))
                .AddTo(_compositeDisposable);

            this.OnDestroyAsObservable()
                .Subscribe(_ =>
                {
                    Disconnect();
                    targetTexture.Release();
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
