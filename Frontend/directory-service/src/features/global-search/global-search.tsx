"use client";

import { Button } from "@/shared/ui/button";
import { Skeleton } from "@/shared/ui/skeleton";
import {
  Command,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/shared/ui/command";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/shared/ui/dialog";
import { useQuery } from "@tanstack/react-query";
import {
  BriefcaseBusiness,
  Building2,
  CircleAlert,
  MapPin,
  RefreshCw,
  Search,
  type LucideIcon,
} from "lucide-react";
import { useEffect, useState } from "react";
import { globalSearchQueryOptions } from "./api/global-search-query-options";
import { useRouter } from "next/navigation";

function SearchResultItem({
  icon: Icon,
  title,
  subtitle,
  value,
  onSelect,
}: {
  icon: LucideIcon;
  title: string;
  subtitle: string;
  value: string;
  onSelect: () => void;
}) {
  return (
    <CommandItem
      value={value}
      onSelect={onSelect}
      className="gap-3 rounded-xl px-3 py-3"
    >
      <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-sky-50 text-sky-700 ring-1 ring-sky-100 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-900">
        <Icon aria-hidden="true" className="size-4" />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-medium">{title}</span>
        <span className="mt-0.5 block truncate text-xs text-muted-foreground">
          {subtitle}
        </span>
      </span>
    </CommandItem>
  );
}

function SearchResultsSkeleton() {
  return (
    <div role="status" className="space-y-1 pt-2">
      <span className="sr-only">Ищем в справочнике…</span>
      <div aria-hidden="true">
        {["w-40", "w-52", "w-32"].map((width) => (
          <div key={width} className="flex items-center gap-3 px-3 py-3">
            <Skeleton className="size-10 shrink-0 rounded-xl motion-reduce:animate-none" />
            <div className="min-w-0 flex-1 space-y-2">
              <Skeleton
                className={`h-4 max-w-full ${width} motion-reduce:animate-none`}
              />
              <Skeleton className="h-3 w-64 max-w-full motion-reduce:animate-none" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default function GlobalSearch() {
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState(false);
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const router = useRouter();

  const isDebouncing = search.trim() !== debouncedSearch;

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
    }, 400);

    return () => window.clearTimeout(timer);
  }, [search]);

  const searchQuery = useQuery(globalSearchQueryOptions(debouncedSearch));
  const hasSearchText = search.trim().length >= 2;
  const isSearchLoading =
    hasSearchText && (isDebouncing || searchQuery.isLoading);

  const locations =
    searchQuery.data?.filter((d) => d.type === "location") ?? [];
  const departments =
    searchQuery.data?.filter((d) => d.type === "department") ?? [];
  const positions =
    searchQuery.data?.filter((d) => d.type === "position") ?? [];

  useEffect(() => {
    function handleKeydown(event: KeyboardEvent) {
      const isShortcut =
        (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k";

      if (!isShortcut) return;

      event.preventDefault();
      setOpen(true);
    }

    window.addEventListener("keydown", handleKeydown);

    return () => {
      window.removeEventListener("keydown", handleKeydown);
    };
  }, []);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        render={
          <Button
            type="button"
            variant="outline"
            aria-label="Открыть глобальный поиск"
            className="ml-auto size-9 shrink-0 gap-2 rounded-xl text-muted-foreground sm:h-10 sm:w-auto sm:min-w-64 sm:justify-start sm:px-3"
          />
        }
      >
        <Search aria-hidden="true" className="size-4" />
        <span className="hidden flex-1 text-left sm:inline">
          Найти в справочнике
        </span>
      </DialogTrigger>

      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-0 overflow-hidden rounded-2xl p-0 sm:max-w-2xl">
        <DialogHeader className="gap-2 border-b bg-linear-to-br from-sky-50/80 via-background to-background px-5 py-5 pr-12 dark:from-sky-950/30">
          <div className="flex items-center gap-2 text-xs font-medium text-sky-700 dark:text-sky-400">
            <Search aria-hidden="true" className="size-3.5" />
            Directory Service
          </div>
          <DialogTitle className="text-xl font-semibold">
            Поиск по справочнику
          </DialogTitle>
          <DialogDescription className="text-xs leading-relaxed sm:text-sm">
            Локации, подразделения и позиции в одном месте.
          </DialogDescription>
        </DialogHeader>

        <Command
          shouldFilter={false}
          className="rounded-none! bg-transparent p-3 **:data-[slot=input-group]:h-11!"
        >
          <CommandInput
            value={search}
            onValueChange={setSearch}
            placeholder="Название локации, подразделения или позиции"
            aria-label="Поиск по справочнику"
            maxLength={200}
          />
          {!hasSearchText && (
            <p className="px-3 pt-2 pb-3 text-xs text-muted-foreground">
              Введите минимум 2 символа.
            </p>
          )}

          {isSearchLoading && <SearchResultsSkeleton />}

          {hasSearchText && !isSearchLoading && searchQuery.isError && (
            <div
              role="alert"
              className="mx-1 mt-3 rounded-xl border border-destructive/20 bg-destructive/5 p-4"
            >
              <div className="flex items-start gap-3">
                <CircleAlert
                  aria-hidden="true"
                  className="mt-0.5 size-4 shrink-0 text-destructive"
                />
                <div>
                  <p className="text-sm font-medium">Не удалось выполнить поиск</p>
                  <p className="mt-1 text-xs leading-relaxed text-muted-foreground">
                    Проверь подключение и попробуй ещё раз. Поисковый запрос сохранён.
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="mt-3 gap-2"
                    disabled={searchQuery.isFetching}
                    onClick={() => void searchQuery.refetch()}
                  >
                    <RefreshCw aria-hidden="true" className="size-4" />
                    Попробовать ещё раз
                  </Button>
                </div>
              </div>
            </div>
          )}

          {hasSearchText && !isSearchLoading && searchQuery.isSuccess && (
            <CommandList
              className="max-h-[min(45dvh,24rem)]"
              label="Результаты поиска"
            >
              {searchQuery.data.length === 0 && (
                <div role="status" className="px-3 py-10 text-center">
                  <Search
                    aria-hidden="true"
                    className="mx-auto mb-3 size-6 text-muted-foreground/60"
                  />
                  <p className="text-sm font-medium">Ничего не найдено</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Попробуй изменить поисковый запрос.
                  </p>
                </div>
              )}
              {locations.length > 0 && (
                <CommandGroup heading="Локации">
                  {locations.map((current) => (
                    <SearchResultItem
                      key={current.id}
                      icon={MapPin}
                      title={current.title}
                      subtitle={current.subtitle}
                      value={`location:${current.id}`}
                      onSelect={() => {
                        router.push(`/locations?focusId=${current.id}`);
                        setOpen(false);
                      }}
                    />
                  ))}
                </CommandGroup>
              )}
              {locations.length > 0 &&
                (departments.length > 0 || positions.length > 0) && (
                  <CommandSeparator alwaysRender className="mx-2 my-1" />
                )}
              {departments.length > 0 && (
                <CommandGroup heading="Подразделения">
                  {departments.map((current) => (
                    <SearchResultItem
                      key={current.id}
                      icon={Building2}
                      title={current.title}
                      subtitle={current.subtitle}
                      value={`department:${current.id}`}
                      onSelect={() => {
                        router.push(`/departments?focusId=${current.id}`);
                        setOpen(false);
                      }}
                    />
                  ))}
                </CommandGroup>
              )}
              {departments.length > 0 && positions.length > 0 && (
                <CommandSeparator alwaysRender className="mx-2 my-1" />
              )}
              {positions.length > 0 && (
                <CommandGroup heading="Позиции">
                  {positions.map((current) => (
                    <SearchResultItem
                      key={current.id}
                      icon={BriefcaseBusiness}
                      title={current.title}
                      subtitle={current.subtitle}
                      value={`position:${current.id}`}
                      onSelect={() => {
                        router.push(`/positions?focusId=${current.id}`);
                        setOpen(false);
                      }}
                    />
                  ))}
                </CommandGroup>
              )}
            </CommandList>
          )}
        </Command>

        <div className="flex items-center justify-between gap-3 border-t bg-muted/30 px-5 py-3">
          <span className="text-xs text-muted-foreground">
            Стрелки: выбор. Enter: открыть.
          </span>
          <DialogClose render={<Button variant="ghost" size="sm" />}>
            Закрыть
            <kbd className="rounded border bg-background px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">
              Esc
            </kbd>
          </DialogClose>
        </div>
      </DialogContent>
    </Dialog>
  );
}
