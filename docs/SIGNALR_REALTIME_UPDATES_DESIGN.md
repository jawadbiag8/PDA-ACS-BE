# SignalR real-time updates – design

## Goal

Allow users to see updated data **without manual refresh** for:

1. **GET /api/AdminDashboard/summary**
2. **GET /api/PMDashboard/header**
3. **GET /api/Incident/{id}** (e.g. `/api/Incident/816`)
4. **GET /api/Asset/{id}/controlpanel** (e.g. `/api/Asset/241/controlpanel`)
5. **GET /api/KpisLov/manual-from-asset/{assetId}** (KPI/manual data for an asset)

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
| `PMDashboard.Header` | `PMDashboard.Header` | GET /api/PMDashboard/header |
| `Incident.{id}` | `Incident.816` | GET /api/Incident/816 |
| `Asset.{assetId}.ControlPanel` | `Asset.241.ControlPanel` | GET /api/Asset/241/controlpanel |
| `Asset.{assetId}.KpisLov` | `Asset.241.KpisLov` | GET /api/KpisLov/manual-from-asset/241?kpiId=X (per KPI) |

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

- **AdminDashboard.Summary** and **PMDashboard.Header**  
  When assets, incidents, or dashboard metrics change. Notified from: Asset create/update/delete/bulk-upload; Incident create/update/delete/add-comment. When the **external KPI scheduler** recalculates metrics (AssetMetrics, KPIsResults, etc.), it calls **POST /api/DataUpdate/notify-dashboards** so clients refetch both dashboards.  “domain event” or “data changed”
- **Incident.{id}**  
  When incident `id` is created/updated or a comment is added (in `IncidentService`: after Create, Update, AddComment, Delete). When the **external scheduler** creates or closes incidents, it calls **POST /api/DataUpdate/notify-incidents** (optionally with `incidentIds`).

- **Asset.{id}.ControlPanel** and **Asset.{id}.KpisLov**  
  When data that feeds the control panel for asset `id` changes. Notified from: Asset update/delete (in backend). When the **KPI scheduler** or manual check updates KPI results for an asset, it calls **POST /api/Asset/{id}/controlpanel/notify** (which also notifies both dashboards).

Adding a new endpoint later = define a new topic + add notify calls in the code paths that change that resource (or expose a notify API for external systems).

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
- **Notifier:** `DAMS.API/Services/DataUpdateNotifier.cs` – implements `IDataUpdateNotifier`; used by services and controllers to call `NotifyTopicAsync(topic)`.
- **Topics:** `DAMS.Application/DataUpdateTopics.cs` – constants and helpers: `AdminDashboardSummary`, `PMDashboardHeader`, `Incident(id)`, `AssetControlPanel(assetId)`, `AssetKpisLov(assetId)`.
- **Auth:** JWT is read from query string for paths under `/hubs` so SignalR connections can send `?access_token=...`.
- **Triggers (backend write paths):**  
  - **AdminDashboard.Summary** and **PMDashboard.Header:** Asset create/update/delete/bulk-upload; Incident create/update/delete/add-comment.  
  - **Incident.{id}:** Incident create, update, add-comment, delete.  
  - **Asset.{id}.ControlPanel** and **Asset.{id}.KpisLov:** Asset update, delete; also when **POST /api/Asset/{id}/controlpanel/notify** is called.
- **Notify endpoints (for external KPI scheduler):**  
  - **POST /api/DataUpdate/notify-dashboards** – call after recalculating dashboard metrics (AssetMetrics, KPIsResults, etc.).  
  - **POST /api/DataUpdate/notify-incidents** – call after creating or closing/updating incidents; optional body `{ "incidentIds": [ ... ] }`.  
  - **POST /api/Asset/{id}/controlpanel/notify** – call after updating KPI results for an asset (e.g. manual check or scheduler); also notifies both dashboards.

**Hub URL:** `{BaseUrl}/hubs/data-update` (e.g. `http://47.129.240.107:7008/hubs/data-update`).

---

## Frontend: how to use

1. **Connect** to the hub with the same JWT (e.g. send as `access_token` query param or via `accessTokenFactory`).
2. **Join topics** for the pages you are on:
   - Admin Dashboard summary page → `connection.invoke("JoinTopic", "AdminDashboard.Summary")`.
   - PM Dashboard page → `connection.invoke("JoinTopic", "PMDashboard.Header")`.
   - Incident detail (e.g. id 816) → `connection.invoke("JoinTopic", "Incident.816")`.
   - Asset control panel (e.g. asset 241) → `connection.invoke("JoinTopic", "Asset.241.ControlPanel")` and optionally `"Asset.241.KpisLov"`.
3. **Listen** for `DataUpdated`: `connection.on("DataUpdated", (topic) => { /* refetch the REST API for that topic */ })`.
4. **Refetch:** When you receive `DataUpdated(topic)`, call the corresponding REST endpoint and update the UI.
5. **Leave topic** when navigating away (optional): `connection.invoke("LeaveTopic", topic)`.

**Topic → REST endpoint mapping:**

| Topic | Refetch this endpoint |
|-------|------------------------|
| `AdminDashboard.Summary` | GET /api/AdminDashboard/summary |
| `PMDashboard.Header` | GET /api/PMDashboard/header |
| `Incident.{id}` | GET /api/Incident/{id} |
| `Asset.{assetId}.ControlPanel` | GET /api/Asset/{assetId}/controlpanel |
| `Asset.{assetId}.KpisLov` | GET /api/KpisLov/manual-from-asset/{assetId}?kpiId=X (per KPI as needed) |

**Topics to join for testing (copy-paste):**

- Dashboards only: `AdminDashboard.Summary`, `PMDashboard.Header`
- One asset (e.g. 249): `Asset.249.ControlPanel`, `Asset.249.KpisLov`
- One incident (e.g. 101): `Incident.101`
- Full set (dashboards + one asset + one incident):  
  `AdminDashboard.Summary`, `PMDashboard.Header`, `Asset.249.ControlPanel`, `Asset.249.KpisLov`, `Incident.101`  
  (replace 249 and 101 with real IDs.)

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
  else if (topic === "PMDashboard.Header") fetch("/api/PMDashboard/header").then(/* update UI */);
  else if (topic.startsWith("Incident.")) fetch(`/api/Incident/${topic.split(".")[1]}`).then(/* update UI */);
  else if (topic.startsWith("Asset.")) {
    const parts = topic.split(".");
    const assetId = parts[1];
    if (parts[2] === "ControlPanel") fetch(`/api/Asset/${assetId}/controlpanel`).then(/* update UI */);
    else if (parts[2] === "KpisLov") { /* refetch manual-from-asset for assetId as needed */ }
  }
});

await connection.start();
await connection.invoke("JoinTopic", "AdminDashboard.Summary");
await connection.invoke("JoinTopic", "PMDashboard.Header");
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
   - Create/update/delete an asset → you should see **DataUpdated: AdminDashboard.Summary**, **PMDashboard.Header**, and **Asset.{id}.ControlPanel** (if you joined that topic).
   - Create/update/delete an incident or add a comment → you should see **DataUpdated: AdminDashboard.Summary**, **PMDashboard.Header**, and **Incident.{id}**.
   - Call **POST /api/DataUpdate/notify-dashboards** → **AdminDashboard.Summary** and **PMDashboard.Header**.
   - Call **POST /api/Asset/249/controlpanel/notify** → **Asset.249.ControlPanel**, **Asset.249.KpisLov**, and both dashboard topics.
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

---

## Notify endpoints for external systems (KPI scheduler)

When the **KPI scheduler** (or another external system) writes to the database, it should call one of these endpoints so SignalR clients refetch. All require the same JWT (e.g. service account).

| Endpoint | When to call | Topics notified |
|----------|--------------|------------------|
| **POST /api/DataUpdate/notify-dashboards** | After recalculating and writing dashboard metrics (AssetMetrics, KPIsResults, KPIsResultHistories). | AdminDashboard.Summary, PMDashboard.Header |
| **POST /api/DataUpdate/notify-incidents** | After creating or closing/updating incidents. Optional body: `{ "incidentIds": [101, 102] }` to also refresh incident detail pages. | AdminDashboard.Summary, PMDashboard.Header, and each Incident.{id} if incidentIds provided |
| **POST /api/Asset/{id}/controlpanel/notify** | After updating KPI results for a single asset (e.g. manual check or scheduler run for that asset). | Asset.{id}.ControlPanel, Asset.{id}.KpisLov, AdminDashboard.Summary, PMDashboard.Header |

See [API Documentation](./API_DOCUMENTATION.md) for the DataUpdate and Asset controller sections.
