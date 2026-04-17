# Asynaprous Chat (C#)

It has two parts:

- `ChatTrackerApi`: ASP.NET Core backend + browser UI
- `ChatPeerClient`: console client for socket/peer testing

## Requirements

- .NET 8 SDK

Check it:

```bash
dotnet --version
```

## Folder layout

```text
AsynaprousChat-CSharp
├─ ChatTrackerApi
├─ ChatPeerClient
└─ README.md
```

## Running the web app

From the repo root:

```bash
dotnet run --project ChatTrackerApi
```

Then open:

```text
http://localhost:5000/chat.html
```

Demo accounts:

- `alice / alice123`
- `user1 / password`
- `admin / admin123`

Open two browser tabs or windows and log into different accounts if you want to test chat between users.

The app keeps login state per tab/window, so you do not need Incognito mode.

## Running the console clients

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

The last argument is optional. If you leave it out, it uses `general`.

Useful commands in the console client:

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

## Running it from another device on the same network

If you want to open the web UI from your phone or another laptop on the same Wi‑Fi, run the API like this:

```bash
dotnet run --project ChatTrackerApi --urls "http://0.0.0.0:5000"
```

Find your PC's local IP:

```bash
ipconfig
```

Then open this on the other device:

```text
http://YOUR_PC_IP:5000/chat.html
```

Example:

```text
http://192.168.0.101:5000/chat.html
```

Do not use `localhost` on another device. That points back to that device, not your PC.

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

## Common issues

### Port 5000 is already in use

Find what is using it:

```bash
netstat -ano | findstr :5000
```

Kill the process by PID:

```bash
taskkill /PID <PID> /F
```

Or just run on another port:

```bash
dotnet run --project ChatTrackerApi --urls "http://0.0.0.0:5001"
```

Then open:

```text
http://localhost:5001/chat.html
```

### Browser still shows the old UI

Hard refresh:

```text
Ctrl + F5
```

If that still does not work, close the tab and open it again.

### Another device cannot connect

Usually one of these:

- the API is still running on `localhost` only
- Windows Firewall blocked the port
- the other device is not on the same network
- you are using the wrong local IP

## Quick smoke test

1. Run `ChatTrackerApi`
2. Open `http://localhost:5000/chat.html`
3. Log in as `alice`
4. Open a second tab/window
5. Log in as `user1`
6. Send a message in `#general`
7. Click a user and try a DM
