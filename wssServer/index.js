import {WebSocketServer, WebSocket} from "ws";
import ip from "ip";

const port = 5555;
const server = new WebSocketServer({port: port});

server.on('connection', function connection(ws, req) {
    const remoteAddress = req.socket.remoteAddress;
    console.log(`connection ${remoteAddress}`);

    ws.on('open', function open() {
        console.log(`open ${remoteAddress}`);
    });
    ws.on('close', function close() {
        console.log(`close ${remoteAddress}`);
    });

    ws.on('message', function message(data, isBinary) {
        server.clients.forEach(function each(client) {
            if (client !== ws && client.readyState === WebSocket.OPEN) {
                client.send(data, {binary: isBinary});
            } else if (client === ws) {
                console.log(`message from ${client._socket.remoteAddress}`);
            }
        });
    });
});

console.log(`${ip.address()}:${port}`);
