using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebRTC;
using WebRTC.Extension;
using WebRTC.Signaler;

namespace Scene
{
    public sealed class ReceiveSceneManager : MonoBehaviour
    {
        private readonly CompositeDisposable _compositeDisposable = new CompositeDisposable();

        private void Awake()
        {
            Unity.WebRTC.WebRTC.Initialize();
            StartCoroutine(Unity.WebRTC.WebRTC.Update());
        }

        private void Start()
        {
            RegisterUI();
        }

        #region UI

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

        #region DataChannel

        private ShareTapPoint _shareTapPoint;

        private void ChannelCreated(RTCDataChannel channel)
        {
            _shareTapPoint = new ShareTapPoint(channel);
            receiveImage.OnPointerClickAsObservable()
                .Subscribe(OnTap)
                .AddTo(_compositeDisposable);

            _shareTapPoint.OnScreenSize
                .Subscribe(OnReceiveScreenSize)
                .AddTo(_compositeDisposable);
        }

        private void OnTap(PointerEventData data)
        {
            var point = receiveImage.rectTransform.InverseTransformPoint(data.position);
            _shareTapPoint.SendMessage(ShareTapPoint.MessageType.TapPoint, (short)point.x, (short)point.y);
        }

        private void OnReceiveScreenSize((short, short) msg)
        {
            var (w, h) = msg;
            var sizeDelta = receiveImage.rectTransform.sizeDelta;
            var ratio = (float)h / w;
            Debug.Log(ratio);
            var fixedH = sizeDelta.x * ratio;
            if (Mathf.Approximately(sizeDelta.y, fixedH))
            {
                return;
            }

            sizeDelta.y = fixedH;
            receiveImage.rectTransform.sizeDelta = sizeDelta;
        }

        #endregion

        #region WebRTC

        [SerializeField]
        private RawImage receiveImage;

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

            _peer = new PeerController(
                false,
                _signaler,
                Array.Empty<MediaStream>(),
                Array.Empty<string>()
            );

            _peer.DataChannels
                .ObserveAdd()
                .Where(e => string.Equals(e.Key, ShareTapPoint.ChannelName))
                .Select(e => e.Value)
                .Subscribe(ChannelCreated)
                .AddTo(_compositeDisposable);

            _peer.ReceiveMediaStream.OnAddTrackAsObservable()
                .Subscribe(e =>
                {
                    if (e.Track is VideoStreamTrack videoTrack)
                    {
                        videoTrack.OnVideoReceivedAsObservable().Subscribe(
                            texture =>
                            {
                                receiveImage.texture = texture;
                                receiveImage.color = Color.white;
                            }).AddTo(_compositeDisposable);
                    }
                }).AddTo(_compositeDisposable);

            this.OnDestroyAsObservable()
                .Subscribe(_ => { Disconnect(); });
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
