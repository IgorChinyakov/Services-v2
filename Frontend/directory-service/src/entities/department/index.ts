export { fetchDepartments } from "./api/list/fetch-departments";
export { fetchDepartmentById } from "./api/details/fetch-department-by-id";
export { departmentListQueryOptions } from "./api/list/department-list-query-options";
export { default as DepartmentCard } from "./ui/department-card";

export type {
  DepartmentTreeNodeDto,
  DepartmentChildrenPagedResultDto,
  DepartmentDetailsDto,
  DepartmentCursorPageDto,
  DepartmentNodeDto,
  GetAllDepartmentsParams,
  GetDepartmentChildrenParams,
  GetRootDepartmentsParams,
  RootDepartmentDto,
  RootDepartmentsPagedResultDto,
  TopDepartmentByPositionsDto,
  DepartmentListItemDto,
  DepartmentsPagedResultDto,
} from "./model/types";
