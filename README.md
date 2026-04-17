# Asynaprous Chat — C# Port

A C#/.NET port of the original asynchronous chat project.

This repo includes:

* a **web chat UI** served by ASP.NET Core
* a **console peer client** for terminal-based testing

---

## Project Structure

```text
AsynaprousChat-CSharp
├─ ChatTrackerApi       # ASP.NET Core backend + browser UI
├─ ChatPeerClient       # Console peer client
└─ README.md
```

---

## Requirements

* **.NET 8 SDK**
* A modern browser for the web UI

Check your .NET version:

```bash
dotnet --version
```

---

## Quick Start

### Run the Web UI

```bash
dotnet run --project ChatTrackerApi
```

Open:

```text
http://localhost:5000/chat.html
```

### Demo Accounts

* `alice / alice123`
* `user1 / password`
* `admin / admin123`

Open two browser windows or tabs and sign in with different accounts to test channel chat and direct messages.

---

## Console Client

Start the tracker API first:

```bash
dotnet run --project ChatTrackerApi
```

Then run two clients in separate terminals:

```bash
dotnet run --project ChatPeerClient -- alice alice123 127.0.0.1 7001 http://localhost:5000 general
```

```bash
dotnet run --project ChatPeerClient -- user1 password 127.0.0.1 7002 http://localhost:5000 general
```

### Client Arguments

```text
1. username
2. password
3. listenHost
4. listenPort
5. trackerBaseUrl
6. channel (optional, default: general)
```

### Available Commands

* `/help`
* `/peers`
* `/connect <peerId>`
* `/join <channel>`
* `/broadcast <message>`
* `/dm <peerId> <message>`
* `/history [channel]`
* `/quit`

---

## Running on Another Device

To access the web UI from another device on the same Wi-Fi/LAN, run:

```bash
dotnet run --project ChatTrackerApi --urls "http://0.0.0.0:5000"
```

Find your local IP address.

### Windows

```bash
ipconfig
```

Then open this on another device:

```text
http://YOUR_PC_IP:5000/chat.html
```

Example:

```text
http://192.168.0.101:5000/chat.html
```

> Do not use `localhost` on another device.

---

## Main Endpoints

### Auth / users

* `POST /login`
* `GET /whoami`
* `GET /users`

### Channels / peers

* `POST /submit-info`
* `GET /get-list`
* `POST /add-list`
* `POST /connect-peer`

### Messaging

* `POST /broadcast-peer`
* `POST /send-peer`
* `POST /web/send-dm`
* `GET /web/dm-messages?user=alice`
* `GET /messages?channel=general`

---

## Notes

* The default channel is **`#general`**
* New channels can be created from the web UI
* The current version stores data **in memory**, so restarting the server resets users, channels, and messages
* The web version uses the tracker API rather than raw browser peer-to-peer sockets

---

## Troubleshooting

### Port 5000 is already in use

Find the process:

```bash
netstat -ano | findstr :5000
```

Kill it by PID:

```bash
taskkill /PID <PID> /F
```

Or run on another port:

```bash
dotnet run --project ChatTrackerApi --urls "http://0.0.0.0:5001"
```

### Browser still shows the old UI

Do a hard refresh:

* `Ctrl + F5`

### Another device cannot connect

Common causes:

* the server is only running on `localhost`
* Windows Firewall is blocking the port
* the device is not on the same network

---

## Suggested Demo Flow

1. Start `ChatTrackerApi`
2. Open `http://localhost:5000/chat.html` in two browser windows
3. Log in as `alice` and `user1`
4. Send a message in `#general`
5. Test a direct message
6. Create and join a new channel
