\set ON_ERROR_STOP on
\pset pager off

select version();

select
    count(*) as departments,
    count(*) filter (where is_active) as active_departments
from departments;

select count(*) as department_locations from department_locations;
select count(*) as department_positions from department_positions;

\echo '===== BASELINE: TARGET INDEXES DISABLED ====='

begin;

drop index if exists ix_department_parent_id;
drop index if exists ix_departments_path_gist;
drop index if exists ix_departments_active_name_trgm;
drop index if exists idx_department_locations_location_id;
drop index if exists idx_department_positions_position_id;

analyze departments;
analyze department_locations;
analyze department_positions;

\echo '--- GET /departments/{id}/children ---'
explain (analyze, buffers)
select child.id,
       child.name,
       child.slug,
       child.path::text,
       child.depth,
       exists(
           select 1
           from departments nested
           where nested.parent_id = child.id
             and nested.is_active = true) as has_children
from departments child
where child.parent_id = md5('ds22-department:111')::uuid
  and child.is_active = true
order by child.name, child.id;

\echo '--- GET /departments/{id}/ancestors ---'
explain (analyze, buffers)
with target as (
    select path
    from departments
    where id = md5('ds22-department:1111')::uuid
      and is_active = true)
select ancestor.id,
       ancestor.name,
       ancestor.slug,
       ancestor.path::text,
       ancestor.depth
from target
join departments ancestor
  on ancestor.path @> target.path
 and ancestor.id <> md5('ds22-department:1111')::uuid
 and ancestor.is_active = true
order by ancestor.depth, ancestor.id;

\echo '--- GET /departments/tree/search?q=platform ---'
explain (analyze, buffers)
select id, name, slug, path::text, depth
from departments
where is_active = true
  and name ilike '%platform%'
order by name, id;

\echo '--- location relation lookup by location_id ---'
explain (analyze, buffers)
select dl.department_id
from department_locations dl
join departments d
  on d.id = dl.department_id
 and d.is_active = true
where dl.location_id = md5('ds22-location:42')::uuid;

\echo '--- position relation lookup by position_id ---'
explain (analyze, buffers)
select dp.department_id
from department_positions dp
join departments d
  on d.id = dp.department_id
 and d.is_active = true
where dp.position_id = md5('ds22-position:42')::uuid;

rollback;

\echo '===== OPTIMIZED: TARGET INDEXES ENABLED ====='

analyze departments;
analyze department_locations;
analyze department_positions;

\echo '--- GET /departments/{id}/children ---'
explain (analyze, buffers)
select child.id,
       child.name,
       child.slug,
       child.path::text,
       child.depth,
       exists(
           select 1
           from departments nested
           where nested.parent_id = child.id
             and nested.is_active = true) as has_children
from departments child
where child.parent_id = md5('ds22-department:111')::uuid
  and child.is_active = true
order by child.name, child.id;

\echo '--- GET /departments/{id}/ancestors ---'
explain (analyze, buffers)
with target as (
    select path
    from departments
    where id = md5('ds22-department:1111')::uuid
      and is_active = true)
select ancestor.id,
       ancestor.name,
       ancestor.slug,
       ancestor.path::text,
       ancestor.depth
from target
join departments ancestor
  on ancestor.path @> target.path
 and ancestor.id <> md5('ds22-department:1111')::uuid
 and ancestor.is_active = true
order by ancestor.depth, ancestor.id;

\echo '--- GET /departments/tree/search?q=platform ---'
explain (analyze, buffers)
select id, name, slug, path::text, depth
from departments
where is_active = true
  and name ilike '%platform%'
order by name, id;

\echo '--- location relation lookup by location_id ---'
explain (analyze, buffers)
select dl.department_id
from department_locations dl
join departments d
  on d.id = dl.department_id
 and d.is_active = true
where dl.location_id = md5('ds22-location:42')::uuid;

\echo '--- position relation lookup by position_id ---'
explain (analyze, buffers)
select dp.department_id
from department_positions dp
join departments d
  on d.id = dp.department_id
 and d.is_active = true
where dp.position_id = md5('ds22-position:42')::uuid;

\echo '--- department relation lookup uses leading composite columns ---'
explain (analyze, buffers)
select location_id
from department_locations
where department_id = md5('ds22-department:1111')::uuid;

explain (analyze, buffers)
select position_id
from department_positions
where department_id = md5('ds22-department:1111')::uuid;
