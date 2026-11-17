# Global Search

`GET /api/search?q=engineer`

The endpoint searches entity names, case-insensitively, using a literal substring.
Leading and trailing whitespace is removed before validation. The trimmed query
must contain 2 to 200 characters. Missing, shorter or longer queries return
HTTP 400 with the existing error envelope (`invalidField: "Q"`).

## Response

The standard envelope contains a flat array in `result`. No matches means
`result: []`, not an error. Results are grouped in this order: locations,
departments, positions. Each group contains at most five records, sorted by
name and then ID. There is no pagination or total count.

Each result has these properties:

| Field | Meaning |
| --- | --- |
| `type` | `location`, `department` or `position` |
| `id` | Entity ID; combine with `type` to identify the result |
| `title` | Entity name |
| `subtitle` | Address, department path or position's department names |
| `isActive` | Current activity; inactive non-archived departments remain searchable |
| `departmentId` | Optional navigation context for a position; null for other types |

Archived records are excluded. Locations and positions must also be active,
matching their current list APIs. A position is returned only once, even when
linked to multiple departments. Its subtitle lists up to three active,
non-archived departments, sorted by name and ID. `departmentId` points to the
first of these. If no suitable department exists, `departmentId` is null and
the subtitle falls back to the description or an empty string.

The frontend chooses the destination from `type`, `id` and optional
`departmentId`; the backend does not embed frontend URLs. For a position without
department context, use the general positions section and focus the record by ID.

The query is parameterized. `%`, `_` and backslashes are treated literally,
not as SQL pattern operators. Cancellation is propagated to the database.
No additional server-side search cache is used.

## Focused results in the frontend

Selecting a result opens `/locations?focusId=<id>`,
`/departments?focusId=<id>` or `/positions?focusId=<id>`. The destination
loads the record independently of list pagination and displays a highlighted
card above the list. The URL preserves the selection on reload. Clearing it
removes only `focusId`, leaving other query parameters intact.

`GET /api/positions/{id}` returns the position card data in the standard
envelope. Inactive, archived and unknown positions return HTTP 404; an empty
GUID returns HTTP 400. `GET /api/locations/{id}` additionally returns
`departmentCount` and `deletedAt` for the existing location card.
