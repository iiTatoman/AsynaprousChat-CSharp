# Asynaprous Chat (C#)

It has two parts:

- `ChatTrackerApi`: ASP.NET Core backend + browser UI
- `ChatPeerClient`: console client for socket/peer testing

## Folder layout

```text
AsynaprousChat-CSharp
├─ ChatTrackerApi
├─ ChatPeerClient
└─ README.md
```

## Run the web version

From the repo root:

```bash
dotnet run --project ChatTrackerApi
```

Open:

```text
http://localhost:5000/chat.html
```

Demo accounts:

- `alice / alice123`
- `user1 / password`
- `admin / admin123`

Open two browser tabs or windows and sign into different accounts to test chat between users.

## Run the console clients

Start the API first:

```bash
dotnet run --project ChatTrackerApi
```

Then in two separate terminals:

```bash
dotnet run --project ChatPeerClient -- alice alice123 127.0.0.1 7001 http://localhost:5000 general
```

```bash
dotnet run --project ChatPeerClient -- user1 password 127.0.0.1 7002 http://localhost:5000 general
```

Argument order:

```text
username password listenHost listenPort trackerBaseUrl channel
```

The last argument is optional. If omitted, it uses `general`.

Useful console commands:

```text
/help
/peers
/connect <peerId>
/join <channel>
/broadcast <message>
/dm <peerId> <message>
/history [channel]
/quit
```

## Access from another device on the same network

To expose the web UI to other devices on the same LAN, start the API with:

```bash
dotnet run --project ChatTrackerApi --urls "http://0.0.0.0:5000"
```

Then open:

```text
http://<your-local-ip>:5000/chat.html
```

Example:

```text
http://192.168.0.101:5000/chat.html
```

Make sure the other device is on the same network. `localhost` only works on the machine running the server.

## Main endpoints

```text
POST /login
GET  /whoami
GET  /users

POST /submit-info
GET  /get-list
POST /add-list
POST /connect-peer

POST /broadcast-peer
POST /send-peer
POST /web/send-dm
GET  /web/dm-messages?user=alice
GET  /messages?channel=general
```

## Notes

This is a demo app, not a production chat server.

A few things to know:

- data is kept in memory
- restarting the API clears users, channels, and messages
- `#general` is the default channel
- you can create extra channels from the web UI
- the browser version goes through the tracker API

### Another device cannot connect

Usually one of these:

- the API is still running on `localhost` only
- Windows Firewall blocked the port
- the other device is not on the same network
- you are using the wrong local I.
