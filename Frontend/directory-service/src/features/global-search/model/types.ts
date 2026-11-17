export type SearchEntityType = "location" | "department" | "position";

export type GlobalSearchResultDto = {
  type: SearchEntityType;
  id: string;
  title: string;
  subtitle: string;
  isActive: boolean;
  departmentId: string | null;
};
