"use client";

import {
  departmentListQueryOptions,
  DepartmentCard,
} from "@/entities/department";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import PaginationControls from "@/shared/ui/pagination-controls";
import { useQuery } from "@tanstack/react-query";
import {
  Building2,
  CircleAlert,
  Network,
  RefreshCw,
  Search,
} from "lucide-react";
import { useEffect, useState } from "react";
import LocationMultiSelect from "./location-multi-select";

export default function DepartmentsList() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const [filters, setFilters] = useState({
    search: "",
    locationIds: [] as string[],
  });

  const [locationIds, setLocationIds] = useState<string[]>([]);

  const departmentsQuery = useQuery(
    departmentListQueryOptions(page, 20, filters.search, filters.locationIds),
  );
  const totalPages = departmentsQuery.data?.totalPages;

  if (totalPages !== undefined && page > Math.max(1, totalPages)) {
    setPage(Math.max(1, totalPages));
  }

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
            <h2 className="text-lg font-semibold">Подразделений пока нет</h2>
            <p className="mt-2 max-w-sm text-sm text-muted-foreground">
              Когда в справочник добавят отделы, они появятся здесь.
            </p>
          </div>
        )}

      {!departmentsQuery.isLoading &&
        departmentsQuery.error === null &&
        departmentsQuery.data !== undefined &&
        departmentsQuery.data.items.length > 0 && (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {departmentsQuery.data.items.map((department) => (
              <DepartmentCard dto={department} key={department.id} />
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
