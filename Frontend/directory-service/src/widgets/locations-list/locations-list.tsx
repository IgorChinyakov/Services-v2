"use client";

import {
  fetchLocations,
  LocationCard,
  type LocationsPagedResultDto,
} from "@/entities/location";
import { Button } from "@/shared/ui/button";
import { Building2, CircleAlert, MapPinned, RefreshCw } from "lucide-react";
import { useEffect, useState } from "react";

export default function LocationsList() {
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<LocationsPagedResultDto | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    async function loadLocations() {
      try {
        setError(null);
        setIsLoading(true);

        const data = await fetchLocations(controller.signal);

        setData(data);
      } catch {
        if (controller.signal.aborted) {
          return;
        }
        setError("Не удалось загрузить локации");
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadLocations();

    return () => {
      controller.abort();
    };
  }, [retryCount]);

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

        {!isLoading && data !== null && (
          <div className="w-fit rounded-full bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800 ring-1 ring-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:ring-emerald-800">
            Всего: {data.totalCount}
          </div>
        )}
      </header>

      {isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }, (_, index) => (
            <div
              key={index}
              className="h-72 animate-pulse rounded-xl bg-muted ring-1 ring-foreground/5"
            />
          ))}
        </div>
      )}

      {!isLoading && error !== null && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-destructive/20 bg-linear-to-b from-destructive/5 to-background px-6 py-10 text-center shadow-sm">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive ring-1 ring-destructive/20">
            <CircleAlert className="size-6" />
          </div>
          <h2 className="text-lg font-semibold text-destructive">{error}</h2>
          <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">
            Проверь подключение к Directory Service и попробуй ещё раз.
          </p>
          <Button
            type="button"
            size="lg"
            variant="outline"
            onClick={() => setRetryCount((current) => current + 1)}
            className="mt-6 border-destructive/20 bg-background px-4 text-foreground shadow-sm hover:border-destructive/30 hover:bg-destructive/5"
          >
            <RefreshCw data-icon="inline-start" />
            Попробовать ещё раз
          </Button>
        </div>
      )}

      {!isLoading && error === null && data?.items.length === 0 && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-dashed bg-muted/30 px-6 text-center">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-background shadow-sm ring-1 ring-foreground/10">
            <Building2 className="size-6 text-muted-foreground" />
          </div>
          <h2 className="text-lg font-semibold">Локаций пока нет</h2>
          <p className="mt-2 max-w-sm text-sm text-muted-foreground">
            Directory Service работает, но в справочник ещё не добавили ни одной
            локации.
          </p>
        </div>
      )}

      {!isLoading && error === null && data && data.items.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {data.items.map((item) => (
            <LocationCard dto={item} key={item.id} />
          ))}
        </div>
      )}
    </section>
  );
}
