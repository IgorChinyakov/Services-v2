"use client";

import { locationListQueryOptions, LocationCard } from "@/entities/location";
import CreateLocationDialog from "@/features/create-location/create-location-dialog";
import { Button } from "@/shared/ui/button";
import { useQuery } from "@tanstack/react-query";
import { Building2, CircleAlert, MapPinned, RefreshCw } from "lucide-react";

export default function LocationsList() {
  const locationsQuery = useQuery(locationListQueryOptions());

  return (
    <section className="mx-auto w-full max-w-7xl space-y-7">
      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-sm font-medium text-emerald-700 dark:text-emerald-400">
            <MapPinned className="size-4" />
            Справочник офисов
          </div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Локации
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-base">
            Офисы, склады и другие места, где работают подразделения компании.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          {!locationsQuery.isLoading && locationsQuery.data !== undefined && (
            <div className="w-fit rounded-full bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800 ring-1 ring-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:ring-emerald-800">
              Всего: {locationsQuery.data.totalCount}
            </div>
          )}
          <CreateLocationDialog />
        </div>
      </header>

      {locationsQuery.isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }, (_, index) => (
            <div
              key={index}
              className="h-72 animate-pulse rounded-xl bg-muted ring-1 ring-foreground/5"
            />
          ))}
        </div>
      )}

      {!locationsQuery.isLoading && locationsQuery.error && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-destructive/20 bg-linear-to-b from-destructive/5 to-background px-6 py-10 text-center shadow-sm">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive ring-1 ring-destructive/20">
            <CircleAlert className="size-6" />
          </div>
          <h2 className="text-lg font-semibold text-destructive">
            {locationsQuery.error.message}
          </h2>
          <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">
            Проверь подключение к Directory Service и попробуй ещё раз.
          </p>
          <Button
            type="button"
            size="lg"
            variant="outline"
            onClick={() => void locationsQuery.refetch()}
            disabled={locationsQuery.isFetching}
            className="mt-6 border-destructive/20 bg-background px-4 text-foreground shadow-sm hover:border-destructive/30 hover:bg-destructive/5"
          >
            <RefreshCw data-icon="inline-start" />
            Попробовать ещё раз
          </Button>
        </div>
      )}

      {!locationsQuery.isLoading &&
        locationsQuery.error === null &&
        locationsQuery.data?.items.length === 0 && (
          <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-dashed bg-muted/30 px-6 text-center">
            <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-background shadow-sm ring-1 ring-foreground/10">
              <Building2 className="size-6 text-muted-foreground" />
            </div>
            <h2 className="text-lg font-semibold">Локаций пока нет</h2>
            <p className="mt-2 max-w-sm text-sm text-muted-foreground">
              Directory Service работает, но в справочник ещё не добавили ни
              одной локации.
            </p>
          </div>
        )}

      {!locationsQuery.isLoading &&
        locationsQuery.error === null &&
        locationsQuery.data !== undefined &&
        locationsQuery.data.items.length > 0 && (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {locationsQuery.data.items.map((item) => (
              <LocationCard dto={item} key={item.id} />
            ))}
          </div>
        )}
    </section>
  );
}
