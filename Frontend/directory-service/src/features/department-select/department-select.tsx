"use client";

import {
  fetchDepartments,
  type DepartmentListItemDto,
} from "@/entities/department";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { useInfiniteQuery } from "@tanstack/react-query";
import { Building2, Check, ChevronDown, Search, X } from "lucide-react";
import { useEffect, useId, useState } from "react";

export type Props = {
  value: DepartmentListItemDto[];
  onChange: (departments: DepartmentListItemDto[]) => void;
  excludeIds?: string[];
  excludeSubtreeOf?: string;
  mode: "single" | "multiple";
  label?: string;
  disabled?: boolean;
};

export default function DepartmentSelect({
  value,
  onChange,
  excludeIds = [],
  excludeSubtreeOf,
  mode,
  label = "Подразделения",
  disabled = false,
}: Props) {
  const panelId = useId();
  const [open, setOpen] = useState(false);
  const [previousDisabled, setPreviousDisabled] = useState(disabled);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(searchInput.trim()), 400);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  if (disabled !== previousDisabled) {
    setPreviousDisabled(disabled);
    if (disabled) setOpen(false);
  }

  const isOpen = open && !disabled;

  const departmentsQuery = useInfiniteQuery({
    queryKey: [
      "departments",
      "select",
      search,
      isActive,
      excludeIds,
      excludeSubtreeOf,
    ],
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      fetchDepartments({
        signal,
        page: pageParam,
        pageSize: 20,
        search,
        locationIds: null,
        excludeIds: excludeIds.length > 0 ? excludeIds : null,
        excludeSubtreeOf,
        isActive,
      }),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
    enabled: isOpen,
  });

  const departments =
    departmentsQuery.data?.pages.flatMap((page) => page.items) ?? [];
  const selectedIds = new Set(value.map((department) => department.id));
  const waitingForSearch = searchInput.trim() !== search;

  function toggle(department: DepartmentListItemDto) {
    if (disabled) return;

    if (selectedIds.has(department.id)) {
      onChange(value.filter((item) => item.id !== department.id));
      return;
    }

    onChange(mode === "single" ? [department] : [...value, department]);
    if (mode === "single") setOpen(false);
  }

  return (
    <div className="relative w-full min-w-0">
      <div className="mb-2 flex items-center justify-between gap-3">
        <span id={`${panelId}-label`} className="text-sm font-medium">
          {label}
        </span>
        {value.length > 0 && (
          <Button
            type="button"
            variant="ghost"
            size="xs"
            disabled={disabled}
            onClick={() => onChange([])}
          >
            Сбросить
          </Button>
        )}
      </div>

      <button
        type="button"
        aria-labelledby={`${panelId}-label`}
        aria-expanded={isOpen}
        aria-controls={panelId}
        onClick={() => setOpen((current) => !current)}
        className="flex min-h-12 w-full items-center gap-2 rounded-xl border bg-background px-3 py-2 text-left shadow-xs transition-colors hover:border-sky-300 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:border-input"
        disabled={disabled}
      >
        <Building2
          aria-hidden="true"
          className="size-4 shrink-0 text-sky-700"
        />
        <span className="min-w-0 flex-1 truncate text-sm">
          {value.length === 0
            ? mode === "single"
              ? "Выбери подразделение"
              : "Выбери подразделения"
            : mode === "single"
              ? value[0].name
              : `Выбрано подразделений: ${value.length}`}
        </span>
        <ChevronDown
          aria-hidden="true"
          className="size-4 shrink-0 text-muted-foreground"
        />
      </button>

      {value.length > 0 && (
        <div
          className="mt-2 flex flex-wrap gap-1.5"
          aria-label="Выбранные подразделения"
        >
          {value.map((department) => (
            <button
              key={department.id}
              type="button"
              onClick={() => toggle(department)}
              disabled={disabled}
              aria-label={`Убрать ${department.name}`}
              className="inline-flex items-center gap-1.5 rounded-full border bg-sky-50 px-2.5 py-1 text-xs font-medium text-sky-900 transition-colors hover:bg-sky-100 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-sky-950 dark:text-sky-200"
            >
              {department.name}
              <X aria-hidden="true" className="size-3" />
            </button>
          ))}
        </div>
      )}

      {isOpen && (
        <div
          id={panelId}
          className="absolute z-30 mt-2 w-full overflow-hidden rounded-2xl border bg-popover shadow-xl ring-1 ring-foreground/5"
        >
          <div className="space-y-3 border-b bg-linear-to-br from-sky-50/70 to-background p-3 dark:from-sky-950/30">
            <div className="relative">
              <Search
                aria-hidden="true"
                className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
              />
              <Input
                autoFocus
                type="search"
                aria-label="Поиск подразделений"
                placeholder="Найти подразделение"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                className="h-10 rounded-lg bg-background pl-9"
              />
            </div>
            <div
              className="flex gap-1 rounded-lg bg-muted/70 p-1"
              aria-label="Статус подразделений"
            >
              {(
                [
                  { label: "Активные", value: true },
                  { label: "Неактивные", value: false },
                ] as const
              ).map((option) => (
                <button
                  key={option.label}
                  type="button"
                  aria-pressed={isActive === option.value}
                  onClick={() => setIsActive(option.value)}
                  className={`flex-1 rounded-md px-2 py-1.5 text-xs font-medium transition-colors ${isActive === option.value ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground"}`}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </div>

          <div className="max-h-64 overflow-y-auto p-1.5">
            {(waitingForSearch || departmentsQuery.isPending) && (
              <p
                role="status"
                className="px-3 py-6 text-center text-sm text-muted-foreground"
              >
                Загрузка подразделений...
              </p>
            )}
            {!waitingForSearch &&
              departmentsQuery.isError &&
              !departmentsQuery.data && (
                <div className="space-y-2 px-3 py-4 text-center">
                  <p role="alert" className="text-sm text-destructive">
                    Не удалось загрузить подразделения.
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => void departmentsQuery.refetch()}
                  >
                    Повторить
                  </Button>
                </div>
              )}
            {!waitingForSearch &&
              departmentsQuery.isSuccess &&
              departments.length === 0 && (
                <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                  Ничего не найдено.
                </p>
              )}
            {!waitingForSearch &&
              departments.map((department) => (
                <button
                  key={department.id}
                  type="button"
                  aria-pressed={selectedIds.has(department.id)}
                  onClick={() => toggle(department)}
                  className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left transition-colors hover:bg-sky-50 focus-visible:bg-sky-50 focus-visible:outline-none dark:hover:bg-sky-950 dark:focus-visible:bg-sky-950"
                >
                  <span
                    className={`flex size-5 shrink-0 items-center justify-center border ${mode === "single" ? "rounded-full" : "rounded"} ${selectedIds.has(department.id) ? "border-sky-700 bg-sky-700 text-white" : "border-input"}`}
                  >
                    {selectedIds.has(department.id) && (
                      <Check aria-hidden="true" className="size-3.5" />
                    )}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-medium">
                      {department.name}
                    </span>
                    <span className="block truncate text-xs text-muted-foreground">
                      {department.path}
                    </span>
                  </span>
                  <span className="text-[11px] text-muted-foreground">
                    {department.isActive ? "Активен" : "Неактивен"}
                  </span>
                </button>
              ))}
          </div>

          {!waitingForSearch && departmentsQuery.hasNextPage && (
            <div className="border-t p-2">
              {departmentsQuery.isFetchNextPageError && (
                <p
                  role="alert"
                  className="pb-2 text-center text-xs text-destructive"
                >
                  Не удалось загрузить следующую страницу.
                </p>
              )}
              <Button
                type="button"
                variant="ghost"
                className="w-full"
                disabled={departmentsQuery.isFetchingNextPage}
                onClick={() => void departmentsQuery.fetchNextPage()}
              >
                {departmentsQuery.isFetchingNextPage
                  ? "Загрузка..."
                  : "Показать ещё"}
              </Button>
            </div>
          )}
          <div className="flex items-center justify-between border-t bg-muted/25 px-3 py-2 text-xs text-muted-foreground">
            <span>
              {value.length === 0
                ? "Ничего не выбрано"
                : `Выбрано: ${value.length}`}
            </span>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="font-medium text-sky-700 hover:underline dark:text-sky-400"
            >
              Готово
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
