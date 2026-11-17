"use client";

import {
  departmentListQueryOptions,
  DepartmentCard,
} from "@/entities/department";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Checkbox } from "@/shared/ui/checkbox";
import PaginationControls from "@/shared/ui/pagination-controls";
import { useIsMutating, useQuery } from "@tanstack/react-query";
import {
  Building2,
  CircleAlert,
  Network,
  Pencil,
  RefreshCw,
  Search,
} from "lucide-react";
import { ReactNode, useEffect, useRef, useState } from "react";
import LocationMultiSelect from "./location-multi-select";
import Link from "next/link";
import { Archive } from "lucide-react";
import DepartmentArchiveDialog from "@/features/archive-department/department-archive-dialog";
import { useDepartmentArchiveView } from "@/features/archive-department/use-department-archive-view";
import ToggleDepartmentActivity from "@/features/toggle-department-activity/toggle-department-activity";
import DepartmentSelection from "@/features/department-selection/department-selection";
import { DepartmentBulkActionBar } from "@/features/bulk-departments";
import { useSelectedDepartmentsStore } from "@/features/department-selection/store/selected-departments-store";

export default function DepartmentsList({
  focusedDepartment = null,
}: {
  focusedDepartment?: ReactNode;
}) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const { isArchived, changeArchiveView } = useDepartmentArchiveView();
  const isBulkPending =
    useIsMutating({ mutationKey: ["departments", "bulk"] }) > 0;

  const controllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      controllerRef.current?.abort();
      controllerRef.current = null;
    };
  }, []);

  const [filters, setFilters] = useState({
    search: "",
    locationIds: [] as string[],
  });

  const [locationIds, setLocationIds] = useState<string[]>([]);

  const selectedIds = useSelectedDepartmentsStore((state) => state.selectedIds);

  const clearSelection = useSelectedDepartmentsStore(
    (state) => state.clearSelecteion,
  );

  const setSelectedIds = useSelectedDepartmentsStore(
    (state) => state.setSelectedIds,
  );

  const departmentsQuery = useQuery(
    departmentListQueryOptions({
      page,
      pageSize: 20,
      search: filters.search,
      locationIds: filters.locationIds,
      isArchived,
    }),
  );
  const totalPages = departmentsQuery.data?.totalPages;

  if (totalPages !== undefined && page > Math.max(1, totalPages)) {
    setPage(Math.max(1, totalPages));
  }

  useEffect(() => {
    clearSelection();
  }, [isArchived, page, search, filters, locationIds, clearSelection]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1);
      setFilters({
        search: search.trim(),
        locationIds: locationIds,
      });
    }, 400);

    return () => window.clearTimeout(timer);
  }, [search, locationIds]);

  const handleCheckedAll = (nextIsCheckedAll: boolean) => {
    setSelectedIds(
      nextIsCheckedAll === false ? [] : pageItems.map((i) => i.id),
    );
  };

  const pageItems = departmentsQuery.data?.items ?? [];

  const isAllChecked =
    pageItems.length > 0 && pageItems.every((i) => selectedIds.includes(i.id));

  const isSomeChecked = pageItems.some((i) => selectedIds.includes(i.id));

  return (
    <section className="mx-auto w-full max-w-7xl space-y-7">
      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-sm font-medium text-sky-700 dark:text-sky-400">
            <Network className="size-4" />
            Структура компании
          </div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Подразделения
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-base">
            Отделы и команды компании с их местом в общей структуре.
          </p>
        </div>

        {!departmentsQuery.isLoading && departmentsQuery.data !== undefined && (
          <div className="w-fit rounded-full bg-sky-50 px-4 py-2 text-sm font-medium text-sky-800 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
            Всего: {departmentsQuery.data.totalCount}
          </div>
        )}
      </header>

      {focusedDepartment}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm font-medium text-muted-foreground">
          Показывать подразделения
        </p>
        <div
          role="group"
          aria-label="Режим просмотра подразделений"
          className="grid grid-cols-2 gap-1 rounded-xl border bg-muted/60 p-1"
        >
          <Button
            type="button"
            variant="ghost"
            aria-pressed={!isArchived}
            className="gap-2 rounded-lg px-5 aria-pressed:bg-background aria-pressed:text-sky-700 aria-pressed:shadow-sm dark:aria-pressed:text-sky-400"
            onClick={() => {
              changeArchiveView(false);
              setPage(1);
            }}
          >
            <Building2 className="size-4" aria-hidden="true" /> Не в архиве
          </Button>
          <Button
            type="button"
            variant="ghost"
            aria-pressed={isArchived}
            className="gap-2 rounded-lg px-5 aria-pressed:bg-background aria-pressed:text-amber-700 aria-pressed:shadow-sm dark:aria-pressed:text-amber-400"
            onClick={() => {
              changeArchiveView(true);
              setPage(1);
            }}
          >
            <Archive className="size-4" aria-hidden="true" /> Архивные
          </Button>
        </div>
      </div>

      <div
        role="search"
        className="rounded-2xl border bg-linear-to-br from-sky-50/70 via-background to-background p-4 shadow-sm dark:from-sky-950/30 sm:p-5"
      >
        <div className="grid gap-4 sm:grid-cols-2 sm:items-end">
          <div className="min-w-0 space-y-2">
            <label
              htmlFor="department-search"
              className="block text-sm font-medium text-foreground"
            >
              Найти подразделение
            </label>
            <div className="relative">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
              />
              <Input
                id="department-search"
                type="search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Например, отдел разработки"
                className="h-11 rounded-xl bg-background pl-10"
              />
            </div>
          </div>
          <div className="min-w-0">
            <label
              htmlFor="department-location-filter"
              className="mb-2 block text-sm font-medium text-foreground"
            >
              Локации
            </label>
            <LocationMultiSelect
              onChange={(locationIds) => setLocationIds(locationIds)}
            />
          </div>
        </div>
      </div>

      {selectedIds.length > 0 && (
        <>
          <DepartmentBulkActionBar
            selectedIds={selectedIds}
            isArchived={isArchived}
            onClearSelection={clearSelection}
            onSucceeded={(ids) => {
              const succeededIds = new Set(ids);
              useSelectedDepartmentsStore.setState((state) => ({
                selectedIds: state.selectedIds.filter(
                  (id) => !succeededIds.has(id),
                ),
              }));
            }}
          ></DepartmentBulkActionBar>
        </>
      )}

      {departmentsQuery.isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }, (_, index) => (
            <div
              key={index}
              className="h-64 animate-pulse rounded-xl bg-muted ring-1 ring-foreground/5"
            />
          ))}
        </div>
      )}

      {!departmentsQuery.isLoading && departmentsQuery.error !== null && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-destructive/20 bg-linear-to-b from-destructive/5 to-background px-6 py-10 text-center shadow-sm">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive ring-1 ring-destructive/20">
            <CircleAlert className="size-6" />
          </div>
          <h2 className="text-lg font-semibold text-destructive">
            {departmentsQuery.error.message}
          </h2>
          <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">
            Проверь подключение к Directory Service и попробуй ещё раз.
          </p>
          <Button
            type="button"
            size="lg"
            variant="outline"
            onClick={() => void departmentsQuery.refetch()}
            disabled={departmentsQuery.isFetching}
            className="mt-6 border-destructive/20 bg-background px-4 text-foreground shadow-sm hover:border-destructive/30 hover:bg-destructive/5"
          >
            <RefreshCw data-icon="inline-start" />
            Попробовать ещё раз
          </Button>
        </div>
      )}

      {!departmentsQuery.isLoading &&
        departmentsQuery.error === null &&
        departmentsQuery.data?.items.length === 0 && (
          <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-dashed bg-muted/30 px-6 text-center">
            <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-background shadow-sm ring-1 ring-foreground/10">
              <Building2 className="size-6 text-muted-foreground" />
            </div>
            <h2 className="text-lg font-semibold">
              {filters.search || filters.locationIds.length > 0
                ? "Подразделения не найдены"
                : isArchived
                  ? "Архив пуст"
                  : "Подразделений пока нет"}
            </h2>
            <p className="mt-2 max-w-sm text-sm text-muted-foreground">
              {filters.search || filters.locationIds.length > 0
                ? "Попробуй изменить поисковую строку или выбранные локации."
                : isArchived
                  ? "Здесь появятся подразделения, перемещённые в архив."
                  : "Когда в справочник добавят отделы, они появятся здесь."}
            </p>
          </div>
        )}

      {!departmentsQuery.isLoading &&
        departmentsQuery.error === null &&
        departmentsQuery.data !== undefined &&
        departmentsQuery.data.items.length > 0 && (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <div className="col-span-full flex items-center gap-3 rounded-xl border border-sky-200/70 bg-sky-50/40 px-4 py-3 dark:border-sky-900 dark:bg-sky-950/20">
              <Checkbox
                id="select-all-departments"
                checked={isAllChecked}
                indeterminate={!isAllChecked && isSomeChecked}
                disabled={isBulkPending}
                onCheckedChange={(nextIsCheckedAll) =>
                  handleCheckedAll(nextIsCheckedAll)
                }
                aria-describedby="select-all-departments-description"
                className="size-5 rounded-md border-sky-300 focus-visible:ring-sky-400/30 dark:border-sky-700"
              />
              <div className="min-w-0">
                <label
                  htmlFor="select-all-departments"
                  className="cursor-pointer text-sm font-medium"
                >
                  Выбрать все на странице
                </label>
                <p
                  id="select-all-departments-description"
                  className="mt-0.5 text-xs leading-relaxed text-muted-foreground"
                >
                  Только карточки ниже, без отдельной найденной записи.
                </p>
              </div>
            </div>
            {departmentsQuery.data.items.map((department) => (
              <DepartmentCard
                dto={department}
                key={department.id}
                selection={
                  <DepartmentSelection
                    departmentId={department.id}
                    departmentName={department.name}
                    disabled={isBulkPending}
                  />
                }
                actions={
                  <div className="flex flex-wrap items-center gap-1">
                    {!isArchived && (
                      <Link
                        href={`/departments/${department.id}`}
                        className={buttonVariants({
                          variant: "outline",
                          size: "sm",
                        })}
                      >
                        <Pencil data-icon="inline-start" />
                        Редактировать
                      </Link>
                    )}
                    <DepartmentArchiveDialog
                      department={department}
                      action={isArchived ? "restore" : "archive"}
                    />
                    {!isArchived && (
                      <ToggleDepartmentActivity
                        departmentId={department.id}
                        isActive={department.isActive}
                      />
                    )}
                  </div>
                }
              />
            ))}
          </div>
        )}

      {departmentsQuery.data && departmentsQuery.data.totalPages > 1 && (
        <PaginationControls
          page={page}
          totalPages={departmentsQuery.data.totalPages}
          hasNextPage={departmentsQuery.data.hasNextPage}
          setPage={setPage}
        />
      )}
    </section>
  );
}
