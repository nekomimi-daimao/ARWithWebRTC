using System;
using UniRx;
using Unity.WebRTC;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace WebRTC.Extension
{
    /// <summary>
    /// <see cref="RTCPeerConnection"/>
    /// </summary>
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

    /// <summary>
    /// <see cref="RTCDataChannel"/>
    /// </summary>
    public static class ExtensionRTCDataChannel
    {
        public static IObservable<Unit> OnOpenAsObservable(this RTCDataChannel dataChannel)
        {
            return Observable.FromEvent<DelegateOnOpen>(
                h => h.Invoke,
                h => dataChannel.OnOpen += h,
                h => dataChannel.OnOpen += h
            );
        }

        public static IObservable<Unit> OnCloseAsObservable(this RTCDataChannel dataChannel)
        {
            return Observable.FromEvent<DelegateOnClose>(
                h => h.Invoke,
                h => dataChannel.OnClose += h,
                h => dataChannel.OnClose += h
            );
        }

        public static IObservable<byte[]> OnMessageAsObservable(this RTCDataChannel dataChannel)
        {
            return Observable.FromEvent<DelegateOnMessage, byte[]>(
                h => h.Invoke,
                h => dataChannel.OnMessage += h,
                h => dataChannel.OnMessage += h
            );
        }
    }

    /// <summary>
    /// <see cref="MediaStream"/>
    /// </summary>
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

    /// <summary>
    /// <see cref="VideoStreamTrack"/>
    /// </summary>
    public static class ExtensionVideoStreamTrack
    {
        public static IObservable<Texture> OnVideoReceivedAsObservable(this VideoStreamTrack track)
        {
            return Observable.FromEvent<OnVideoReceived, Texture>(
                h => h.Invoke,
                h => track.OnVideoReceived += h,
                h => track.OnVideoReceived -= h
            );
        }
    }
}
