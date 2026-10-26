"use client";

import { positionListQueryOptions } from "@/entities/position";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";
import { Spinner } from "@/shared/ui/spinner";
import { useInfiniteQuery } from "@tanstack/react-query";
import {
  BriefcaseBusiness,
  CircleAlert,
  FolderOpen,
  RefreshCw,
} from "lucide-react";
import { useEffect, useRef } from "react";

export type DepartmentPositionsProps = {
  departmentId: string | null;
  name: string | null;
};

export default function DepartmentPositions({
  departmentId,
  name,
}: DepartmentPositionsProps) {
  return (
    <Card className="min-w-0 gap-0 overflow-hidden rounded-2xl border bg-card py-0 shadow-sm ring-0">
      <CardHeader className="gap-4 border-b bg-linear-to-br from-amber-50/80 via-card to-card px-5 py-5 dark:from-amber-950/25 sm:px-6">
        <div className="flex items-start gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-800 ring-1 ring-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:ring-amber-800">
            <BriefcaseBusiness className="size-5" aria-hidden="true" />
          </span>
          <div className="min-w-0 space-y-1">
            <h2 className="text-lg font-semibold">Позиции подразделения</h2>
            <p className="text-sm text-muted-foreground">
              Должности и роли выбранного отдела.
            </p>
          </div>
        </div>
      </CardHeader>

      <CardContent className="px-5 py-6 sm:px-6">
        {departmentId === null || name === null ? (
          <div className="flex min-h-64 flex-col items-center justify-center rounded-xl border border-dashed bg-muted/20 px-6 text-center">
            <FolderOpen
              className="mb-4 size-9 text-amber-700/70 dark:text-amber-400/70"
              aria-hidden="true"
            />
            <p className="font-medium">Выбери подразделение</p>
            <p className="mt-2 max-w-xs text-sm leading-relaxed text-muted-foreground">
              Нажми на название отдела в дереве, чтобы увидеть его позиции.
            </p>
          </div>
        ) : (
          <SelectedDepartmentPositions
            departmentId={departmentId}
            name={name}
          />
        )}
      </CardContent>
    </Card>
  );
}

function SelectedDepartmentPositions({
  departmentId,
  name,
}: {
  departmentId: string;
  name: string;
}) {
  const positionsQuery = useInfiniteQuery(
    positionListQueryOptions(departmentId),
  );
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
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium">{name}</p>
        {positionsQuery.data && (
          <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-900 dark:bg-amber-950 dark:text-amber-200">
            Позиций: {positions.length}
          </span>
        )}
      </div>

      {positionsQuery.isPending && (
        <div className="space-y-2" role="status" aria-label="Загрузка позиций">
          {[0, 1, 2].map((index) => (
            <div
              key={index}
              className="flex min-h-20 items-center gap-3 rounded-xl border px-4"
            >
              <Skeleton className="size-10 shrink-0" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-2/3" />
                <Skeleton className="h-3 w-1/2" />
              </div>
            </div>
          ))}
        </div>
      )}

      {positionsQuery.isError && !positionsQuery.data && (
        <div
          role="alert"
          className="flex min-h-56 flex-col items-center justify-center rounded-xl border border-destructive/20 bg-destructive/5 px-6 text-center"
        >
          <CircleAlert
            className="mb-3 size-8 text-destructive"
            aria-hidden="true"
          />
          <p className="font-medium text-destructive">
            Не удалось загрузить позиции
          </p>
          <p className="mt-2 max-w-xs text-sm text-muted-foreground">
            Проверь соединение и попробуй ещё раз.
          </p>
          <Button
            type="button"
            variant="outline"
            className="mt-4"
            disabled={positionsQuery.isFetching}
            onClick={() => void positionsQuery.refetch()}
          >
            <RefreshCw data-icon="inline-start" />
            Повторить
          </Button>
        </div>
      )}

      {positionsQuery.isSuccess && positions.length === 0 && (
        <div className="flex min-h-56 flex-col items-center justify-center rounded-xl border border-dashed bg-muted/20 px-6 text-center">
          <BriefcaseBusiness
            className="mb-3 size-8 text-muted-foreground"
            aria-hidden="true"
          />
          <p className="font-medium">В этом отделе пока нет позиций</p>
          <p className="mt-2 max-w-xs text-sm text-muted-foreground">
            Когда позиции добавят, они появятся здесь.
          </p>
        </div>
      )}

      {positions.length > 0 && (
        <ul className="space-y-2" aria-label="Позиции выбранного отдела">
          {positions.map((position) => (
            <li
              key={position.id}
              className="flex min-w-0 gap-3 rounded-xl border bg-background px-4 py-3"
            >
              <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300">
                <BriefcaseBusiness className="size-4" aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <p className="font-medium wrap-break-word">{position.name}</p>
                <p className="mt-1 text-sm leading-relaxed text-muted-foreground wrap-break-word">
                  {position.description?.trim() || "Описание не указано"}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}

      {positionsQuery.isFetchNextPageError && (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-destructive/20 bg-destructive/5 p-3"
        >
          <p className="text-sm text-destructive">
            Следующая порция не загрузилась. Уже полученные позиции сохранены.
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={positionsQuery.isFetching}
            onClick={() => void positionsQuery.fetchNextPage()}
          >
            Повторить
          </Button>
        </div>
      )}

      {positionsQuery.isFetchingNextPage && (
        <div
          role="status"
          className="flex items-center justify-center gap-2 py-3 text-sm text-muted-foreground"
        >
          <Spinner aria-hidden="true" />
          Загружаем следующую порцию позиций...
        </div>
      )}

      <div ref={loaderRef} aria-hidden="true" className="h-1" />
    </div>
  );
}
