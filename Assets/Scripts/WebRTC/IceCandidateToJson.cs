using System;
using Unity.WebRTC;
using UnityEngine;

namespace WebRTC
{
    [Serializable]
    public class IceCandidateToJson
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;

        public static IceCandidateToJson From(RTCIceCandidate iceCandidate)
        {
            return new IceCandidateToJson
            {
                candidate = iceCandidate.Candidate,
                sdpMid = iceCandidate.SdpMid,
                sdpMLineIndex = iceCandidate.SdpMLineIndex ?? 0,
            };
        }

        public RTCIceCandidate To()
        {
            var init = new RTCIceCandidateInit
            {
                candidate = this.candidate,
                sdpMid = this.sdpMid,
                sdpMLineIndex = this.sdpMLineIndex,
            };
            return new RTCIceCandidate(init);
        }

        public static IceCandidateToJson FromJson(string json)
        {
            return JsonUtility.FromJson<IceCandidateToJson>(json);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    // ReSharper disable once InconsistentNaming
    public static class ExtensionRTCIceCandidate
    {
        public static IceCandidateToJson From(this RTCIceCandidate iceCandidate)
        {
            return IceCandidateToJson.From(iceCandidate);
        }
    }
}
