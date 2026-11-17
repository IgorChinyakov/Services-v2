"use client";

import { ApiRequestError } from "@/shared/api/error";
import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import { isAxiosError } from "axios";
import { CircleAlert, RefreshCw, Search, X } from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useId, type ReactNode } from "react";

const tones = {
  sky: {
    panel: "border-sky-200 from-sky-50/80 dark:border-sky-900 dark:from-sky-950/30",
    icon: "bg-sky-100 text-sky-700 dark:bg-sky-950 dark:text-sky-300",
  },
  emerald: {
    panel: "border-emerald-200 from-emerald-50/80 dark:border-emerald-900 dark:from-emerald-950/30",
    icon: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300",
  },
  amber: {
    panel: "border-amber-200 from-amber-50/80 dark:border-amber-900 dark:from-amber-950/30",
    icon: "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
  },
};

export function FocusedResultPanel({
  tone,
  isPending,
  isFetching,
  error,
  hasData,
  onRetry,
  children,
}: {
  tone: keyof typeof tones;
  isPending: boolean;
  isFetching: boolean;
  error: Error | null;
  hasData: boolean;
  onRetry: () => void;
  children: ReactNode;
}) {
  const headingId = useId();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const status =
    error instanceof ApiRequestError
      ? error.status
      : isAxiosError(error)
        ? error.response?.status
        : undefined;
  const message = status === 404
    ? "Запись могла быть удалена, перемещена в архив или стать недоступной. Попробуй найти её снова."
    : status === 400
      ? "Ссылка на запись некорректна. Убери выделение и повтори поиск."
      : status === 403
        ? "У тебя нет доступа к этой записи."
        : "Проверь подключение и попробуй ещё раз.";

  function clearFocus() {
    const params = new URLSearchParams(searchParams.toString());
    params.delete("focusId");
    const query = params.toString();
    router.replace(`${pathname}${query ? `?${query}` : ""}`, { scroll: false });
  }

  return (
    <section
      aria-labelledby={headingId}
      aria-busy={isFetching}
      className={`relative overflow-hidden rounded-2xl border bg-linear-to-br via-background to-background p-4 shadow-sm sm:p-6 ${tones[tone].panel}`}
    >
      <header className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-start gap-3">
          <span
            className={`flex size-10 shrink-0 items-center justify-center rounded-xl ${tones[tone].icon}`}
          >
            <Search aria-hidden="true" className="size-4" />
          </span>
          <div>
            <h2 id={headingId} className="text-base font-semibold">
              Найдено через поиск
            </h2>
            <p className="mt-1 text-xs leading-relaxed text-muted-foreground sm:text-sm">
              Выбранная запись показана отдельно от общего списка.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={clearFocus}
          className="w-fit shrink-0 gap-2"
        >
          <X aria-hidden="true" className="size-4" />
          Убрать выделение
        </Button>
      </header>

      {isPending && (
        <div role="status" className="rounded-xl border bg-background/80 p-5">
          <span className="sr-only">Загрузка найденной записи…</span>
          <div aria-hidden="true" className="space-y-5">
            <div className="flex items-center gap-3">
              <Skeleton className="size-11 shrink-0 rounded-xl motion-reduce:animate-none" />
              <div className="min-w-0 flex-1 space-y-2">
                <Skeleton className="h-5 w-56 max-w-full motion-reduce:animate-none" />
                <Skeleton className="h-4 w-24 motion-reduce:animate-none" />
              </div>
            </div>
            <Skeleton className="h-24 w-full motion-reduce:animate-none" />
            <Skeleton className="h-4 w-44 max-w-full motion-reduce:animate-none" />
          </div>
        </div>
      )}

      {error && (
        <div
          role="alert"
          className="mb-4 flex flex-col gap-4 rounded-xl border border-destructive/20 bg-destructive/5 p-4 sm:flex-row sm:items-start"
        >
          <CircleAlert
            aria-hidden="true"
            className="size-5 shrink-0 text-destructive"
          />
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium">
              {hasData
                ? "Не удалось обновить запись"
                : status === 404
                  ? "Запись не найдена"
                  : "Не удалось открыть запись"}
            </p>
            <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
              {message}
            </p>
            {hasData && (
              <p className="mt-1 text-xs text-muted-foreground">
                Ниже показаны ранее загруженные данные.
              </p>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isFetching}
              onClick={onRetry}
              className="mt-3 gap-2"
            >
              <RefreshCw
                aria-hidden="true"
                className={`size-4 ${isFetching ? "animate-spin motion-reduce:animate-none" : ""}`}
              />
              {isFetching ? "Повторяем запрос…" : "Попробовать ещё раз"}
            </Button>
          </div>
        </div>
      )}
      {children}
    </section>
  );
}
