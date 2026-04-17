# Asynaprous Chat — C# Port

This is a C# reimplementation of the original Python **Asynaprous Chat** app.

## Architecture

The original Python project combined:
- a tracker server (HTTP endpoints)
- peer registration and peer discovery
- peer-to-peer messaging over TCP
- async/non-blocking communication
- a browser-style chat UI

This C# port now includes **both**:
- **ChatTrackerApi**: ASP.NET Core Minimal API that manages login, session token, peer registry, channels, and message history
- **ChatPeerClient**: console peer client using `HttpClient`, `TcpListener`, and `TcpClient` with `async/await`
- **Web UI (`chat.html`)**: browser-based frontend served by the tracker API for channel chat and direct messages

## Main Endpoints

- `POST /login`
- `GET /whoami`
- `GET /users`
- `POST /submit-info`
- `GET /get-list`
- `POST /add-list`
- `POST /connect-peer`
- `POST /broadcast-peer`
- `POST /send-peer`
- `POST /web/send-dm`
- `GET /web/dm-messages?user=alice`
- `GET /messages?channel=general`

## Suggested CV wording

**Asynchronous Chat Application (C#)**
- Ported a hybrid chat system from Python to **C#**, combining an **ASP.NET Core** tracker API with **TCP-based peer-to-peer messaging** and a **browser-based chat interface**.
- Implemented **user login, session-based authentication, peer discovery, channel membership, direct messaging, and broadcast messaging**.
- Used **async/await**, `TcpListener`, `TcpClient`, and thread-safe in-memory collections to support concurrent communication.
- Designed a tracker-plus-peer architecture where the central server manages discovery and fallback delivery while peers communicate directly in real time.

## Run notes

These projects target **.NET 8**.

### Option A — Run the web version (closest to your Python chat.html flow)

#### 1. Run the tracker API
```bash
dotnet run --project ChatTrackerApi
```
Default URL:
- `http://localhost:5000`

#### 2. Open the browser UI
Open:
- `http://localhost:5000/chat.html`

You can log in with:
- `alice / alice123`
- `user1 / password`
- `admin / admin123`

To simulate two users, open:
- one normal browser window
- one incognito/private window

Then log into different accounts and test:
- channel messages
- direct messages
- joining a new channel

### Option B — Run the console peer clients

#### 1. Run the tracker API
```bash
dotnet run --project ChatTrackerApi
```

#### 2. Run a peer client
Example terminal 1:
```bash
dotnet run --project ChatPeerClient -- alice alice123 127.0.0.1 7001 http://localhost:5000 general
```

Example terminal 2:
```bash
dotnet run --project ChatPeerClient -- user1 password 127.0.0.1 7002 http://localhost:5000 general
```

Arguments:
1. username
2. password
3. listenHost
4. listenPort
5. trackerBaseUrl
6. channel (optional, default: general)

### Available peer commands
- `/help`
- `/peers`
- `/connect <peerId>`
- `/join <channel>`
- `/broadcast <message>`
- `/dm <peerId> <message>`
- `/history [channel]`
- `/quit`

## Notes

- The **browser UI** uses the tracker API for real-time-ish chat via polling.
- Raw TCP peer-to-peer messaging remains available in the **console client**.
- Data is stored in memory to keep the project interview-friendly and easy to explain.
- You can later extend it with **SQLite**, **SignalR**, or a **WPF/WinForms UI** if you want a stronger C# showcase.


## Browser multi-user note
This web UI stores login per browser tab/window using sessionStorage and sends Basic Authorization headers, so you can open two normal browser windows/tabs on the same URL and sign in as different users.
