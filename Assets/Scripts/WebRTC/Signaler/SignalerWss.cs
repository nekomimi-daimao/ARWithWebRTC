using System;
using Cysharp.Threading.Tasks;
using NativeWebSocket;
using UniRx;
using Unity.WebRTC;
using UnityEngine;

namespace WebRTC.Signaler
{
    public class SignalerWss : ISignaler, IDisposable
    {
        #region WSS

        private readonly WebSocket _webSocket;
        public WebSocket WebSocket => _webSocket;

        public bool IsConnected => _webSocket.State == WebSocketState.Open;

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

            Observable.FromEvent<WebSocketOpenEventHandler>(
                    h => h.Invoke,
                    h => _webSocket.OnOpen += h,
                    h => _webSocket.OnOpen -= h
                )
                .Subscribe(OnOpen).AddTo(_compositeDisposable);

            Observable.FromEvent<WebSocketCloseEventHandler, WebSocketCloseCode>(
                h => h.Invoke,
                h => _webSocket.OnClose += h,
                h => _webSocket.OnClose -= h
            ).Subscribe(OnClose).AddTo(_compositeDisposable);

            Observable.FromEvent<WebSocketErrorEventHandler, string>(
                h => h.Invoke,
                h => _webSocket.OnError += h,
                h => _webSocket.OnError -= h
            ).Subscribe(OnError).AddTo(_compositeDisposable);

            Observable.EveryUpdate()
                .Subscribe(_ => _webSocket.DispatchMessageQueue())
                .AddTo(_compositeDisposable);
        }

        public async UniTask Connect()
        {
            // never return
            _webSocket.Connect().AsUniTask().Forget();

            bool IsConnecting() => _webSocket.State == WebSocketState.Connecting;
            // *** -> Connecting
            await UniTask.WaitUntil(IsConnecting);
            // Connecting -> ***
            await UniTask.WaitWhile(IsConnecting);
        }

        public void Dispose()
        {
            _webSocket?.Close();

            _compositeDisposable?.Dispose();
            _receiveOffer?.Dispose();
            _receiveAnswer?.Dispose();
            _receiveIce?.Dispose();
        }

        private void OnOpen(Unit _)
        {
            Debug.Log($"{nameof(SignalerWss)} WS {nameof(OnOpen)}");
        }

        private void OnClose(WebSocketCloseCode code)
        {
            Debug.Log($"{nameof(SignalerWss)} WS {nameof(OnClose)} {code}");
        }

        private void OnError(string error)
        {
            Debug.LogError($"{nameof(SignalerWss)} WS {nameof(OnError)} {error}");
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
            var s = System.Text.Encoding.UTF8.GetString(message);
            switch (t)
            {
                case (byte)MessageType.Offer:
                    _receiveOffer?.OnNext(SessionDescriptionToJson.FromJson(s).To());
                    break;
                case (byte)MessageType.Answer:
                    _receiveAnswer?.OnNext(SessionDescriptionToJson.FromJson(s).To());
                    break;
                case (byte)MessageType.Ice:
                    _receiveIce?.OnNext(IceCandidateToJson.FromJson(s).To());
                    break;
                default:
                    return;
            }
        }

        public UniTask SendMessage(MessageType type, string json)
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
            return SendMessage(MessageType.Offer, description.From().ToJson());
        }

        public UniTask SendAnswer(RTCSessionDescription description)
        {
            Debug.Log($"{nameof(SignalerWss)} {nameof(SendAnswer)} {description}");
            return SendMessage(MessageType.Answer, description.From().ToJson());
        }

        public UniTask SendIce(RTCIceCandidate candidate)
        {
            Debug.Log($"{nameof(SignalerWss)} {nameof(SendIce)} {candidate}");
            return SendMessage(MessageType.Ice, candidate.From().ToJson());
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
