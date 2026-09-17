begin;

with generated_locations as (
    select
        ('71000000-0000-0000-0000-' || lpad(number::text, 12, '0'))::uuid as id,
        number,
        (array[
            'Moscow',
            'Kazan',
            'London',
            'Tokyo',
            'Berlin',
            'Novosibirsk',
            'Yekaterinburg',
            'Samara',
            'Omsk',
            'Tula',
            'Perm',
            'Sochi'
        ])[1 + ((number * 7) % 12)] as city,
        case number % 3
            when 0 then 'Office'
            when 1 then 'Warehouse'
            else 'Hub'
        end as location_type
    from generate_series(1, 36) as numbers(number)
),
seed_locations as (
    select
        id,
        format('Demo %s %s %s', city, location_type, lpad(number::text, 2, '0')) as name,
        city,
        format('Demo Street %s', lpad(number::text, 2, '0')) as street,
        format('%sA', number) as house,
        format('Office %s', number) as apartment,
        number % 5 <> 0 as is_active,
        timestamptz '2025-01-01 09:00:00+00'
            + (((number * 11) % 37) * interval '1 day') as created_at
    from generated_locations
)
insert into locations (
    id,
    name,
    city,
    street,
    house,
    apartment,
    is_active,
    created_at,
    updated_at)
select
    id,
    name,
    city,
    street,
    house,
    apartment,
    is_active,
    created_at,
    created_at + interval '2 hours'
from seed_locations
on conflict do nothing;

insert into department_locations (id, department_id, location_id)
select
    ('72000000-0000-0000-0000-' || lpad(number::text, 12, '0'))::uuid,
    department.id,
    ('71000000-0000-0000-0000-' || lpad(number::text, 12, '0'))::uuid
from generate_series(1, 36) as numbers(number)
cross join departments as department
where department.slug = 'headquarters'
  and (number % 2 = 0 or number <= 6)
on conflict do nothing;

insert into department_locations (id, department_id, location_id)
select
    ('73000000-0000-0000-0000-' || lpad(number::text, 12, '0'))::uuid,
    department.id,
    ('71000000-0000-0000-0000-' || lpad(number::text, 12, '0'))::uuid
from generate_series(1, 36) as numbers(number)
cross join departments as department
where department.slug = 'sales'
  and (number % 3 = 0 or number between 25 and 30)
on conflict do nothing;

commit;

select
    count(*) filter (where name like 'Demo %') as demo_locations,
    count(*) filter (where name like 'Demo %' and is_active) as active_demo_locations,
    count(*) filter (where name like 'Demo %' and not is_active) as inactive_demo_locations
from locations;

select
    department.slug,
    department.id as department_id,
    count(*) as demo_location_count
from department_locations as department_location
join departments as department on department.id = department_location.department_id
join locations as location on location.id = department_location.location_id
where location.name like 'Demo %'
group by department.slug, department.id
order by department.slug;
