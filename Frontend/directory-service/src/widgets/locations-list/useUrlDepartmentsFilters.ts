import type { DepartmentListItemDto } from "@/entities/department";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

export function useUrlDepartmentsFilters() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  return {
    handleDepartmentsChange: (
      departments: DepartmentListItemDto[],
      setPage: (page: number) => void,
    ) => {
      const params = new URLSearchParams(searchParams.toString());
      const ids = departments.map((department) => department.id);

      if (ids.length > 0) {
        params.set("departmentIds", ids.join(","));
      } else {
        params.delete("departmentIds");
      }

      params.delete("page"); // при смене фильтра вернуться на первую страницу
      router.replace(`${pathname}?${params.toString()}`);

      setPage(1);
    },
    urlDepartmentIds: [
      ...new Set(
        (searchParams.get("departmentIds") ?? "").split(",").filter(Boolean),
      ),
    ],
  };
}
