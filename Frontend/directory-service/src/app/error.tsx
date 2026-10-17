"use client";

import { Button } from "@/shared/ui/button";
import { CircleAlert, RefreshCw, RotateCcw } from "lucide-react";

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <section className="relative mx-auto flex min-h-[min(70vh,44rem)] w-full max-w-7xl items-center justify-center overflow-hidden rounded-3xl border border-destructive/15 bg-linear-to-br from-destructive/5 via-background to-emerald-50/50 px-5 py-14 dark:to-emerald-950/10 sm:px-10">
      <div className="pointer-events-none absolute -right-20 -top-24 size-72 rounded-full border border-destructive/10" />
      <div className="pointer-events-none absolute -right-8 -top-12 size-48 rounded-full border border-destructive/10" />

      <div className="relative w-full max-w-xl rounded-2xl border bg-background/95 p-6 shadow-xl shadow-foreground/5 sm:p-9">
        <div className="mb-7 flex size-14 items-center justify-center rounded-2xl bg-destructive/10 text-destructive ring-1 ring-destructive/15">
          <CircleAlert className="size-7" aria-hidden="true" />
        </div>

        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-destructive">
          Ошибка приложения
        </p>
        <h1 className="mt-3 text-2xl font-semibold tracking-tight sm:text-3xl">
          Не удалось показать страницу
        </h1>
        <p className="mt-4 max-w-md text-sm leading-6 text-muted-foreground sm:text-base">
          Во время отображения этого раздела произошёл непредвиденный сбой.
          Попробуй повторить загрузку, а если ошибка останется — обнови страницу.
        </p>

        <div className="mt-8 flex flex-col gap-3 sm:flex-row">
          <Button type="button" size="lg" onClick={() => reset()}>
            <RotateCcw data-icon="inline-start" aria-hidden="true" />
            Повторить
          </Button>
          <Button
            type="button"
            size="lg"
            variant="outline"
            onClick={() => window.location.reload()}
          >
            <RefreshCw data-icon="inline-start" aria-hidden="true" />
            Обновить страницу
          </Button>
        </div>

        {process.env.NODE_ENV === "development" && (
          <details className="mt-8 border-t pt-5 text-sm text-muted-foreground">
            <summary className="cursor-pointer font-medium text-foreground">
              Детали для разработки
            </summary>
            <p className="mt-3 break-words font-mono text-xs leading-5">
              {error.message}
            </p>
            {error.digest && (
              <p className="mt-2 font-mono text-xs">Код: {error.digest}</p>
            )}
          </details>
        )}
      </div>
    </section>
  );
}
