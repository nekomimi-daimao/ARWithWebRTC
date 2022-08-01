using System;
using Cysharp.Threading.Tasks;
using Unity.WebRTC;

namespace WebRTC.Signaler
{
    public interface ISignaler
    {
        UniTask SendOffer(RTCSessionDescription description);
        UniTask SendAnswer(RTCSessionDescription description);
        UniTask SendIce(RTCIceCandidate candidate);
        IObservable<RTCSessionDescription> ReceiveOffer { get; }
        IObservable<RTCSessionDescription> ReceiveAnswer { get; }
        IObservable<RTCIceCandidate> ReceiveIce { get; }
    }
}
