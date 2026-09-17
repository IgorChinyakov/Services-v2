\set ON_ERROR_STOP on

begin;

delete from departments
where slug like 'ds22%';

delete from locations
where name like 'DS22 Location %';

delete from positions
where name like 'DS22 Position %';

commit;
