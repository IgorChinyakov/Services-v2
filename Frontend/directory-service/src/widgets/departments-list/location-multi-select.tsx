"use client";

import { fetchLocations, type LocationDto } from "@/entities/location";
import {
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxChipsInput,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  ComboboxValue,
  useComboboxAnchor,
} from "@/shared/ui/combobox";
import { Button } from "@/shared/ui/button";
import { useInfiniteQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";

const pageSize = 20;

type Props = {
  onChange: (ids: string[]) => void;
};

export default function LocationMultiSelect({ onChange }: Props) {
  const [selectedLocations, setSelectedLocations] = useState<LocationDto[]>([]);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(searchInput.trim()), 400);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  const locationsQuery = useInfiniteQuery({
    queryKey: ["locations", "multi-select", search],
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      fetchLocations(signal, pageSize, pageParam, search),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
  });
  const locations =
    locationsQuery.data?.pages.flatMap((page) => page.items) ?? [];
  const isSearchWaiting = searchInput.trim() !== search;

  const anchor = useComboboxAnchor();

  function updateSelection(selected: LocationDto[]) {
    setSelectedLocations(selected);
    onChange(selected.map((location) => location.id));
  }

  return (
    <Combobox
      multiple
      autoHighlight
      value={selectedLocations}
      items={isSearchWaiting ? [] : locations}
      filter={null}
      itemToStringLabel={(location: LocationDto) => location.name}
      isItemEqualToValue={(item: LocationDto, value: LocationDto) =>
        item.id === value.id
      }
      onValueChange={updateSelection}
      onInputValueChange={setSearchInput}
    >
      <ComboboxChips
        ref={anchor}
        className="min-h-11 w-full rounded-xl bg-background"
      >
        <ComboboxValue>
          {(values) => (
            <>
              {values.map((value: LocationDto) => (
                <ComboboxChip key={value.id}>{value.name}</ComboboxChip>
              ))}
              <ComboboxChipsInput
                id="department-location-filter"
                placeholder={values.length === 0 ? "Выбери локации" : undefined}
              />
            </>
          )}
        </ComboboxValue>
        {selectedLocations.length > 0 && (
          <Button
            type="button"
            size="sm"
            variant="ghost"
            className="h-7 shrink-0 px-2 text-xs text-muted-foreground"
            onClick={() => updateSelection([])}
          >
            Сбросить
          </Button>
        )}
      </ComboboxChips>
      <ComboboxContent anchor={anchor}>
        {locationsQuery.isSuccess && !isSearchWaiting && (
          <ComboboxEmpty>Локации не найдены.</ComboboxEmpty>
        )}
        <ComboboxList>
          {(item) => (
            <ComboboxItem key={item.id} value={item}>
              {item.name}
            </ComboboxItem>
          )}
        </ComboboxList>
        {(isSearchWaiting || locationsQuery.isPending) && (
          <p role="status" className="px-3 py-2 text-sm text-muted-foreground">
            {isSearchWaiting ? "Ищем локации..." : "Загрузка локаций..."}
          </p>
        )}
        {locationsQuery.isError && !locationsQuery.data && !isSearchWaiting && (
          <div className="space-y-1 px-3 py-2">
            <p role="alert" className="text-sm text-destructive">
              Не удалось загрузить локации.
            </p>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => void locationsQuery.refetch()}
              disabled={locationsQuery.isFetching}
            >
              Повторить
            </Button>
          </div>
        )}
        {locationsQuery.hasNextPage && !isSearchWaiting && (
          <div className="border-t p-1">
            {locationsQuery.isFetchNextPageError && (
              <p role="alert" className="px-2 py-1 text-sm text-destructive">
                Не удалось загрузить следующую порцию локаций.
              </p>
            )}
            <Button
              type="button"
              variant="ghost"
              className="w-full"
              disabled={locationsQuery.isFetchingNextPage}
              onClick={() => void locationsQuery.fetchNextPage()}
            >
              {locationsQuery.isFetchingNextPage
                ? "Загрузка..."
                : locationsQuery.isFetchNextPageError
                  ? "Повторить загрузку"
                  : "Показать ещё"}
            </Button>
          </div>
        )}
      </ComboboxContent>
    </Combobox>
  );
}
