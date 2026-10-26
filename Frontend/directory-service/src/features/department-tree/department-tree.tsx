"use client";

import { departmentsTreeQueryOptions } from "@/entities/department/api/tree/departments-tree-query-options";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";
import { useQuery } from "@tanstack/react-query";
import {
  Building2,
  ChevronRight,
  FolderTree,
  TriangleAlert,
} from "lucide-react";
import { DepartmentChildren } from "./department-children";
import { useDepartmentTreeStore } from "./store/department-tree-store";

export default function DepartmentTree() {
  const expandedIds = useDepartmentTreeStore((state) => state.expandedIds);
  const selected = useDepartmentTreeStore((state) => state.selected);
  const toggleExpanded = useDepartmentTreeStore(
    (state) => state.toggleExpanded,
  );
  const selectDepartment = useDepartmentTreeStore(
    (state) => state.toggleSelected,
  );

  const treeQuery = useQuery(departmentsTreeQueryOptions);

  const nodes = treeQuery.data ?? [];

  return (
    <Card className="min-w-0 gap-0 overflow-hidden rounded-2xl border bg-card py-0 shadow-sm ring-0">
      <CardHeader className="gap-4 border-b bg-linear-to-br from-sky-50/80 via-card to-card px-5 py-5 dark:from-sky-950/30 sm:px-6">
        <div className="flex items-start gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-sky-100 text-sky-700 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
            <FolderTree className="size-5" aria-hidden="true" />
          </span>
          <div className="min-w-0 space-y-1">
            <CardTitle className="text-lg font-semibold">
              Подразделения
            </CardTitle>
            <CardDescription>
              Выбери отдел, чтобы увидеть его позиции.
            </CardDescription>
          </div>
        </div>
      </CardHeader>

      <CardContent className="px-4 py-5 sm:px-5">
        <div className="mb-3 flex items-center justify-between gap-3 px-2 text-xs font-medium tracking-wide text-muted-foreground uppercase">
          <span>Корневой уровень</span>
          {!treeQuery.isLoading && !treeQuery.isError && (
            <span aria-label={`Подразделений: ${nodes.length}`}>
              {nodes.length}
            </span>
          )}
        </div>

        {treeQuery.isLoading && (
          <div
            className="space-y-2"
            role="status"
            aria-label="Загрузка подразделений"
          >
            {["w-3/5", "w-4/5", "w-1/2"].map((width, index) => (
              <div
                key={index}
                className="flex h-14 items-center gap-3 rounded-xl border px-4"
              >
                <Skeleton className="size-5 shrink-0" />
                <Skeleton className={`h-4 ${width}`} />
              </div>
            ))}
          </div>
        )}

        {treeQuery.isError && (
          <div
            role="alert"
            className="flex gap-3 rounded-xl border border-destructive/25 bg-destructive/5 p-4 text-sm text-destructive"
          >
            <TriangleAlert
              className="mt-0.5 size-4 shrink-0"
              aria-hidden="true"
            />
            <p>{treeQuery.error.message}</p>
          </div>
        )}

        {!treeQuery.isLoading && !treeQuery.isError && nodes.length === 0 && (
          <div className="flex min-h-48 flex-col items-center justify-center rounded-xl border border-dashed bg-muted/25 px-5 text-center">
            <Building2
              className="mb-3 size-8 text-muted-foreground"
              aria-hidden="true"
            />
            <p className="font-medium">Подразделений пока нет</p>
            <p className="mt-1 max-w-xs text-sm text-muted-foreground">
              Корневые отделы появятся здесь после загрузки.
            </p>
          </div>
        )}

        {!treeQuery.isLoading && !treeQuery.isError && nodes.length > 0 && (
          <ul className="space-y-1" aria-label="Корневые подразделения">
            {nodes.map((node) => {
              const isExpanded = expandedIds.includes(node.id);
              const isSelected = selected.id === node.id;

              return (
                <li key={node.id}>
                  <div
                    className={`flex min-h-12 min-w-0 items-center rounded-xl border pr-2 transition-colors ${isSelected ? "border-sky-300 bg-sky-50 text-sky-950 dark:border-sky-800 dark:bg-sky-950/40 dark:text-sky-100" : "border-transparent hover:bg-muted/60"}`}
                  >
                    {node.hasChildren ? (
                      <button
                        type="button"
                        aria-label={`${isExpanded ? "Свернуть" : "Раскрыть"} дочерние подразделения: ${node.name}`}
                        aria-expanded={isExpanded}
                        onClick={() => toggleExpanded(node.id)}
                        className="flex size-10 shrink-0 items-center justify-center rounded-lg text-muted-foreground hover:bg-sky-100/80 focus-visible:outline-2 focus-visible:outline-sky-600 dark:hover:bg-sky-950"
                      >
                        <ChevronRight
                          className={`size-4 transition-transform ${isExpanded ? "rotate-90" : ""}`}
                          aria-hidden="true"
                        />
                      </button>
                    ) : (
                      <span className="size-10 shrink-0" aria-hidden="true" />
                    )}
                    <button
                      type="button"
                      aria-pressed={isSelected}
                      onClick={() =>
                        selectDepartment({ id: node.id, name: node.name })
                      }
                      className="flex min-h-11 min-w-0 flex-1 items-center gap-2 rounded-lg px-1 text-left text-sm focus-visible:outline-2 focus-visible:outline-sky-600"
                    >
                      <Building2
                        className="size-4 shrink-0 text-sky-700 dark:text-sky-400"
                        aria-hidden="true"
                      />
                      <span className="min-w-0 flex-1 truncate font-medium">
                        {node.name}
                      </span>
                    </button>
                  </div>
                  {isExpanded && (
                    <div className="ml-9 border-l py-2 pl-3">
                      <DepartmentChildren departmentId={node.id} />
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
