import { departmentsChildrenQueryOptions } from "@/entities/department/api/children/departments-children-query-options";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import { Spinner } from "@/shared/ui/spinner";
import { useQuery } from "@tanstack/react-query";
import { Building2, ChevronRight, TriangleAlert } from "lucide-react";
import { useDepartmentTreeStore } from "./store/department-tree-store";

export function DepartmentChildren({ departmentId }: { departmentId: string }) {
  const childrenQuery = useQuery(departmentsChildrenQueryOptions(departmentId));

  const expandedIds = useDepartmentTreeStore((state) => state.expandedIds);
  const selected = useDepartmentTreeStore((state) => state.selected);
  const toggleChildren = useDepartmentTreeStore(
    (state) => state.toggleExpanded,
  );
  const selectDepartment = useDepartmentTreeStore(
    (state) => state.toggleSelected,
  );

  if (childrenQuery.isLoading) {
    return (
      <div
        className="space-y-2"
        role="status"
        aria-label="Загрузка дочерних подразделений"
      >
        {["w-2/3", "w-1/2"].map((width, index) => (
          <div
            key={index}
            className="flex min-h-11 items-center gap-3 rounded-lg px-3"
          >
            <Skeleton className="size-4 shrink-0" />
            <Skeleton className={`h-4 ${width}`} />
          </div>
        ))}
      </div>
    );
  }

  if (childrenQuery.isError) {
    return (
      <div
        role="alert"
        className="flex flex-wrap items-center gap-3 rounded-lg bg-destructive/5 px-3 py-3 text-sm text-destructive"
      >
        <TriangleAlert className="size-4 shrink-0" aria-hidden="true" />
        <span className="min-w-0 flex-1">
          Не удалось загрузить дочерние подразделения.
        </span>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void childrenQuery.refetch()}
        >
          Повторить
        </Button>
      </div>
    );
  }

  if (!childrenQuery.data?.length) {
    return (
      <p className="rounded-lg bg-muted/40 px-3 py-3 text-sm text-muted-foreground">
        У этого подразделения нет дочерних отделов.
      </p>
    );
  }

  return (
    <div className="space-y-2">
      {childrenQuery.isRefetching && (
        <div
          className="flex items-center gap-2 px-3 text-xs text-muted-foreground"
          role="status"
        >
          <Spinner
            className="size-3"
            aria-label="Обновление дочерних подразделений"
          />
          Обновление списка
        </div>
      )}
      <ul className="space-y-1" aria-label="Дочерние подразделения">
        {childrenQuery.data.map((child) => {
          const isExpanded = expandedIds.includes(child.id);
          const isSelected = selected.id === child.id;

          return (
            <li key={child.id} className="min-w-0">
              <div
                className={`flex min-h-11 min-w-0 items-center rounded-lg border pr-2 text-foreground transition-colors ${isSelected ? "border-sky-300 bg-sky-50 text-sky-950 dark:border-sky-800 dark:bg-sky-950/40 dark:text-sky-100" : "border-transparent hover:bg-muted/60"}`}
              >
                {child.hasChildren ? (
                  <button
                    type="button"
                    aria-label={`${isExpanded ? "Свернуть" : "Раскрыть"} дочерние подразделения: ${child.name}`}
                    aria-expanded={isExpanded}
                    onClick={() => toggleChildren(child.id)}
                    className="flex size-9 shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-sky-100/80 focus-visible:outline-2 focus-visible:outline-sky-600 dark:hover:bg-sky-950"
                  >
                    <ChevronRight
                      className={`size-4 transition-transform ${isExpanded ? "rotate-90" : ""}`}
                      aria-hidden="true"
                    />
                  </button>
                ) : (
                  <span className="size-9 shrink-0" aria-hidden="true" />
                )}
                <button
                  type="button"
                  aria-pressed={isSelected}
                  onClick={() =>
                    selectDepartment({ id: child.id, name: child.name })
                  }
                  className="flex min-h-10 min-w-0 flex-1 items-center gap-2 rounded-md px-1 text-left text-sm focus-visible:outline-2 focus-visible:outline-sky-600"
                >
                  <Building2
                    className="size-4 shrink-0 text-sky-700 dark:text-sky-400"
                    aria-hidden="true"
                  />
                  <span className="min-w-0 flex-1 truncate font-medium">
                    {child.name}
                  </span>
                </button>
              </div>
              {isExpanded && (
                <div className="ml-7 border-l py-2 pl-3">
                  <DepartmentChildren departmentId={child.id} />
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
