begin;

insert into departments (
    id,
    name,
    identifier,
    parent_id,
    is_active,
    created_at,
    updated_at,
    depth,
    path)
values
    ('74000000-0000-0000-0000-000000000001', 'Demo Engineering', 'engineering', null, true, '2025-02-01 09:00:00+00', '2025-02-01 09:00:00+00', 0, 'engineering'),
    ('74000000-0000-0000-0000-000000000002', 'Demo Operations', 'operations', null, true, '2025-02-02 09:00:00+00', '2025-02-02 09:00:00+00', 0, 'operations'),
    ('74000000-0000-0000-0000-000000000003', 'Demo Finance', 'finance', null, true, '2025-02-03 09:00:00+00', '2025-02-03 09:00:00+00', 0, 'finance'),
    ('74000000-0000-0000-0000-000000000004', 'Demo Human Resources', 'humanresources', null, true, '2025-02-04 09:00:00+00', '2025-02-04 09:00:00+00', 0, 'humanresources'),
    ('74000000-0000-0000-0000-000000000005', 'Demo Support', 'support', null, true, '2025-02-05 09:00:00+00', '2025-02-05 09:00:00+00', 0, 'support'),
    ('74000000-0000-0000-0000-000000000006', 'Demo Logistics', 'logistics', null, true, '2025-02-06 09:00:00+00', '2025-02-06 09:00:00+00', 0, 'logistics')
on conflict do nothing;

insert into department_locations (id, department_id, location_id)
select
    md5('demo-department-location:' || department.identifier)::uuid,
    department.id,
    location.id
from departments as department
cross join lateral (
    select id
    from locations
    order by created_at, id
    limit 1
) as location
where department.identifier in (
    'engineering',
    'operations',
    'finance',
    'humanresources',
    'support',
    'logistics')
on conflict do nothing;

insert into positions (
    id,
    name,
    description,
    is_active,
    created_at,
    updated_at)
values
    ('75000000-0000-0000-0000-000000000001', 'Demo Software Engineer', 'Develops application features.', true, '2025-03-01 09:00:00+00', '2025-03-01 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000002', 'Demo QA Engineer', 'Tests application behavior.', true, '2025-03-02 09:00:00+00', '2025-03-02 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000003', 'Demo Product Manager', 'Coordinates product delivery.', true, '2025-03-03 09:00:00+00', '2025-03-03 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000004', 'Demo Sales Manager', 'Manages customer sales.', true, '2025-03-04 09:00:00+00', '2025-03-04 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000005', 'Demo Accountant', 'Maintains accounting records.', true, '2025-03-05 09:00:00+00', '2025-03-05 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000006', 'Demo HR Specialist', 'Supports employee processes.', true, '2025-03-06 09:00:00+00', '2025-03-06 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000007', 'Demo Recruiter', 'Inactive position for filter testing.', false, '2025-03-07 09:00:00+00', '2025-03-07 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000008', 'Demo Support Agent', 'Handles support requests.', true, '2025-03-08 09:00:00+00', '2025-03-08 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000009', 'Demo Warehouse Manager', 'Manages warehouse operations.', true, '2025-03-09 09:00:00+00', '2025-03-09 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000010', 'Demo Logistics Coordinator', 'Coordinates deliveries.', true, '2025-03-10 09:00:00+00', '2025-03-10 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000011', 'Demo Security Officer', 'Maintains office security.', true, '2025-03-11 09:00:00+00', '2025-03-11 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000012', 'Demo Analyst', 'Inactive position for filter testing.', false, '2025-03-12 09:00:00+00', '2025-03-12 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000013', 'Demo Designer', 'Designs product interfaces.', true, '2025-03-13 09:00:00+00', '2025-03-13 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000014', 'Demo DevOps Engineer', 'Maintains delivery infrastructure.', true, '2025-03-14 09:00:00+00', '2025-03-14 09:00:00+00'),
    ('75000000-0000-0000-0000-000000000015', 'Demo Office Manager', 'Coordinates office operations.', true, '2025-03-15 09:00:00+00', '2025-03-15 09:00:00+00')
on conflict do nothing;

with requested_links (department_identifier, position_number) as (
    values
        ('headquarters', 1),
        ('headquarters', 2),
        ('headquarters', 3),
        ('headquarters', 4),
        ('headquarters', 5),
        ('headquarters', 6),
        ('headquarters', 7),
        ('headquarters', 8),
        ('headquarters', 9),
        ('headquarters', 10),
        ('engineering', 1),
        ('engineering', 2),
        ('engineering', 3),
        ('engineering', 7),
        ('engineering', 8),
        ('engineering', 11),
        ('engineering', 13),
        ('engineering', 14),
        ('engineering', 15),
        ('sales', 4),
        ('sales', 5),
        ('sales', 8),
        ('sales', 9),
        ('sales', 10),
        ('sales', 12),
        ('sales', 15),
        ('operations', 3),
        ('operations', 8),
        ('operations', 9),
        ('operations', 10),
        ('operations', 11),
        ('operations', 13),
        ('operations', 15),
        ('finance', 5),
        ('finance', 6),
        ('finance', 12),
        ('finance', 14),
        ('finance', 15),
        ('support', 8),
        ('support', 9),
        ('support', 11),
        ('humanresources', 6),
        ('humanresources', 7)
)
insert into department_positions (id, department_id, position_id)
select
    md5(requested_links.department_identifier || ':' || requested_links.position_number)::uuid,
    department.id,
    ('75000000-0000-0000-0000-' || lpad(requested_links.position_number::text, 12, '0'))::uuid
from requested_links
join departments as department
    on department.identifier = requested_links.department_identifier
on conflict do nothing;

commit;

select
    department.identifier,
    count(department_position.position_id) as all_positions_count,
    count(position.id) filter (where position.is_active) as active_positions_count
from departments as department
left join department_positions as department_position
    on department_position.department_id = department.id
left join positions as position
    on position.id = department_position.position_id
where department.identifier in (
    'headquarters',
    'engineering',
    'sales',
    'operations',
    'finance',
    'support',
    'humanresources',
    'logistics')
group by department.id, department.identifier
order by all_positions_count desc, department.identifier;
