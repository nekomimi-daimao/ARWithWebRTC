using System;
using UniRx;
using Unity.WebRTC;
using WebRTC.Extension;

namespace Scene
{
    public sealed class ShareTapPoint
    {
        public const string ChannelName = "stp";

        private readonly RTCDataChannel _dataChannel;

        public ShareTapPoint(RTCDataChannel dataChannel)
        {
            if (!string.Equals(ChannelName, dataChannel.Label))
            {
                return;
            }

            _dataChannel = dataChannel;

            _dataChannel
                .OnMessageAsObservable()
                .TakeUntil(dataChannel.OnCloseAsObservable())
                .Subscribe(ReceiveMessage);
        }

        public enum MessageType : byte
        {
            ScreenSize = 1,
            TapPoint = 2,
        }

        #region Send

        public void SendMessage(MessageType messageType, short width, short height)
        {
            if (_dataChannel.ReadyState != RTCDataChannelState.Open)
            {
                return;
            }

            var array = ShortToBytes(width, height);
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = (byte)messageType;
            _dataChannel.Send(array);
        }

        #endregion

        #region Receive

        private readonly Subject<(short, short)> _onScreenSize = new();
        public IObservable<(short, short)> OnScreenSize => _onScreenSize;
        private readonly Subject<(short, short)> _onTap = new();
        public IObservable<(short, short)> OnTap => _onTap;

        private void ReceiveMessage(byte[] message)
        {
            var t = message[message.Length - 1];
            Array.Resize(ref message, message.Length - 1);
            var sArray = ByteToShort(message);

            switch (t)
            {
                case (byte)MessageType.ScreenSize:
                    _onScreenSize.OnNext((sArray[0], sArray[1]));
                    break;
                case (byte)MessageType.TapPoint:
                    _onTap.OnNext((sArray[0], sArray[1]));
                    break;
                default:
                    return;
            }
        }

        #endregion

        #region Convert

        private static byte[] ShortToBytes(short width, short height)
        {
            var send = new byte[4];
            var wb = BitConverter.GetBytes(width);
            var hb = BitConverter.GetBytes(height);
            send[0] = wb[0];
            send[1] = wb[1];
            send[2] = hb[0];
            send[3] = hb[1];
            return send;
        }

        private static short[] ByteToShort(byte[] b)
        {
            var sArray = new short[b.Length / 2];
            for (var i = 0; i < b.Length / 2; i++)
            {
                sArray[i] = BitConverter.ToInt16(b, i * 2);
            }

            return sArray;
        }

        #endregion
    }
}
