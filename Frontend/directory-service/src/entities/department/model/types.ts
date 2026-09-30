import type { PagedResult } from "@/shared/api";

export type DepartmentNodeDto = {
  id: string;
  parentId: string | null;
  name: string;
  identifier: string;
  path: string;
  depth: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  hasMoreChildren: boolean;
};

export type RootDepartmentDto = DepartmentNodeDto & {
  children: DepartmentNodeDto[];
};

export type TopDepartmentByPositionsDto = {
  id: string;
  name: string;
  identifier: string;
  positionsCount: number;
};

export type RootDepartmentsPagedResultDto = PagedResult<RootDepartmentDto>;

export type DepartmentChildrenPagedResultDto = PagedResult<DepartmentNodeDto>;

export type DepartmentCursorPageDto = {
  items: DepartmentNodeDto[];
  nextCursor: string | null;
  hasNextPage: boolean;
};

export type GetAllDepartmentsParams = {
  cursor?: string;
  size?: number;
};

export type GetRootDepartmentsParams = {
  page?: number;
  size?: number;
  prefetch?: number;
};

export type GetDepartmentChildrenParams = {
  parentId: string;
  page?: number;
  size?: number;
};

export type PagedResultDepartmentNodeDto = PagedResult<DepartmentNodeDto>;

export type DepartmentListItemDto = {
  id: string;
  name: string;
  slug: string;
  path: string;
  createdAt: string;
};

export type DepartmentsPagedResultDto = PagedResult<DepartmentListItemDto>;
