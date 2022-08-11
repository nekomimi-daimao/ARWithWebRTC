using System;
using UniRx;
using Unity.WebRTC;

namespace WebRTC.Extension
{
    // ReSharper disable once InconsistentNaming
    public static class ExtensionRTCPeerConnection
    {
        public static IObservable<Unit> OnNegotiationNeededAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnNegotiationNeeded>(
                h => h.Invoke,
                h => peerConnection.OnNegotiationNeeded += h,
                h => peerConnection.OnNegotiationNeeded -= h);
        }

        public static IObservable<RTCIceCandidate> OnIceCandidateAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnIceCandidate, RTCIceCandidate>(
                h => h.Invoke,
                h => peerConnection.OnIceCandidate += h,
                h => peerConnection.OnIceCandidate -= h);
        }

        public static IObservable<RTCDataChannel> OnDataChannelAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnDataChannel, RTCDataChannel>(
                h => h.Invoke,
                h => peerConnection.OnDataChannel += h,
                h => peerConnection.OnDataChannel -= h
            );
        }

        public static IObservable<RTCTrackEvent> OnTrackAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnTrack, RTCTrackEvent>(
                h => h.Invoke,
                h => peerConnection.OnTrack += h,
                h => peerConnection.OnTrack -= h
            );
        }

        public static IObservable<RTCPeerConnectionState> OnConnectionStateChangeAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnConnectionStateChange, RTCPeerConnectionState>(
                h => h.Invoke,
                h => peerConnection.OnConnectionStateChange += h,
                h => peerConnection.OnConnectionStateChange -= h);
        }

        public static IObservable<RTCIceConnectionState> OnIceConnectionChangeAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnIceConnectionChange, RTCIceConnectionState>(
                h => h.Invoke,
                h => peerConnection.OnIceConnectionChange += h,
                h => peerConnection.OnIceConnectionChange -= h);
        }

        public static IObservable<RTCIceGatheringState> OnIceGatheringStateChangeAsObservable(this RTCPeerConnection peerConnection)
        {
            return Observable.FromEvent<DelegateOnIceGatheringStateChange, RTCIceGatheringState>(
                h => h.Invoke,
                h => peerConnection.OnIceGatheringStateChange += h,
                h => peerConnection.OnIceGatheringStateChange -= h);
        }
    }

    public static class ExtensionMediaStream
    {
        public static IObservable<MediaStreamTrackEvent> OnAddTrackAsObservable(this MediaStream mediaStream)
        {
            return Observable.FromEvent<DelegateOnAddTrack, MediaStreamTrackEvent>(
                h => h.Invoke,
                h => mediaStream.OnAddTrack += h,
                h => mediaStream.OnAddTrack -= h
            );
        }

        public static IObservable<MediaStreamTrackEvent> OnRemoveTrackAsObservable(this MediaStream mediaStream)
        {
            return Observable.FromEvent<DelegateOnRemoveTrack, MediaStreamTrackEvent>(
                h => h.Invoke,
                h => mediaStream.OnRemoveTrack += h,
                h => mediaStream.OnRemoveTrack -= h
            );
        }
    }
}
