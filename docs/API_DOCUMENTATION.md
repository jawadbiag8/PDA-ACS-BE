# DAMS API Documentation

API for the Digital Asset Management System (DAMS). Use this document for a quick reference; for interactive exploration use **Swagger UI** at `/swagger` when the API is running.

---

## Base URL & Authentication

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5033` (or your deployed host) |
| **Swagger UI** | `{BaseUrl}/swagger` |
| **Auth** | JWT Bearer token. Send header: `Authorization: Bearer <token>` |
| **Login** | `POST /api/auth/login` (no auth required); use the returned token for all other requests. |

Most endpoints require roles **PDA Analyst** or **PMO Executive**; PM Dashboard endpoints require **PMO Executive** only.

---

## Response format

All endpoints return a standard wrapper:

```json
{
  "isSuccessful": true,
  "message": "Optional message",
  "data": { ... }
}
```

- **Success:** `isSuccessful: true`, `data` holds the result (object or array).
- **Error:** `isSuccessful: false`, `message` describes the error; `data` may be null.
- **HTTP status:** 200/201 for success; 400 Bad Request, 401 Unauthorized, 404 Not Found, 500 for server errors.

---

## Endpoints by controller

### Auth (`/api/auth`)

No authentication required for login/logout.

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/login` | Login. Body: `{ "username": "...", "password": "..." }`. Returns token in `data`. |
| POST | `/api/auth/logout` | Logout (invalidates token / clears server session if applicable). |

---

### Asset (`/api/asset`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/asset` | List assets. Query: pagination, search, filters (see `AssetFilterDto`). |
| GET | `/api/asset/byministry` | Assets grouped by ministry. Query: `search` (optional). |
| GET | `/api/asset/{id}` | Get asset by ID. |
| GET | `/api/asset/{id}/dashboard/header` | Dashboard header for asset. |
| GET | `/api/asset/{id}/controlpanel` | Control panel data (KPIs, compliance from last 30 days). |
| GET | `/api/asset/ministry/{ministryId}` | Assets for a ministry. Query: same as list. |
| GET | `/api/asset/ministry/{ministryId}/summary` | Ministry assets summary. |
| GET | `/api/asset/department/{departmentId}` | Assets for a department. |
| GET | `/api/asset/dropdown` | Assets for dropdown (id/name). |
| POST | `/api/asset` | Create asset. Body: `CreateAssetDto`. |
| PUT | `/api/asset/{id}` | Update asset. Body: `UpdateAssetDto`. |
| DELETE | `/api/asset/{id}` | Delete asset. |
| POST | `/api/asset/bulk-upload` | Bulk upload assets. Body: form-data, file (CSV, max 10MB). |

---

### Ministry (`/api/ministry`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ministry` | List ministries. Query: `MinistryFilterDto`. |
| GET | `/api/ministry/getall` | All ministries (no filter). |
| GET | `/api/ministry/ministrydetails` | Ministry details with search, pagination. Query: `PagedRequest`, `filterType`, `filterValue` (e.g. Status: UP/DOWN/ALL; metrics: High/Average/Poor/Unknown). |
| GET | `/api/ministry/{id}` | Get ministry by ID. |
| GET | `/api/ministry/{id}/report` | Download ministry PDF report (summary + assets compliance). |
| POST | `/api/ministry` | Create ministry. Body: `CreateMinistryDto`. |
| PUT | `/api/ministry/{id}` | Update ministry. Body: `UpdateMinistryDto`. |
| DELETE | `/api/ministry/{id}` | Delete ministry. |

---

### Department (`/api/department`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/department` | List departments. Query: `DepartmentFilterDto`. |
| GET | `/api/department/getall/{ministryId}` | All departments for a ministry. |
| GET | `/api/department/{id}` | Get department by ID. |
| GET | `/api/department/ministry/{ministryId}` | Departments by ministry. |
| POST | `/api/department` | Create department. Body: `CreateDepartmentDto`. |
| PUT | `/api/department/{id}` | Update department. Body: `UpdateDepartmentDto`. |
| DELETE | `/api/department/{id}` | Delete department. |

---

### Incident (`/api/incident`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/incident` | List incidents. Query: `IncidentFilterDto`. |
| GET | `/api/incident/{id}` | Get incident by ID. |
| GET | `/api/incident/{id}/details` | Incident details (includes KPI details). |
| GET | `/api/incident/asset/{assetId}` | Incidents for an asset. Query: optional `IncidentFilterDto`. |
| GET | `/api/incident/{id}/comments` | Comments for an incident. |
| POST | `/api/incident` | Create incident. Body: `CreateIncidentDto`. |
| POST | `/api/incident/{id}/comments` | Add comment. Body: `CreateIncidentCommentDto` (IncidentId set from route). |
| PUT | `/api/incident/{id}` | Update incident. Body: `UpdateIncidentDto`. |
| DELETE | `/api/incident/{id}` | Delete incident. |

---

### User (`/api/user`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/user` | List all users. |
| GET | `/api/user/{id}` | Get user by ID (string). |
| GET | `/api/user/email/{email}` | Get user by email. |
| GET | `/api/user/role/{roleName}` | Users by role. |
| GET | `/api/user/dropdown` | Users for dropdown. |
| POST | `/api/user` | Create user. Body: `CreateUserDto` (email, password, firstName, lastName). |
| PUT | `/api/user/{id}` | Update user. Body: `UpdateUserDto`. |
| DELETE | `/api/user/{id}` | Delete user. |
| POST | `/api/user/role` | Create role. Body: `CreateRoleDto`. |
| POST | `/api/user/assign-role` | Assign role to user. Body: `AssignRoleDto` (userId, roleName). |
| POST | `/api/user/remove-role` | Remove role from user. Body: `AssignRoleDto`. |

---

### KpisLov (`/api/kpislov`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/kpislov` | List KPI lookups. Query: `KpisLovFilterDto`. |
| GET | `/api/kpislov/{id}` | Get KPI lookup by ID. |
| GET | `/api/kpislov/dropdown` | KPI lookups for dropdown. |
| GET | `/api/kpislov/manual-from-asset/{assetId}?kpiId={kpiId}` | Manual-check data for asset + KPI. `kpiId` required. |
| POST | `/api/kpislov` | Create KPI lookup. Body: `CreateKpisLovDto`. |
| PUT | `/api/kpislov/{id}` | Update KPI lookup. Body: `UpdateKpisLovDto`. |
| DELETE | `/api/kpislov/{id}` | Delete KPI lookup. |

---

### KPIsResult (`/api/kpisresult`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/kpisresult` | List KPI results. Query: `KPIsResultFilterDto`. |
| GET | `/api/kpisresult/{id}` | Get KPI result by ID. |
| GET | `/api/kpisresult/asset/{assetId}` | KPI results for an asset. |
| POST | `/api/kpisresult` | Create KPI result. Body: `CreateKPIsResultDto`. |
| PUT | `/api/kpisresult/{id}` | Update KPI result. Body: `UpdateKPIsResultDto`. |
| DELETE | `/api/kpisresult/{id}` | Delete KPI result. |

---

### CommonLookup (`/api/commonlookup`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/commonlookup` | List common lookups. Query: `PagedRequest`. |
| GET | `/api/commonlookup/{id}` | Get lookup by ID. |
| GET | `/api/commonlookup/type/{type}` | Lookups by type. |
| POST | `/api/commonlookup` | Create lookup. Body: `CreateCommonLookupDto`. |
| PUT | `/api/commonlookup/{id}` | Update lookup. Body: `UpdateCommonLookupDto`. |
| DELETE | `/api/commonlookup/{id}` | Delete lookup. |

---

### AdminDashboard (`/api/admindashboard`)

**Auth:** PDA Analyst, PMO Executive

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admindashboard` | Simple dashboard placeholder. |
| GET | `/api/admindashboard/summary` | Dashboard summary (aggregate stats). |

---

### PMDashboard (`/api/pmdashboard`)

**Auth:** PMO Executive only

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/pmdashboard` | PM dashboard placeholder. |
| GET | `/api/pmdashboard/header` | PM dashboard header. |
| GET | `/api/pmdashboard/indices` | PM dashboard indices. |
| GET | `/api/pmdashboard/bottom-ministries?count=5` | Bottom ministries by citizen impact. Query: `count` (default 5). |
| GET | `/api/pmdashboard/top-ministries?count=5` | Top ministries by compliance. Query: `count` (default 5). |

---

## Query / body types (reference)

- **PagedRequest:** `pageNumber`, `pageSize`, `searchTerm` (and optional sort/filter fields where used).
- **AssetFilterDto:** Extends `PagedRequest`; ministry/department/status and other asset filters.
- **MinistryFilterDto**, **DepartmentFilterDto**, **IncidentFilterDto**, **KPIsResultFilterDto**, **KpisLovFilterDto:** Controller-specific filters; see Swagger for exact properties.

Exact request/response schemas are available in **Swagger** (`/swagger`) and in the `DAMS.Application` DTOs.

---

## Related docs

- [KPI calculation reference (control panel)](./KPI_CALCULATION_REFERENCE.md)
- [KPI by ID – how it works](./KPI_BY_ID_HOW_IT_WORKS.md)
- [Asset control panel compliance calculation](./ASSET_CONTROL_PANEL_COMPLIANCE_CALCULATION.md)

---

## Keeping this doc up to date

When you **add, change, or remove** API endpoints or controllers:

1. Update **docs/API_DOCUMENTATION.md** in the same change:
   - New controller → add a new section and endpoint table.
   - New endpoint → add the row (Method, Path, Description) under the right controller.
   - Changed path/method/auth → update the existing row.
   - Removed endpoint → remove the row (or controller section if empty).
2. Keep the **Response format** and **Base URL & Authentication** sections accurate if auth or response shape changes.
3. Swagger stays in sync from code; this markdown is the human-readable reference—update it so the two stay aligned.
