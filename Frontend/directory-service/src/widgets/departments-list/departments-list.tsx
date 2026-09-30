"use client";

import { departmentListQueryOptions, DepartmentCard } from "@/entities/department";
import { Button } from "@/shared/ui/button";
import { useQuery } from "@tanstack/react-query";
import { Building2, CircleAlert, Network, RefreshCw } from "lucide-react";

export default function DepartmentsList() {
  const departmentsQuery = useQuery(departmentListQueryOptions());

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
    </section>
  );
}
