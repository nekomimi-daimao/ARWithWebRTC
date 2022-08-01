using System;
using Cysharp.Threading.Tasks;
using UniRx;
using Unity.WebRTC;
using UnityEngine;
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
        private readonly CompositeDisposable _compositeDisposable = new();

        public PeerController(bool offerSender, ISignaler signaler, MediaStreamTrack[] track, string[] channel)
        {
            _offerSender = offerSender;
            _signaler = signaler;
            PeerConnection = new RTCPeerConnection(ref Config);

            SubscribeCallbacks();
            SubscribeReceive();

            foreach (var streamTrack in track)
            {
                PeerConnection.AddTrack(streamTrack);
            }

            foreach (var c in channel)
            {
                PeerConnection.CreateDataChannel(c);
            }
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
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

        #endregion

        #region Receive

        private void SubscribeReceive()
        {
            _signaler.ReceiveOffer.Subscribe(ReceiveOffer).AddTo(_compositeDisposable);
            _signaler.ReceiveAnswer.Subscribe(ReceiveAnswer).AddTo(_compositeDisposable);
            _signaler.ReceiveIce.Subscribe(ReceiveIce).AddTo(_compositeDisposable);
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
            var candidate = PeerConnection.AddIceCandidate(iceCandidate);
            if (!candidate)
            {
                Debug.LogError($"{nameof(PeerController)} {nameof(ReceiveIce)} add ice failed");
            }
        }

        #endregion

        #region Callback

        private void SubscribeCallbacks()
        {
            Observable.FromEvent<DelegateOnNegotiationNeeded>(
                h => h.Invoke,
                h => PeerConnection.OnNegotiationNeeded += h,
                h => PeerConnection.OnNegotiationNeeded -= h
            ).Subscribe(OnNegotiationNeeded).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnIceCandidate, RTCIceCandidate>(
                h => h.Invoke,
                h => PeerConnection.OnIceCandidate += h,
                h => PeerConnection.OnIceCandidate -= h
            ).Subscribe(OnIceCandidate).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnDataChannel, RTCDataChannel>(
                h => h.Invoke,
                h => PeerConnection.OnDataChannel += h,
                h => PeerConnection.OnDataChannel -= h
            ).Subscribe(OnDataChannel).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnTrack, RTCTrackEvent>(
                h => h.Invoke,
                h => PeerConnection.OnTrack += h,
                h => PeerConnection.OnTrack -= h
            ).Subscribe(OnTrack).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnConnectionStateChange, RTCPeerConnectionState>(
                h => h.Invoke,
                h => PeerConnection.OnConnectionStateChange += h,
                h => PeerConnection.OnConnectionStateChange -= h
            ).Subscribe(OnConnectionStateChange).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnIceConnectionChange, RTCIceConnectionState>(
                h => h.Invoke,
                h => PeerConnection.OnIceConnectionChange += h,
                h => PeerConnection.OnIceConnectionChange -= h
            ).Subscribe(OnIceConnectionChange).AddTo(_compositeDisposable);

            Observable.FromEvent<DelegateOnIceGatheringStateChange, RTCIceGatheringState>(
                h => h.Invoke,
                h => PeerConnection.OnIceGatheringStateChange += h,
                h => PeerConnection.OnIceGatheringStateChange -= h
            ).Subscribe(OnIceGatheringStateChange).AddTo(_compositeDisposable);
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
            Debug.Log($"{nameof(PeerController)} {nameof(OnDataChannel)} {channel}");
        }

        private void OnTrack(RTCTrackEvent trackEvent)
        {
            Debug.Log($"{nameof(PeerController)} {nameof(OnTrack)} {trackEvent}");
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
