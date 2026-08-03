# SigmaChat — private internet chat rooms

SigmaChat is a lightweight real-time Windows chat app. People join with the same private room code and can chat across devices and networks.

Chat history and received images are saved locally in `%LOCALAPPDATA%\SigmaChat` and reload when the same room is opened again. Images are compressed before being relayed.

Room access uses a separate key prompt after selecting a room. Users can share normal images and general files up to 5 MB. Received files are never opened automatically and use a **Save As** button.

The message editor supports multiple lines: press **Shift+Enter** for a new line and **Enter** to send.

Right-click a message or image to delete it locally. Senders can also choose **Delete for everyone** while the server room remains active.

## Quick local test

1. Run `dotnet run --project server/SigmaGame.Server.csproj`.
2. Run `dotnet run --project client/SigmaGame.Client.csproj` twice.
3. In each app, temporarily use `ws://localhost:5050/ws`, the same room code and key, and different names.

The packaged app defaults to the deployed `wss://sigmachat-server.onrender.com/ws` server. No port forwarding is needed.

## Play over the internet

Deploy the `server` folder to any Docker host (Render, Railway, Fly.io, Azure Container Apps, etc.). The included `Dockerfile` listens on the provider's `PORT` environment variable. After deployment, players enter:

    wss://YOUR-HOST/ws

Use `wss://` for hosted HTTPS services. Everyone must enter the same room code and room key. The first person creates the room and becomes its initial owner. The server stores no accounts or personal data and room state is kept only in memory.

### Render example

Create a new **Web Service**, connect/upload this project, set the root directory to `server`, select **Docker**, and deploy. Use the resulting `https://...onrender.com` address as `wss://...onrender.com/ws` in the game.

Free hosts may sleep while idle; the first connection can take a minute. For production, use one server instance because rooms are in memory.

## Build `sigmagame.exe`

Run the following command with the free .NET 8 SDK:

    dotnet publish client/SigmaGame.Client.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

## Automatic updates

Push a version tag such as `v5.0.1`. The included GitHub Actions workflow creates a release containing `sigmagame.exe`. Version 5 and newer check this repository's latest release at startup and offer to install it.

The build is self-contained for 64-bit Windows, so the destination PC does not need .NET installed. Send the entire `release` folder if it contains supporting files (the current build aims for a single file).

Windows SmartScreen may warn about an unsigned personal build. Code-sign the executable before public distribution.

## Privacy notes

- Room messages exist only in server memory and are not saved to disk.
- Use a long, hard-to-guess room code and share it privately.
- Hosted connections must use `wss://` so messages are encrypted in transit.
- This version is not end-to-end encrypted: the server can technically read messages. Do not claim otherwise or use it for highly sensitive information.

## Architecture

- `client/`: native Windows chat interface
- `server/`: ASP.NET Core WebSocket chat relay
- Clients initiate outbound secure WebSocket connections; no player hosts a listening port.
- The server validates room/name input, verifies hashed room keys, limits message size, and authorizes sender-only global deletion.
