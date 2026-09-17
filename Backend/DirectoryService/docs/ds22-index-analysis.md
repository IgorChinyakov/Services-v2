# DS-22: Index analysis

## Dataset

The benchmark was run on PostgreSQL 16.13 with:

- 10,000 generated departments in four levels;
- 300 generated locations;
- 300 generated positions;
- 30,000 generated department-location links;
- 30,000 generated department-position links.

The database also contained 61 departments and a small number of links created
while testing previous features. The complete measured dataset therefore had
10,061 departments, 30,079 department-location links, and 30,033
department-position links.

The seed is idempotent and uses a dedicated `DS22` prefix. It does not remove or
modify existing business data.

## Results

All measurements use `EXPLAIN (ANALYZE, BUFFERS)` against the same data. For the
baseline, only the target indexes were dropped inside a transaction. The
transaction was then rolled back before collecting the optimized plans.

| Scenario | Baseline plan and time | Optimized plan and time | Index |
|---|---|---|---|
| `GET /departments/{id}/children` | `Seq Scan`, 1.601 ms | `Bitmap Index Scan`, 0.049 ms | `ix_department_parent_id` |
| `GET /departments/{id}/ancestors` | `Seq Scan`, 1.050 ms | `Bitmap Index Scan`, 0.068 ms | `ix_departments_path_gist` |
| `GET /departments/tree/search?q=platform` | `Seq Scan`, 2.468 ms | `Bitmap Index Scan`, 0.230 ms | `ix_departments_active_name_trgm` |
| Relation lookup by `location_id` | `Seq Scan`, 1.630 ms | `Bitmap Index Scan`, 0.973 ms | `idx_department_locations_location_id` |
| Relation lookup by `position_id` | `Seq Scan`, 1.524 ms | `Bitmap Index Scan`, 0.865 ms | `idx_department_positions_position_id` |

The raw plans, including buffer statistics and planning time, are stored in
[`ds22-explain-analyze.txt`](./ds22-explain-analyze.txt).

## Index decisions

### Department tree

- `ix_department_parent_id` is retained because both root/children navigation
  and the correlated `hasChildren` check use `parent_id`.
- `ix_departments_path_gist` is retained for `ltree` operators such as `@>` and
  `<@`. The ancestors query changed from scanning the department table to using
  a bitmap scan over the GIST index.
- `ix_departments_slug` remains a unique btree index because slug uniqueness is
  a business invariant.

### Name search

The existing btree index on `departments.name` is useful for ordering but cannot
serve the contains expression `ILIKE '%search%'`. DS-22 therefore adds the
`pg_trgm` extension and a partial GIN index:

```sql
CREATE INDEX ix_departments_active_name_trgm
ON departments USING gin (name gin_trgm_ops)
WHERE is_active = true;
```

The index is partial because the public tree search always excludes soft-deleted
departments. This keeps inactive rows out of the search index.

### Relation tables

- Separate btree indexes on `location_id` and `position_id` are retained for
  reverse lookups and join scenarios.
- Standalone indexes on `department_id` are removed as duplicates. The unique
  composite btree indexes `(department_id, location_id)` and
  `(department_id, position_id)` already serve queries by their leading
  `department_id` column. Both benchmark queries used `Index Only Scan` and
  completed in 0.005 ms.

### Partial indexes

A separate partial `parent_id` index was not added. More than 91% of the measured
departments were active, and the existing index already reduced the children
query to 0.049 ms. At that distribution, another parent index would add write
and storage cost without a demonstrated read benefit.

## Reproduction

Apply migrations and start PostgreSQL, then run:

```bash
docker compose exec -T postgres psql -U postgres -d directory_service -f - \
  < scripts/seed-ds22-performance-data.sql

docker compose exec -T postgres psql -U postgres -d directory_service -f - \
  < scripts/explain-ds22-indexes.sql
```

To remove only the generated performance data:

```bash
docker compose exec -T postgres psql -U postgres -d directory_service -f - \
  < scripts/cleanup-ds22-performance-data.sql
```
