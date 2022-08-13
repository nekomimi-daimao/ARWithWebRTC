using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.WebRTC;
using UnityEngine;
using WebRTC.Extension;
using WebRTC.Signaler;

namespace WebRTC
{
    public class PeerController : IDisposable
    {
        private static RTCConfiguration Config = new()
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        public RTCPeerConnection PeerConnection { get; }

        private readonly bool _offerSender;
        private readonly ISignaler _signaler;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CompositeDisposable _compositeDisposable = new();
        public MediaStream ReceiveMediaStream { get; } = new();
        public ReactiveDictionary<string, RTCDataChannel> DataChannels { get; } = new();

        public PeerController(bool offerSender, ISignaler signaler, MediaStream[] trackStreams, string[] channel)
        {
            _offerSender = offerSender;
            _signaler = signaler;
            PeerConnection = new RTCPeerConnection(ref Config);

            SubscribeCallbacks();
            SubscribeReceive();
            AddCandidateQueue(_cancellationTokenSource.Token).Forget();
            DataChannels.ObserveRemove()
                .Subscribe(e =>
                {
                    e.Value.Close();
                    e.Value.Dispose();
                }).AddTo(_compositeDisposable);
            DataChannels.AddTo(_compositeDisposable);
            _cancellationTokenSource.AddTo(_compositeDisposable);

            AddTrack(trackStreams);

            var dataChannelInit = new RTCDataChannelInit();
            foreach (var c in channel)
            {
                var createdChannel = PeerConnection.CreateDataChannel(c, dataChannelInit);
                createdChannel.OnOpenAsObservable()
                    .Subscribe(_ =>
                        DataChannels[c] = createdChannel)
                    .AddTo(_compositeDisposable);
            }
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
            _iceCandidatesQueue?.CompleteAdding();
            _iceCandidatesQueue?.Dispose();
            PeerConnection?.Close();
            PeerConnection?.Dispose();
        }

        #region Action

        public async UniTask CreateOffer()
        {
            var offer = PeerConnection.CreateOffer();
            await offer;
            if (offer.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(CreateOffer)} {offer.Error}");
                return;
            }

            var desc = offer.Desc;
            var setLocal = PeerConnection.SetLocalDescription(ref desc);
            await setLocal;
            if (setLocal.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(CreateOffer)} {setLocal.Error}");
            }

            _signaler.SendOffer(desc).Forget();
        }

        public async UniTask CreateAnswer(RTCSessionDescription offerDesc)
        {
            if (offerDesc.type != RTCSdpType.Offer)
            {
                return;
            }

            var setRemote = PeerConnection.SetRemoteDescription(ref offerDesc);
            await setRemote;
            if (setRemote.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(CreateAnswer)} {setRemote.Error}");
            }

            var answer = PeerConnection.CreateAnswer();
            await answer;
            if (answer.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(CreateAnswer)} {answer.Error}");
            }

            var answerDesc = answer.Desc;
            var setLocal = PeerConnection.SetLocalDescription(ref answerDesc);
            await setLocal;
            if (setLocal.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(CreateAnswer)} {setLocal.Error}");
            }

            _signaler.SendAnswer(answerDesc).Forget();
        }

        private void AddTrack(MediaStream[] trackStreams)
        {
            foreach (var s in trackStreams)
            {
                foreach (var mediaStreamTrack in s.GetTracks())
                {
                    PeerConnection.AddTrack(mediaStreamTrack, s);
                }
            }
        }

        #endregion

        #region Receive

        private void SubscribeReceive()
        {
            _signaler.ReceiveOffer.Subscribe(ReceiveOffer).AddTo(_compositeDisposable);
            _signaler.ReceiveAnswer.Subscribe(ReceiveAnswer).AddTo(_compositeDisposable);
            _signaler.ReceiveIce
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(ReceiveIce).AddTo(_compositeDisposable);
            ReceiveMediaStream.AddTo(_compositeDisposable);
        }

        private void ReceiveOffer(RTCSessionDescription desc)
        {
            CreateAnswer(desc).Forget();
        }

        private async void ReceiveAnswer(RTCSessionDescription desc)
        {
            var answer = PeerConnection.SetRemoteDescription(ref desc);
            await answer;
            if (answer.IsError)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(ReceiveAnswer)} {answer.Error}");
            }
        }

        private void ReceiveIce(RTCIceCandidate iceCandidate)
        {
            _iceCandidatesQueue.Add(iceCandidate);
        }

        private readonly BlockingCollection<RTCIceCandidate> _iceCandidatesQueue = new BlockingCollection<RTCIceCandidate>();

        private async UniTaskVoid AddCandidateQueue(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await UniTask.SwitchToThreadPool();
                    var candidate = _iceCandidatesQueue.Take(token);
                    var addResult = PeerConnection.AddIceCandidate(candidate);
                    if (!addResult)
                    {
                        Debug.LogError($"{nameof(PeerController)} {nameof(ReceiveIce)} add ice failed");
                    }
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException || e is OperationCanceledException || e is InvalidOperationException)
                    {
                        break;
                    }

                    Debug.LogException(e);
                }
            }
        }

        #endregion

        #region Callback

        private void SubscribeCallbacks()
        {
            PeerConnection.OnNegotiationNeededAsObservable()
                .Subscribe(OnNegotiationNeeded).AddTo(_compositeDisposable);

            PeerConnection.OnIceCandidateAsObservable()
                .Subscribe(OnIceCandidate).AddTo(_compositeDisposable);

            PeerConnection.OnDataChannelAsObservable()
                .Subscribe(OnDataChannel).AddTo(_compositeDisposable);

            PeerConnection.OnTrackAsObservable()
                .Subscribe(OnTrack).AddTo(_compositeDisposable);

            PeerConnection.OnConnectionStateChangeAsObservable()
                .Subscribe(OnConnectionStateChange).AddTo(_compositeDisposable);

            PeerConnection.OnIceConnectionChangeAsObservable()
                .Subscribe(OnIceConnectionChange).AddTo(_compositeDisposable);

            PeerConnection.OnIceGatheringStateChangeAsObservable()
                .Subscribe(OnIceGatheringStateChange).AddTo(_compositeDisposable);
        }

        private void OnNegotiationNeeded(Unit _)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnNegotiationNeeded)}");
            if (_offerSender)
            {
                CreateOffer().Forget();
            }
        }

        private void OnIceCandidate(RTCIceCandidate iceCandidate)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnIceCandidate)} {iceCandidate.Candidate}");
            _signaler.SendIce(iceCandidate).Forget();
        }

        private void OnDataChannel(RTCDataChannel channel)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnDataChannel)} {channel.Label}");
            DataChannels[channel.Label] = channel;
            channel.OnCloseAsObservable().Subscribe(_ => { DataChannels.Remove(channel.Label); });
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnTrack)} {trackEvent}");
            ReceiveMediaStream.AddTrack(trackEvent.Track);
        }

        private void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnConnectionStateChange)} {state}");
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnIceConnectionChange)} {state}");
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnIceGatheringStateChange)} {state}");
        }

        #endregion
    }
}
