begin;

with child_seed (number, parent_identifier, identifier, name) as (
    values
        (1, 'headquarters', 'headquartersstrategy', 'Demo Corporate Strategy'),
        (2, 'headquarters', 'headquarterslegal', 'Demo Legal'),
        (3, 'headquarters', 'headquarterssecurity', 'Demo Corporate Security'),
        (4, 'headquarters', 'headquartersadministration', 'Demo Administration'),

        (5, 'engineering', 'engineeringbackend', 'Demo Backend Engineering'),
        (6, 'engineering', 'engineeringfrontend', 'Demo Frontend Engineering'),
        (7, 'engineering', 'engineeringquality', 'Demo Quality Assurance'),
        (8, 'engineering', 'engineeringplatform', 'Demo Platform Engineering'),
        (9, 'engineering', 'engineeringmobile', 'Demo Mobile Engineering'),
        (10, 'engineering', 'engineeringdata', 'Demo Data Engineering'),

        (11, 'operations', 'operationsprocurement', 'Demo Procurement'),
        (12, 'operations', 'operationsfacilities', 'Demo Facilities'),
        (13, 'operations', 'operationsfulfillment', 'Demo Fulfillment'),
        (14, 'operations', 'operationsplanning', 'Demo Operations Planning'),
        (15, 'operations', 'operationscontrol', 'Demo Operations Control'),

        (16, 'finance', 'financeaccounting', 'Demo Accounting'),
        (17, 'finance', 'financetreasury', 'Demo Treasury'),
        (18, 'finance', 'financepayroll', 'Demo Payroll'),
        (19, 'finance', 'financeaudit', 'Demo Internal Audit'),
        (20, 'finance', 'financeplanning', 'Demo Financial Planning'),

        (21, 'humanresources', 'humanresourcesrecruiting', 'Demo Recruiting'),
        (22, 'humanresources', 'humanresourceslearning', 'Demo Learning and Development'),
        (23, 'humanresources', 'humanresourcescompensation', 'Demo Compensation and Benefits'),
        (24, 'humanresources', 'humanresourcespartners', 'Demo HR Business Partners'),
        (25, 'humanresources', 'humanresourcesculture', 'Demo People and Culture'),

        (26, 'support', 'supportcustomersuccess', 'Demo Customer Success'),
        (27, 'support', 'supportservicedesk', 'Demo Service Desk'),
        (28, 'support', 'supporttechnical', 'Demo Technical Support'),
        (29, 'support', 'supportonboarding', 'Demo Customer Onboarding'),
        (30, 'support', 'supportknowledge', 'Demo Knowledge Management'),

        (31, 'logistics', 'logisticswarehousing', 'Demo Warehousing'),
        (32, 'logistics', 'logisticstransportation', 'Demo Transportation'),
        (33, 'logistics', 'logisticsinventory', 'Demo Inventory Control'),
        (34, 'logistics', 'logisticsdispatch', 'Demo Dispatch'),
        (35, 'logistics', 'logisticsrouting', 'Demo Route Planning')
)
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
select
    ('76000000-0000-0000-0000-' || lpad(child_seed.number::text, 12, '0'))::uuid,
    child_seed.name,
    child_seed.identifier,
    parent.id,
    true,
    timestamptz '2025-04-01 09:00:00+00' + child_seed.number * interval '1 hour',
    timestamptz '2025-04-01 09:00:00+00' + child_seed.number * interval '1 hour',
    parent.depth + 1,
    (parent.path::text || '.' || child_seed.identifier)::ltree
from child_seed
join departments as parent
    on parent.identifier = child_seed.parent_identifier
on conflict do nothing;

with grandchild_seed (number, parent_identifier, identifier, name) as (
    values
        (1, 'headquartersstrategy', 'headquartersresearch', 'Demo Corporate Research'),
        (2, 'headquartersstrategy', 'headquartersinitiatives', 'Demo Strategic Initiatives'),
        (3, 'engineeringbackend', 'engineeringbackendapi', 'Demo Backend API Team'),
        (4, 'engineeringbackend', 'engineeringbackendintegration', 'Demo Integration Team'),
        (5, 'engineeringbackend', 'engineeringbackendstorage', 'Demo Storage Team'),
        (6, 'engineeringplatform', 'engineeringplatformcloud', 'Demo Cloud Platform'),
        (7, 'engineeringplatform', 'engineeringplatformdelivery', 'Demo Delivery Platform'),
        (8, 'operationsfulfillment', 'operationspicking', 'Demo Picking Team'),
        (9, 'operationsfulfillment', 'operationspacking', 'Demo Packing Team'),
        (10, 'operationsfulfillment', 'operationsshipping', 'Demo Shipping Team'),
        (11, 'financeaccounting', 'financepayables', 'Demo Accounts Payable'),
        (12, 'financeaccounting', 'financereceivables', 'Demo Accounts Receivable'),
        (13, 'humanresourcesrecruiting', 'humanresourcestechnicalrecruiting', 'Demo Technical Recruiting'),
        (14, 'humanresourcesrecruiting', 'humanresourcesoperationsrecruiting', 'Demo Operations Recruiting'),
        (15, 'supporttechnical', 'supportlevelone', 'Demo Support Level One'),
        (16, 'supporttechnical', 'supportleveltwo', 'Demo Support Level Two'),
        (17, 'logisticswarehousing', 'logisticsnorthwarehouse', 'Demo North Warehouse'),
        (18, 'logisticswarehousing', 'logisticssouthwarehouse', 'Demo South Warehouse')
)
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
select
    ('76100000-0000-0000-0000-' || lpad(grandchild_seed.number::text, 12, '0'))::uuid,
    grandchild_seed.name,
    grandchild_seed.identifier,
    parent.id,
    true,
    timestamptz '2025-05-01 09:00:00+00' + grandchild_seed.number * interval '1 hour',
    timestamptz '2025-05-01 09:00:00+00' + grandchild_seed.number * interval '1 hour',
    parent.depth + 1,
    (parent.path::text || '.' || grandchild_seed.identifier)::ltree
from grandchild_seed
join departments as parent
    on parent.identifier = grandchild_seed.parent_identifier
on conflict do nothing;

insert into department_locations (id, department_id, location_id)
select
    md5('demo-tree-department-location:' || department.identifier)::uuid,
    department.id,
    location.id
from departments as department
cross join lateral (
    select id
    from locations
    order by created_at, id
    limit 1
) as location
where department.id::text like '76000000-%'
   or department.id::text like '76100000-%'
on conflict do nothing;

commit;

select
    parent.name as root_name,
    count(child.id) as direct_children
from departments as parent
left join departments as child
    on child.parent_id = parent.id
where parent.parent_id is null
group by parent.id, parent.name
order by parent.name;

select
    parent.name as department_name,
    count(child.id) as direct_children
from departments as parent
join departments as child
    on child.parent_id = parent.id
where parent.depth = 1
group by parent.id, parent.name
order by parent.name;
