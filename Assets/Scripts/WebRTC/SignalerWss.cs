using System;
using Cysharp.Threading.Tasks;
using NativeWebSocket;
using UniRx;
using Unity.WebRTC;
using UnityEngine;

namespace WebRTC
{
    public class SignalerWss : ISignaler, IDisposable

    {
        #region WSS

        private readonly WebSocket _webSocket;
        private readonly CompositeDisposable _compositeDisposable = new();

        public SignalerWss(string url)
        {
            _webSocket = new WebSocket(url);

            Observable.FromEvent<WebSocketMessageEventHandler, byte[]>(
                    h => h.Invoke,
                    h => _webSocket.OnMessage += h,
                    h => _webSocket.OnMessage -= h
                ).Subscribe(OnReceiveMessage)
                .AddTo(_compositeDisposable);

            Observable.EveryUpdate()
                .Subscribe(_ => _webSocket.DispatchMessageQueue())
                .AddTo(_compositeDisposable);
        }

        public UniTask Connect()
        {
            return _webSocket.Connect().AsUniTask();
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
            _receiveOffer?.Dispose();
            _receiveAnswer?.Dispose();
            _receiveIce?.Dispose();

            _webSocket?.Close();
        }

        #endregion

        #region Message

        public enum MessageType : byte
        {
            Offer = 1,
            Answer = 2,
            Ice = 3,
        }

        private void OnReceiveMessage(byte[] message)
        {
            var t = message[message.Length - 1];
            Array.Resize(ref message, message.Length - 1);
            string s;

            switch (t)
            {
                case (byte)MessageType.Offer:
                    s = System.Text.Encoding.UTF8.GetString(message);
                    _receiveOffer?.OnNext(SessionDescriptionToJson.FromJson(s).To());
                    break;
                case (byte)MessageType.Answer:
                    s = System.Text.Encoding.UTF8.GetString(message);
                    _receiveAnswer?.OnNext(SessionDescriptionToJson.FromJson(s).To());
                    break;
                case (byte)MessageType.Ice:
                    s = System.Text.Encoding.UTF8.GetString(message);
                    // _receiveOffer?.OnNext(SessionDescriptionToJson.FromJson(s).To());
                    break;
                default:
                    return;
            }
        }

        public UniTask Send(MessageType type, string json)
        {
            var s = System.Text.Encoding.UTF8.GetBytes(json);
            Array.Resize(ref s, s.Length + 1);
            s[s.Length - 1] = (byte)type;
            return _webSocket.Send(s).AsUniTask();
        }

        #endregion

        #region ISignaler

        public UniTask SendOffer(RTCSessionDescription description)
        {
            Debug.Log($"{nameof(SignalerWss)} {nameof(SendOffer)} {description}");
            return Send(MessageType.Offer, description.From().ToJson());
        }

        public UniTask SendAnswer(RTCSessionDescription description)
        {
            Debug.Log($"{nameof(SignalerWss)} {nameof(SendAnswer)} {description}");
            return Send(MessageType.Answer, description.From().ToJson());
        }

        public async UniTask SendIce(RTCIceCandidate candidate)
        {
            Debug.Log($"{nameof(SignalerWss)} {nameof(SendIce)} {candidate}");
        }

        private readonly Subject<RTCSessionDescription> _receiveOffer = new();
        public IObservable<RTCSessionDescription> ReceiveOffer => _receiveOffer;

        private readonly Subject<RTCSessionDescription> _receiveAnswer = new();
        public IObservable<RTCSessionDescription> ReceiveAnswer => _receiveAnswer;

        private readonly Subject<RTCIceCandidate> _receiveIce = new();
        public IObservable<RTCIceCandidate> ReceiveIce => _receiveIce;

        #endregion
    }
}
