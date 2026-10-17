"use client";

import { PositionCard, positionListQueryOptions } from "@/entities/position";
import { Button } from "@/shared/ui/button";
import { Spinner } from "@/shared/ui/spinner";
import { useInfiniteQuery } from "@tanstack/react-query";
import {
  BriefcaseBusiness,
  CheckCircle2,
  CircleAlert,
  RefreshCw,
} from "lucide-react";
import { useEffect, useRef } from "react";

export default function PositionsList() {
  const positionsQuery = useInfiniteQuery(positionListQueryOptions());
  const positions =
    positionsQuery.data?.pages.flatMap((page) => page.items) ?? [];

  const { hasNextPage, isFetching, isFetchNextPageError, fetchNextPage } =
    positionsQuery;

  const loaderRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const element = loaderRef.current;

    if (!element || !hasNextPage || isFetching || isFetchNextPageError) {
      return;
    }

    const observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) {
        observer.unobserve(element);
        void fetchNextPage();
      }
    });

    observer.observe(element);

    return () => observer.disconnect();
  }, [hasNextPage, isFetching, isFetchNextPageError, fetchNextPage]);

  return (
    <section className="mx-auto w-full max-w-7xl space-y-7">
      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-sm font-medium text-amber-700 dark:text-amber-400">
            <BriefcaseBusiness className="size-4" />
            Каталог должностей
          </div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Позиции
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-base">
            Должности и роли в подразделениях компании.
          </p>
        </div>

        {/* Счётчик показывает только позиции из уже загруженных страниц. */}
        {positionsQuery.data && (
          <div className="w-fit rounded-full bg-amber-50 px-4 py-2 text-sm font-medium text-amber-900 ring-1 ring-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:ring-amber-800">
            Показано: {positions.length}
          </div>
        )}
      </header>

      {/* Пока первая страница загружается, показываем карточки-заглушки. */}
      {positionsQuery.isPending && (
        <div
          className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          aria-label="Загрузка позиций"
        >
          {Array.from({ length: 6 }, (_, index) => (
            <div
              key={index}
              className="h-64 animate-pulse rounded-xl bg-muted ring-1 ring-foreground/5"
            />
          ))}
        </div>
      )}

      {/* Ошибка первой загрузки: данных для показа ещё нет. */}
      {positionsQuery.isError && !positionsQuery.data && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-destructive/20 bg-linear-to-b from-destructive/5 to-background px-6 py-10 text-center shadow-sm">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive ring-1 ring-destructive/20">
            <CircleAlert className="size-6" />
          </div>
          <h2 className="text-lg font-semibold text-destructive">
            Не удалось загрузить позиции
          </h2>
          <p className="mt-2 max-w-md text-sm leading-relaxed text-muted-foreground">
            {positionsQuery.error.message}
          </p>
          <Button
            type="button"
            size="lg"
            variant="outline"
            onClick={() => void positionsQuery.refetch()}
            disabled={positionsQuery.isFetching}
            className="mt-6"
          >
            <RefreshCw data-icon="inline-start" />
            Попробовать ещё раз
          </Button>
        </div>
      )}

      {/* Успешный запрос с пустым списком не считаем ошибкой. */}
      {positionsQuery.isSuccess && positions.length === 0 && (
        <div className="flex min-h-72 flex-col items-center justify-center rounded-2xl border border-dashed bg-muted/30 px-6 text-center">
          <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-background shadow-sm ring-1 ring-foreground/10">
            <BriefcaseBusiness className="size-6 text-muted-foreground" />
          </div>
          <h2 className="text-lg font-semibold">Позиций пока нет</h2>
          <p className="mt-2 max-w-sm text-sm text-muted-foreground">
            Когда в справочник добавят должности, они появятся здесь.
          </p>
        </div>
      )}

      {/* Позиции из всех загруженных страниц выводятся одним списком. */}
      {positions.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {positions.map((position) => (
            <PositionCard dto={position} key={position.id} />
          ))}
        </div>
      )}

      {positionsQuery.isFetchingNextPage && (
        <div
          role="status"
          className="flex items-center justify-center gap-3 border-t pt-6 text-sm font-medium text-muted-foreground"
        >
          <Spinner
            className="text-amber-700 dark:text-amber-400"
            aria-hidden="true"
          />
          Загружаем следующую порцию позиций...
        </div>
      )}

      {positionsQuery.isSuccess && positions.length > 0 && !hasNextPage && (
        <div className="flex items-center gap-4 py-2 text-center text-sm text-muted-foreground">
          <span className="h-px flex-1 bg-border" aria-hidden="true" />
          <p className="flex items-center justify-center gap-2">
            <CheckCircle2
              className="size-4 text-amber-700 dark:text-amber-400"
              aria-hidden="true"
            />
            Все позиции загружены
          </p>
          <span className="h-px flex-1 bg-border" aria-hidden="true" />
        </div>
      )}

      {positionsQuery.isFetchNextPageError && (
        <div className="flex flex-col gap-4 rounded-2xl border border-destructive/20 bg-destructive/5 p-5 sm:flex-row sm:items-center sm:justify-between">
          <div role="alert" className="flex min-w-0 items-start gap-3">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-destructive/10 text-destructive">
              <CircleAlert className="size-5" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="font-medium text-destructive">
                Не удалось загрузить следующую порцию
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                Уже загруженные позиции остались в списке.
              </p>
              {positionsQuery.error?.message && (
                <p className="mt-2 wrap-break-word text-sm text-destructive">
                  {positionsQuery.error.message}
                </p>
              )}
            </div>
          </div>
          <Button
            type="button"
            size="lg"
            variant="outline"
            className="w-full shrink-0 sm:w-auto"
            onClick={() => void positionsQuery.fetchNextPage()}
            disabled={positionsQuery.isFetching}
          >
            <RefreshCw className="size-4" aria-hidden="true" />
            {positionsQuery.isFetchingNextPage
              ? "Загружаем..."
              : "Попробовать ещё раз"}
          </Button>
        </div>
      )}

      <div ref={loaderRef}></div>
    </section>
  );
}
