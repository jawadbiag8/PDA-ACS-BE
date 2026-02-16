# SignalR real-time updates – design

## Goal

Allow users to see updated data **without manual refresh** for:

1. **GET /api/AdminDashboard/summary**
2. **GET /api/Incident/{id}** (e.g. `/api/Incident/816`)
3. **GET /api/Asset/{id}/controlpanel** (e.g. `/api/Asset/241/controlpanel`)

More endpoints may be added later, so the design should be **generic and extensible**.

---

## Recommended approach: **Notify-then-refetch**

- **SignalR is used only to notify** that “this resource has new data”.
- The client **continues to use the existing REST endpoints** for the actual data.
- Flow: client opens page → calls REST API for initial data → subscribes to a **topic** for that resource → when server sends a notification for that topic → client **refetches the same REST endpoint** and updates the UI.

### Why this approach

| Aspect | Notify-then-refetch | Alternative: push full payload over SignalR |
|--------|---------------------|---------------------------------------------|
| **REST APIs** | No change; single source of truth | Would need to duplicate “what to send” in the hub |
| **Adding new endpoints** | Add one new topic + trigger points | New hub method + duplicate DTO/serialization |
| **Consistency** | Same response shape as today | Must keep REST and SignalR payloads in sync |
| **Payload size** | Only a small “topic” string over SignalR | Large JSON over SignalR for summary/control panel |
| **Client** | One refetch after notify (one extra HTTP call) | No refetch; slightly faster update |

**Conclusion:** Notify-then-refetch keeps existing implementation untouched, scales to many endpoints via a single generic hub, and avoids duplicating logic or payloads.

---

## Generic design: **topic-based hub**

### 1. One hub, topic-based groups

- **Single hub** (e.g. `DataUpdateHub`).
- Each “resource” is a **topic** (string). Clients **join a group** named by that topic.
- Server, when data for that topic changes, sends one message to that group: e.g. `DataUpdated(topic)`.

**Topic naming convention (extensible):**

| Topic pattern | Example | Corresponding REST endpoint |
|---------------|---------|-----------------------------|
| `AdminDashboard.Summary` | `AdminDashboard.Summary` | GET /api/AdminDashboard/summary |
| `Incident.{id}` | `Incident.816` | GET /api/Incident/816 |
| `Asset.{id}.ControlPanel` | `Asset.241.ControlPanel` | GET /api/Asset/241/controlpanel |

Future examples: `Ministry.{id}.Report`, `Asset.{id}.DashboardHeader`, etc.

### 2. Server responsibilities

- **Hub:**  
  - `JoinTopic(topic)` – client calls this to subscribe; server adds connection to group `topic`.  
  - Server invokes `DataUpdated(topic)` (or similar) on the group when data for that topic has changed.
- **Services (existing):**  
  After any write that affects a topic, call a small helper: “notify topic X”.  
  - This helper uses `IHubContext<DataUpdateHub>` to send to the group.  
  - No change to return types or URLs of existing REST APIs.

### 3. Client responsibilities (frontend)

- On page load:  
  - Call REST API (e.g. GET /api/AdminDashboard/summary).  
  - Connect to SignalR and call `JoinTopic("AdminDashboard.Summary")`.
- On receiving `DataUpdated("AdminDashboard.Summary")`:  
  - Call GET /api/AdminDashboard/summary again and update UI.

Same pattern for Incident and Asset control panel (with their topic names and endpoints).

---

## What changes are required

| Layer | Change |
|-------|--------|
| **New** | SignalR package, hub class, topic constants, mapping “topic ↔ REST endpoint” (doc or config). |
| **New** | A small “notify” service or extension that accepts a topic and uses `IHubContext` to send `DataUpdated(topic)` to the group. |
| **Existing services** | After relevant writes (create/update/delete), call the notify with the right topic(s). No change to REST controller actions or DTOs. |
| **Existing REST** | No change to URLs, responses, or behaviour. |
| **Auth** | SignalR connection should use same auth (e.g. JWT). Optionally restrict which topics a user can join (e.g. by role or resource access). |

So: **REST stays as-is**; **add** SignalR + topic-based notifications and call the notify from existing write paths.

---

## When to notify (trigger points)

- **AdminDashboard.Summary**  
  When anything that the dashboard summary depends on changes (e.g. assets, incidents, ministries). That can be many places; options:  
  - Call notify from each relevant service method (Create/Update/Delete asset, incident, etc.), or  
  - Introduce a small “domain event” or “data changed” pipeline that multiple services raise, and one handler calls the hub (more generic for future endpoints).

- **Incident.{id}**  
  When incident `id` is created/updated or a comment is added (e.g. in `IncidentService`: after Create, Update, AddComment, call notify for `Incident.{id}`).

- **Asset.{id}.ControlPanel**  
  When data that feeds the control panel for asset `id` changes (e.g. KPI results, asset record, or anything that affects control panel computation). Call notify from the relevant service(s) after save (e.g. `KPIsResultService`, or wherever control panel inputs are updated).

Adding a new endpoint later = define a new topic + add notify calls in the code paths that change that resource.

---

## Pros and cons

### Pros

- **No impact on existing REST API** – same URLs, same responses; no breaking changes.
- **Generic and scalable** – one hub, one message shape; new endpoints = new topic + trigger points.
- **Single source of truth** – only REST returns the real payload; no duplication of DTOs or logic in SignalR.
- **Small Surface area** – hub has essentially “join topic” and “DataUpdated(topic)”.
- **Familiar client pattern** – “subscribe to topic → on notify → refetch REST”.

### Cons

- **One extra HTTP request** after each notification (refetch). Usually acceptable for dashboard, incident, control panel.
- **Trigger coverage** – you must remember to call “notify” in every code path that changes data for a topic (can be mitigated with a small list in code or docs: “topic X is invalidated by operations A, B, C”).
- **Auth/authorization** – must ensure users can only join topics they’re allowed to see (e.g. not join `Incident.816` if they shouldn’t see that incident).

---

## Impact summary

| Question | Answer |
|----------|--------|
| Will existing REST implementation change? | **No.** Same endpoints, same responses. |
| Is this a separate “layer” on top of REST? | **Yes.** SignalR is an additional channel for “please refetch”; REST remains the data channel. |
| Can we add more endpoints later? | **Yes.** Define a new topic and add notify calls where that resource is updated. |
| Do we need to duplicate response shapes in SignalR? | **No.** Only a topic identifier is sent over SignalR. |

---

## Suggested implementation order (when you implement)

1. Add SignalR package and register hub + auth in the API.
2. Implement `DataUpdateHub` with `JoinTopic(topic)` and server-side `DataUpdated(topic)`.
3. Add topic constants and a thin “notify topic” helper using `IHubContext`.
4. Add trigger calls for the three current endpoints (AdminDashboard summary, Incident by id, Asset control panel).
5. Document topic ↔ REST endpoint mapping for frontend (and for future endpoints).
6. Frontend: connect to hub, join topic(s), on `DataUpdated` refetch the corresponding REST URL.

This keeps the implementation generic and keeps all existing behaviour intact while enabling real-time updates without manual refresh.

---

## Implementation summary (done)

- **Hub:** `DAMS.API/Hubs/DataUpdateHub.cs` – clients call `JoinTopic(topic)` / `LeaveTopic(topic)`; server sends `DataUpdated(topic)` to the group.
- **Notifier:** `DAMS.API/Services/DataUpdateNotifier.cs` – implements `IDataUpdateNotifier`; used by services to call `NotifyTopicAsync(topic)`.
- **Topics:** `DAMS.Application/DataUpdateTopics.cs` – constants and helpers: `AdminDashboardSummary`, `Incident(id)`, `AssetControlPanel(assetId)`.
- **Auth:** JWT is read from query string for paths under `/hubs` so SignalR connections can send `?access_token=...`.
- **Triggers:**  
  - **AdminDashboard.Summary:** Asset create/update/delete/bulk-upload; Incident create/delete.  
  - **Incident.{id}:** Incident create, update, add-comment, delete.  
  - **Asset.{id}.ControlPanel:** Asset update, delete.

**Hub URL:** `{BaseUrl}/hubs/data-update` (e.g. `http://47.129.240.107:7008/hubs/data-update`).

---

## Frontend: how to use

1. **Connect** to the hub with the same JWT (e.g. send as `access_token` query param or via `accessTokenFactory`).
2. **On the page** that shows dashboard summary: call `connection.invoke("JoinTopic", "AdminDashboard.Summary")`.
3. **On the page** that shows incident 816: call `connection.invoke("JoinTopic", "Incident.816")`.
4. **On the page** that shows asset 241 control panel: call `connection.invoke("JoinTopic", "Asset.241.ControlPanel")`.
5. **Listen** for `DataUpdated`: `connection.on("DataUpdated", (topic) => { /* refetch the REST API for that topic */ })`.
6. **Refetch:** When you receive `DataUpdated(topic)`, call the corresponding REST endpoint and update the UI.

**Topic → REST endpoint mapping:**

| Topic | Refetch this endpoint |
|-------|------------------------|
| `AdminDashboard.Summary` | GET /api/AdminDashboard/summary |
| `Incident.816` | GET /api/Incident/816 |
| `Asset.241.ControlPanel` | GET /api/Asset/241/controlpanel |

**Example (JavaScript/TypeScript):**

```javascript
import * as signalR from "@microsoft/signalr";

const token = "your-jwt";
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://47.129.240.107:7008/hubs/data-update", { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build();

connection.on("DataUpdated", (topic) => {
  if (topic === "AdminDashboard.Summary") fetch("/api/AdminDashboard/summary").then(/* update UI */);
  else if (topic.startsWith("Incident.")) fetch(`/api/Incident/${topic.split(".")[1]}`).then(/* update UI */);
  else if (topic.startsWith("Asset.")) {
    const parts = topic.split(".");
    if (parts[2] === "ControlPanel") fetch(`/api/Asset/${parts[1]}/controlpanel`).then(/* update UI */);
  }
});

await connection.start();
await connection.invoke("JoinTopic", "AdminDashboard.Summary");
```

---

## Testing without a full frontend

You can test the hub **without** your real frontend using the included test page or the browser console.

### Option 1: Test page (recommended, no CORS)

1. Start the API (e.g. `dotnet run` for DAMS.API or use your deployed URL).
2. Open the test page **from the same origin as the API** so the browser does not send cross-origin requests (no CORS):
   - **https://localhost:7008/signalr-test-page.html** (the API serves this from `wwwroot`).
   - If you use a different base URL, use **`{YourApiBaseUrl}/signalr-test-page.html`**.
3. The default **API base URL** in the page is `https://localhost:7008`; change it if your Swagger is elsewhere.
4. Click **1. Login** (enter username/password) so the token is filled, or paste a JWT from Swagger login.
5. Click **2. Connect to hub** – the status should show “Connected”.
6. Click **3. Join topic** (e.g. **AdminDashboard.Summary** or **Incident.816**).
7. In another tab, open **Swagger** (e.g. https://localhost:7008/swagger/index.html) and trigger a change:
   - Create/update/delete an asset → you should see **DataUpdated: AdminDashboard.Summary** (and **Asset.{id}.ControlPanel** if you joined that topic).
   - Update incident 816 or add a comment → you should see **DataUpdated: Incident.816**.
8. The test page log will show each **DataUpdated (topic)** as it arrives.

If you open the file from `file://` or another domain, you may get CORS errors. Always use the URL above (same origin as the API).

### Option 2: Browser console (quick check)

1. Log in via Swagger and copy the JWT from the response.
2. Open any page on the same origin (or a local HTML file that loads SignalR from CDN).
3. In the console, load SignalR and connect (replace `YOUR_JWT`; use the same base URL as your Swagger, e.g. `https://localhost:7008`):

```javascript
// Load SignalR (if not already on page)
const script = document.createElement('script');
script.src = 'https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js';
script.onload = () => {
  const c = new signalR.HubConnectionBuilder()
    .withUrl('https://localhost:7008/hubs/data-update', { accessTokenFactory: () => 'YOUR_JWT' })
    .withAutomaticReconnect()
    .build();
  c.on('DataUpdated', (topic) => console.log('DataUpdated', topic));
  c.start().then(() => c.invoke('JoinTopic', 'AdminDashboard.Summary')).then(() => console.log('Joined'));
};
document.head.appendChild(script);
```

4. Trigger a change in Swagger; you should see `DataUpdated AdminDashboard.Summary` (or the relevant topic) in the console.

### CORS

To avoid CORS entirely, open the test page **from the API** (same origin): **https://localhost:7008/signalr-test-page.html**. The API serves this file from `wwwroot`. If you open the file from `file://` or another domain, you may get CORS errors; in that case use the URL above.
