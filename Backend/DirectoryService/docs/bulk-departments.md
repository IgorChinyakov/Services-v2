# Bulk department operations

One HTTP request processes up to 100 selected department IDs. These endpoints
reuse the single-record repository operations, including their hierarchy rules,
locking and archive/restore side effects. They do not run one HTTP request per ID.

## Endpoints

| Method | Route | Body |
| --- | --- | --- |
| POST | `/api/departments/bulk/archive` | `{ "ids": ["<guid>"] }` |
| POST | `/api/departments/bulk/restore` | `{ "ids": ["<guid>"] }` |
| PATCH | `/api/departments/bulk/activity` | `{ "ids": ["<guid>"], "isActive": false }` |

`ids` is required, nonempty, contains no empty GUIDs or duplicates, and has at
most 100 elements. Activity requires an explicit boolean `isActive`; missing or
null values are rejected instead of defaulting to false. Invalid bodies return
HTTP 400 in the standard error envelope before any mutation takes place.

## Partial success

A completed batch returns HTTP 200 even if some or all individual operations
failed. The standard envelope has `isError: false` and an empty outer `errors`
array. Read item failures from `result.errors`, not from a rejected HTTP request:

```json
{
  "result": {
    "succeededIds": ["10000000-0000-0000-0000-000000000001"],
    "errors": [
      {
        "id": "20000000-0000-0000-0000-000000000002",
        "code": "department.activity.active_descendants",
        "message": "Deactivate active descendants first."
      }
    ],
    "succeededCount": 1,
    "failedCount": 1
  },
  "errors": [],
  "isError": false
}
```

Every ID appears once in either `succeededIds` or `errors`. Successful IDs are
listed in processing order, not necessarily request order. An activity operation
already at its requested value counts as successful. Archive of an already
archived record and restore of a non-archived record retain the single-record
API's not-found error behavior.

Each item runs in its own DI scope, database context and transaction. A failure
rolls back only that item's work, never earlier successful items. This also
prevents tracked entities from a failed operation leaking into a later save.

## Hierarchy and related records

The handler reads selected records' depths, including archived and inactive
records. Activation and restore process ancestors before descendants; archive
and deactivation process descendants before ancestors. Equal depths are ordered
by ID. Unknown IDs are processed last and reported individually.

Unselected ancestors and descendants are not automatically activated, restored
or archived. Existing restrictions still apply: for example, activating a child
whose unselected ancestor is inactive fails for that child. Archiving keeps the
existing path-rewrite and orphaned location/position archive behavior. Restoring
a department does not automatically restore its locations or positions.

The initial depth snapshot chooses processing order but does not replace the
repository's transactional checks. Concurrent changes can still cause item
conflicts, which are returned as partial failures.

## Cancellation and cache

Department and location server-cache tags are invalidated once after a batch
with any successful items, including when a later item is cancelled. Cancellation
stops subsequent processing; already committed items are not undone. If the
connection is lost or the client cancels, refresh the list before retrying because
the client may not have received the outcome of earlier items.

On the frontend, invalidate list/tree/detail queries after any successes. Clear
selection on full success; on partial success, keep only failed IDs selected and
show their messages. Selected IDs stay local to the page and are cleared when
filters, pagination or archive view change.
