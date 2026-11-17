export type BulkDepartmentAction = "archive" | "restore" | "activate" | "deactivate";

export type BulkDepartmentRequest = {
  ids: string[];
};

export type BulkDepartmentActivityRequest = BulkDepartmentRequest & {
  isActive: boolean;
};

export type BulkDepartmentErrorDto = {
  id: string;
  code: string;
  message: string;
};

export type BulkDepartmentResultDto = {
  succeededIds: string[];
  errors: BulkDepartmentErrorDto[];
  succeededCount: number;
  failedCount: number;
};
