export { bulkArchiveDepartments } from "./api/bulk-archive-departments";
export { bulkRestoreDepartments } from "./api/bulk-restore-departments";
export { bulkUpdateDepartmentActivity } from "./api/bulk-update-department-activity";
export { default as DepartmentBulkConfirmDialog } from "./department-bulk-confirm-dialog";
export { default as DepartmentBulkActionBar } from "./department-bulk-action-bar";
export type {
  BulkDepartmentAction,
  BulkDepartmentRequest,
  BulkDepartmentActivityRequest,
  BulkDepartmentErrorDto,
  BulkDepartmentResultDto,
} from "./model/types";
