# ARWithWebRTC

[ARFoundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.2/manual/index.html)の画面を[WebRTC](https://docs.unity3d.com/ja/Packages/com.unity.webrtc@2.4/manual/index.html)
で配信し、映像を受信した側でDataChannelを用いてARFoundationを操作するサンプルです。

[詳しくはこちら](https://zenn.dev/nekomimi_daimao/articles/fd27aba5c96b66)

## バージョン

| Platform | Version |
| ---- | ---- |
| Unity | 2021.3.8 |
| ARFoundation | 4.2.3 |
| WebRTC | 2.4.0-exp.8 |

## 起動

この順番で起動してください。

1. WSSServer
2. Receive Scene
3. AR Scene

### WSServer

`/wssServer`

シグナリングのためのWebSocketServerです。  
`node.js`で起動します。

```shell
cd wssServer
npm start
```

### Receive Scene

[AR Scene](#ar-scene)から送信されてくる映像を受信するシーンです。

以下のシーンを起動します。  
`Assets/Scenes/ReceiveScene.unity`

[WSS Server](#WSServer)のアドレスを入力して接続します。  
`ws://192.168.XXX.XXX:5555`

### AR Scene

映像を配信するシーンです。  
[Plane Detection](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/plane-manager.html)をしています。

以下のシーンをビルドします。  
`Assets/Scenes/ReceiveScene.unity`

[WSS Server](#WSServer)のアドレスを入力して接続します。  
`ws://192.168.XXX.XXX:5555`

---

起動に成功すればReceive SceneにAR Sceneの画面が表示されます。

## 操作

Receive Sceneの画面をタップすると、DataChannelを用いてAR Sceneに座標が送信されて`Cube`が発射されます。
