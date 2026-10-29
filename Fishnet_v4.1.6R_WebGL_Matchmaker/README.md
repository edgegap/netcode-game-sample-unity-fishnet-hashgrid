# FishNet on Edgegap, with an optional Edgegap Matchmaker (WebGL and desktop)

The SpaceEdge FishNet sample on Edgegap dedicated servers. **Start Game** has two modes, selected on the `MatchmakingSystem` object in the `MainMenu` scene:

- **Direct connection (default).** Connects to a server address and port you set. Test locally first, then against a deployment you start from the Edgegap dashboard. No matchmaker is needed.
- **Edgegap Matchmaker (opt-in).** Tick **Use Edgegap Matchmaker**. The [Edgegap Matchmaker](https://docs.edgegap.com/learn/matchmaking) groups players, Edgegap starts a dedicated server near them, and every client connects to it automatically.

In both modes the game client holds no Edgegap API token; matchmaker mode only uses the matchmaker's Auth Token, which is safe to ship in clients. The server stops its own Edgegap deployment once the match is empty.

Tested with Unity `2021.3.45f2`, FishNet `4.1.6R`, the Bayou WebSocket transport and Edgegap Unity SDK `3.5.5` with the POST fix that shipped in `3.5.6`. The project now pins SDK `3.5.6` (see [Edgegap SDK version](#edgegap-sdk-version)).

### Unity version

The project is on Unity `2021.3.45f2`, the editor it was tested with; open it with that exact version. Unity 2021.3 is no longer in Unity's support window. Newer editors (2022.3 LTS, Unity 6) are untested for this sample: Unity will offer to upgrade the project, and FishNet `4.1.6R`, the scenes and the build helpers have not been checked after such an upgrade.

## How it works

| Piece | File | Role |
|---|---|---|
| Menu bridge | `Assets/SpaceEdge/Scripts/Systems/MatchmakingSystem.cs` | Connects **Start Game** to a direct connection (default) or to the matchmaking client. With the matchmaker off, it disables the matchmaking client so the game never contacts a matchmaker. |
| Matchmaking client | `Assets/Edgegap/EdgegapMatchmakingClient.cs` | Matchmaker mode only. Creates a ticket (SDK Group Up flow, a group of one), polls it until `HOST_ASSIGNED`, then connects with `wss://{fqdn}:{external port}`. |
| Server lifecycle | `Assets/Edgegap/EdgegapServerLifecycle.cs` | In server builds, starts the FishNet server on `0.0.0.0:7770`. On Edgegap it reads the injected variables and stops the deployment when no one joins within 120 s or once everyone has left for 30 s. |
| Persistence | `Assets/Edgegap/EdgegapPersistence.cs` | Keeps the `Edgegap` object alive across the MainMenu → BattleScene → MainMenu scene changes. |
| Edgegap SDK | `Packages/manifest.json` | Git package from [edgegap-unity-sdk](https://github.com/edgegap/edgegap-unity-sdk), pinned to tag `3.5.6` (see below). |
| Edgegap plugin | `Packages/manifest.json` | The [Edgegap Unity plugin](https://github.com/edgegap/buildtool-unity-plugin) (`com.edgegap.unity-servers-plugin`, `#3.2.1`, Editor only): builds, containerizes, uploads and deploys the server from **Tools → Edgegap Server Hosting**. The project has no Dockerfile of its own; `.dockerignore` keeps the plugin's Docker context to `Builds/EdgegapServer/`. |
| Build helpers | `Assets/Edgegap/Editor/` | `EdgegapPluginBuild` runs the plugin's **Build server** from the command line, `EdgegapClientBuild` builds test clients (WebGL or desktop), plus scene setup. |

The server no longer relies on FishNet's **Start On Headless**: the lifecycle component starts it, so the same scene works for client and server builds without toggling anything.

### Edgegap SDK version

The SDK comes from its git repository instead of a copy in `Assets/`, pinned to a release tag so every checkout gets the same SDK:

```json
"com.edgegap.unity-sdk": "https://github.com/edgegap/edgegap-unity-sdk.git#3.5.6"
```

This sample needs SDK `3.5.6` or later. Before Unity 2022.3, SDK 3.5.5 and earlier send POST bodies through `UnityWebRequest.Post(url, string)`, which form-encodes the JSON, and the matchmaker answers every ticket request with HTTP 400. [edgegap/edgegap-unity-sdk#68](https://github.com/edgegap/edgegap-unity-sdk/pull/68) fixed this by sending the JSON raw on Unity before 2022.3 (`Runtime/SafeHttpRequest.cs`), and the fix shipped in 3.5.6.

To move to a newer SDK, change the tag in `Packages/manifest.json` to a newer release (not `main`, which can change under you) and test matchmaking again. Unity records the resolved commit in `Packages/packages-lock.json` the first time it opens the project; Unity re-resolves the package when the tag in the manifest changes.

## 1. Play locally (direct connection)

1. Open this folder with Unity `2021.3.45f2` and install the **Linux Build Support (Mono)**, **Linux Dedicated Server Build Support** and **WebGL Build Support** modules. Start Docker Desktop.
2. Open **Tools → Edgegap Server Hosting**:
   1. **Sign in with Edgegap** (or paste a token from **Get token**), then **Validate**.
   2. **Build server**. The output goes to `Builds/EdgegapServer/`.
   3. **Containerize with Docker**: **Image Name** `fishnet-server`, **Image Tag** empty (the plugin uses the current UTC time), default Dockerfile.
   4. **Test Your Server Locally**: **Optional Docker Run Parameters** `-p 7770:7770`, then **Deploy local container**. Docker Desktop shows `Edgegap Server | Starting server on 0.0.0.0:7770.` The plugin passes mock Edgegap variables, so the server exits 120 s after start if nobody joins, or 30 s after the last player leaves; **Terminate** before the next run.
3. Build the client from **File → Build Settings**, or from the command line:
   ```bash
   Unity -batchmode -projectPath . -executeMethod EdgegapClientBuild.Build -edgegapClientTarget webgl
   ```
   Serve `Builds/Client/webgl` (for example `python -m http.server 8080`), open `http://localhost:8080` in two tabs and click **Start Game**. The defaults (`localhost`, `7770`, **Direct Use Wss** off) connect to the local server. Desktop builds (`-edgegapClientTarget win64`) work too, since Bayou supports native clients.

Without the Editor (CI, scripted tests), `Unity -batchmode -projectPath . -executeMethod EdgegapPluginBuild.BuildServer` runs the plugin's **Build server**.

## 2. Play on an Edgegap server (direct connection)

1. In the plugin, **Upload to Edgegap**: **Application Name** (for example `fishnet-server`), then **Upload image and Create app version**. The plugin pushes the image to your Edgegap registry and opens the dashboard's version form with the image and tag filled in. On it:
   - Port: replace the form's default port with name `gameport`, port `7770`, protocol **WS**, **TLS Upgrade** on. Edgegap terminates TLS, so browsers connect with `wss://` while the server speaks plain `ws`.
   - Registry credentials: if the form asks for them, use the username and token from the dashboard's **Container Registry** page, or deployments fail to pull the image.
   - Keep the version name the plugin proposes (the image tag) and save.
2. **Deploy to cloud** in the plugin (or Deploy on the version page). On `MatchmakingSystem`, set **Direct Address** to the deployment's host (`<id>.pr.edgegap.net`), **Direct Port** to its external port, and tick **Direct Use Wss**. Rebuild the client and play.

The server stops its deployment 120 s after it starts if nobody joins, and 30 s after the last player leaves.

## 3. Turn on the Edgegap Matchmaker (optional)

1. In `matchmaker-config.json`, set `application.name` / `version` to your app and version, and list every origin that serves the WebGL build in `allowed_cors_origins`.
2. Dashboard → **Matchmaker** → **Create Matchmaker** → upload the file → **Create and Start**.
3. In `MainMenu`, copy the **API URL** and **Auth Token** into the `Edgegap` object's **Edgegap Matchmaking Client** (`Base Url`, `Auth Token`), and tick **Use Edgegap Matchmaker** on `MatchmakingSystem`.
4. Rebuild the client, serve it from an origin listed in `allowed_cors_origins` (e.g. `http://localhost:8080`), open two tabs and click **Start Game** in both. The `casual` profile matches 2 to 4 players.

**Playing alone:** with the default direct connection there is no queue: one client connects straight to `localhost:7770` (or the deployment you set) and plays on its own. With the matchmaker on, the shipped `matchmaker-config.json` (matchmaker `3.3.2`) needs at least 2 players (`min_team_size: 2`), with a 20 s expansion that lowers `min_team_size` to 1 and a 30 s `ticket_expiration_period`. With the earlier 5-minute expiration, a lone ticket in our tests did not get a server at the expansion; it only reached `HOST_ASSIGNED` around its expiration. The 30 s expiration keeps that wait short; the lone-player case was not re-tested with it. Test the matchmaker with two clients.

### Shipping a new build

In the plugin, **Upload to Edgegap → Rebuild from Source** builds the server, containerizes it with a fresh timestamp tag and pushes it, then opens the version form again (set the `gameport` WS port with TLS Upgrade and save) → in the matchmaker, **Stop → Edit** `application.version` **→ Start**.

## Notes

- Free tier: the matchmaker restarts every 3 hours (URL and token stay the same), and only one deployment runs at a time.
- WebGL can't ping latency beacons, so the profile has no `latencies` rule and Edgegap places the server from player IPs.
- Next steps for a real game: player validation (below), backfill, and a production matchmaker tier.

**Known limitation:** the server does not check that a connecting player holds a matchmaker ticket for this match, so anyone who learns the server address can join. Deployments stop themselves when no one joins or everyone leaves, which limits cost. Before production, validate each player against the match's tickets (`MM_TICKET_IDS` / the matchmaker API) or an auth token.
