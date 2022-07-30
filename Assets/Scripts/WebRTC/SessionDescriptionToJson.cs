using System;
using System.Collections.Generic;
using System.Linq;
using Unity.WebRTC;
using UnityEngine;

namespace WebRTC
{
    [Serializable]
    public class SessionDescriptionToJson
    {
        public string type;
        public string sdp;

        private static readonly Dictionary<RTCSdpType, string> Converter =
            Enum.GetValues(typeof(RTCSdpType))
                .Cast<RTCSdpType>()
                .ToDictionary(sdpType => sdpType, sdpType => sdpType.ToString().ToLower());

        public static SessionDescriptionToJson From(RTCSessionDescription sessionDescription)
        {
            return new SessionDescriptionToJson
            {
                type = Converter[sessionDescription.type],
                sdp = sessionDescription.sdp
            };
        }

        public RTCSessionDescription To()
        {
            return new RTCSessionDescription
            {
                type = Converter.FirstOrDefault(pair => string.Equals(pair.Value.ToLower(), this.type.ToLower())).Key,
                sdp = this.sdp,
            };
        }

        public static SessionDescriptionToJson FromJson(string json)
        {
            return JsonUtility.FromJson<SessionDescriptionToJson>(json);
        }
        
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    // ReSharper disable once InconsistentNaming
    public static class ExtensionRTCSessionDescription
    {
        public static SessionDescriptionToJson From(this RTCSessionDescription description)
        {
            return SessionDescriptionToJson.From(description);
        }
    }
}
