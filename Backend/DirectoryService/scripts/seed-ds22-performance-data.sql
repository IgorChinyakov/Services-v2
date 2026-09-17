\set ON_ERROR_STOP on

begin;

-- Ten roots, then three increasingly wide levels. The deterministic ids and
-- slugs make the script idempotent and give benchmark queries stable targets.
insert into departments (
    id,
    name,
    slug,
    parent_id,
    is_active,
    created_at,
    updated_at,
    deleted_at,
    depth,
    path)
select
    md5('ds22-department:' || number)::uuid,
    format('DS22 Root Department %s', lpad(number::text, 2, '0')),
    format('ds22root%s', lpad(number::text, 2, '0')),
    null,
    true,
    timestamptz '2025-01-01 00:00:00+00' + number * interval '1 minute',
    timestamptz '2025-01-01 00:00:00+00' + number * interval '1 minute',
    null,
    0,
    format('ds22root%s', lpad(number::text, 2, '0'))::ltree
from generate_series(1, 10) as numbers(number)
on conflict do nothing;

insert into departments (
    id,
    name,
    slug,
    parent_id,
    is_active,
    created_at,
    updated_at,
    deleted_at,
    depth,
    path)
select
    md5('ds22-department:' || number)::uuid,
    format('DS22 Department %s', lpad(number::text, 5, '0')),
    format('ds22dept%s', lpad(number::text, 5, '0')),
    parent.id,
    true,
    timestamptz '2025-01-02 00:00:00+00' + number * interval '1 minute',
    timestamptz '2025-01-02 00:00:00+00' + number * interval '1 minute',
    null,
    1,
    (parent.path::text || '.' || format('ds22dept%s', lpad(number::text, 5, '0')))::ltree
from generate_series(11, 110) as numbers(number)
join departments as parent
  on parent.id = md5(
      'ds22-department:' || (1 + ((number - 11) / 10))::text)::uuid
on conflict do nothing;

insert into departments (
    id,
    name,
    slug,
    parent_id,
    is_active,
    created_at,
    updated_at,
    deleted_at,
    depth,
    path)
select
    md5('ds22-department:' || number)::uuid,
    case
        when number % 97 = 0
            then format('DS22 Platform Department %s', lpad(number::text, 5, '0'))
        else format('DS22 Department %s', lpad(number::text, 5, '0'))
    end,
    format('ds22dept%s', lpad(number::text, 5, '0')),
    parent.id,
    true,
    timestamptz '2025-01-03 00:00:00+00' + number * interval '1 minute',
    timestamptz '2025-01-03 00:00:00+00' + number * interval '1 minute',
    null,
    2,
    (parent.path::text || '.' || format('ds22dept%s', lpad(number::text, 5, '0')))::ltree
from generate_series(111, 1110) as numbers(number)
join departments as parent
  on parent.id = md5(
      'ds22-department:' || (11 + ((number - 111) / 10))::text)::uuid
on conflict do nothing;

insert into departments (
    id,
    name,
    slug,
    parent_id,
    is_active,
    created_at,
    updated_at,
    deleted_at,
    depth,
    path)
select
    md5('ds22-department:' || number)::uuid,
    case
        when number % 97 = 0
            then format('DS22 Platform Department %s', lpad(number::text, 5, '0'))
        else format('DS22 Department %s', lpad(number::text, 5, '0'))
    end,
    format('ds22dept%s', lpad(number::text, 5, '0')),
    parent.id,
    number % 10 <> 0,
    timestamptz '2025-01-04 00:00:00+00' + number * interval '1 minute',
    timestamptz '2025-01-04 00:00:00+00' + number * interval '1 minute',
    case when number % 10 = 0 then timestamptz '2025-06-01 00:00:00+00' else null end,
    3,
    (parent.path::text || '.' || format('ds22dept%s', lpad(number::text, 5, '0')))::ltree
from generate_series(1111, 10000) as numbers(number)
join departments as parent
  on parent.id = md5(
      'ds22-department:' || (111 + ((number - 1111) / 10))::text)::uuid
on conflict do nothing;

insert into locations (
    id,
    name,
    city,
    street,
    house,
    apartment,
    is_active,
    created_at,
    updated_at,
    deleted_at)
select
    md5('ds22-location:' || number)::uuid,
    format('DS22 Location %s', lpad(number::text, 3, '0')),
    format('DS22 City %s', 1 + number % 20),
    format('Performance Street %s', lpad(number::text, 3, '0')),
    number::text,
    format('Office %s', number),
    true,
    timestamptz '2025-02-01 00:00:00+00' + number * interval '1 hour',
    timestamptz '2025-02-01 00:00:00+00' + number * interval '1 hour',
    null
from generate_series(1, 300) as numbers(number)
on conflict do nothing;

insert into positions (
    id,
    name,
    description,
    is_active,
    created_at,
    updated_at,
    deleted_at)
select
    md5('ds22-position:' || number)::uuid,
    format('DS22 Position %s', lpad(number::text, 3, '0')),
    format('Performance test position %s', number),
    true,
    timestamptz '2025-03-01 00:00:00+00' + number * interval '1 hour',
    timestamptz '2025-03-01 00:00:00+00' + number * interval '1 hour',
    null
from generate_series(1, 300) as numbers(number)
on conflict do nothing;

insert into department_locations (id, department_id, location_id)
select
    md5(format('ds22-department-location:%s:%s', department_number, link_number))::uuid,
    md5('ds22-department:' || department_number)::uuid,
    md5(
        'ds22-location:' ||
        (1 + ((department_number + link_number * 97) % 300))::text)::uuid
from generate_series(1, 10000) as departments(department_number)
cross join generate_series(0, 2) as links(link_number)
on conflict do nothing;

insert into department_positions (id, department_id, position_id)
select
    md5(format('ds22-department-position:%s:%s', department_number, link_number))::uuid,
    md5('ds22-department:' || department_number)::uuid,
    md5(
        'ds22-position:' ||
        (1 + ((department_number + link_number * 89) % 300))::text)::uuid
from generate_series(1, 10000) as departments(department_number)
cross join generate_series(0, 2) as links(link_number)
on conflict do nothing;

commit;

analyze departments;
analyze locations;
analyze positions;
analyze department_locations;
analyze department_positions;

select
    (select count(*) from departments where slug like 'ds22%') as departments,
    (select count(*) from locations where name like 'DS22 Location %') as locations,
    (select count(*) from positions where name like 'DS22 Position %') as positions,
    (select count(*)
     from department_locations
     where id in (
         select md5(format('ds22-department-location:%s:%s', department_number, link_number))::uuid
         from generate_series(1, 10000) as departments(department_number)
         cross join generate_series(0, 2) as links(link_number))) as department_locations,
    (select count(*)
     from department_positions
     where id in (
         select md5(format('ds22-department-position:%s:%s', department_number, link_number))::uuid
         from generate_series(1, 10000) as departments(department_number)
         cross join generate_series(0, 2) as links(link_number))) as department_positions;
