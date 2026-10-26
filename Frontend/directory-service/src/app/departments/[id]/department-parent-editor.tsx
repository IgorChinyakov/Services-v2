"use client";

import {
  type DepartmentListItemDto,
  type DepartmentDetailsDto,
} from "@/entities/department";
import { UpdateDepartmentParentRequest } from "@/entities/department/model/types";
import DepartmentSelect from "@/features/department-select/department-select";
import { useEditDepartmentParent } from "@/features/edit-department-parent/use-edit-department-parent";
import GetResult from "@/shared/api/get-result";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Checkbox } from "@/shared/ui/checkbox";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Building2, GitBranch, Save } from "lucide-react";
import Link from "next/link";
import { useEffect, useRef, useState } from "react";

export default function DepartmentParentEditor({
  departmentId,
}: {
  departmentId: string;
}) {
  // TODO: Initialize the current parent selection when its details are available.
  const [selectedParent, setSelectedParent] = useState<DepartmentListItemDto[]>(
    [],
  );
  const [withoutParent, setWithoutParent] = useState(false);

  const controllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    controllerRef.current = controller;

    return () => {
      controller.abort();

      if (controllerRef.current === controller) controllerRef.current = null;
    };
  }, []);

  const editParent = useEditDepartmentParent();

  const currentDepartmentQuery = useQuery({
    queryKey: ["department", departmentId],
    queryFn: (queryParams) =>
      GetResult<DepartmentDetailsDto>({
        url: `departments/${departmentId}`,
        signal: queryParams.signal,
      }),
  });

  const parentId = currentDepartmentQuery.data?.parentId;

  const currentParentQuery = useQuery({
    queryKey: ["department", parentId],
    enabled: parentId != null,
    queryFn: (queryParams) => {
      if (!parentId) throw new Error("Parent ID is missing");

      return GetResult<DepartmentDetailsDto>({
        url: `departments/${parentId}`,
        signal: queryParams.signal,
      });
    },
  });

  const handleEdit = async () => {
    const controller = controllerRef.current;
    if (!controller || editParent.isPending) return;

    const request: UpdateDepartmentParentRequest = {
      parentId: withoutParent ? null : selectedParent[0]?.id,
    };
    try {
      await editParent.mutateAsync({
        departmentId,
        values: request,
        signal: controller.signal,
      });
    } catch {
      // Ошибка уже показана в onError мутации; отмену не показываем.
    }
  };

  function handleWithoutParentChange(checked: boolean) {
    setWithoutParent(checked);
    if (checked) setSelectedParent([]);
  }

  return (
    <div className="mx-auto w-full max-w-5xl space-y-7 pb-12">
      <Link
        href="/departments"
        className="inline-flex items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft aria-hidden="true" className="size-4" />
        Все подразделения
      </Link>

      <header className="flex flex-col gap-4 border-b pb-6 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2 text-sm font-medium text-sky-700 dark:text-sky-400">
            <GitBranch aria-hidden="true" className="size-4" />
            Структура компании
          </div>
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Изменить родителя
          </h1>
          <p className="mt-2 max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-base">
            Выбери новое место подразделения в структуре. Изменение вступит в
            силу только после сохранения.
          </p>
        </div>
        <span className="w-fit rounded-full bg-sky-50 px-3 py-1.5 text-xs font-medium text-sky-800 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
          Редактирование
        </span>
      </header>

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.25fr)]">
        <section className="rounded-2xl border bg-linear-to-br from-sky-50/70 via-background to-background p-5 shadow-sm dark:from-sky-950/25 sm:p-6">
          <div className="mb-5 flex size-11 items-center justify-center rounded-xl bg-sky-100 text-sky-700 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
            <Building2 aria-hidden="true" className="size-5" />
          </div>
          <h2 className="text-lg font-semibold">Редактируемый отдел</h2>
          {currentDepartmentQuery.isPending && (
            <p role="status" className="mt-4 text-sm text-muted-foreground">
              Загружаем данные подразделения...
            </p>
          )}
          {currentDepartmentQuery.isError && (
            <div className="mt-4 space-y-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4">
              <p role="alert" className="text-sm text-destructive">
                Не удалось загрузить данные подразделения.
              </p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => void currentDepartmentQuery.refetch()}
              >
                Повторить
              </Button>
            </div>
          )}
          {currentDepartmentQuery.data && (
            <div className="mt-4 space-y-4">
              <div className="flex flex-wrap items-center gap-2">
                <p className="text-xl font-semibold tracking-tight">
                  {currentDepartmentQuery.data.name}
                </p>
                <span className="rounded-full bg-background px-2.5 py-1 text-xs font-medium ring-1 ring-foreground/10">
                  {currentDepartmentQuery.data.isActive
                    ? "Активен"
                    : "Неактивен"}
                </span>
              </div>
              <dl className="grid gap-3 rounded-xl bg-background/80 p-4 ring-1 ring-foreground/7 sm:grid-cols-2">
                <div className="sm:col-span-2">
                  <dt className="text-xs font-medium text-muted-foreground">
                    Текущий родитель
                  </dt>
                  <dd className="mt-1 break-all text-sm">
                    {parentId === null && "Нет, корневое подразделение"}
                    {parentId !== null && currentParentQuery.isPending && (
                      <span role="status" className="text-muted-foreground">
                        Загружаем родителя...
                      </span>
                    )}
                    {parentId !== null && currentParentQuery.isError && (
                      <span className="flex flex-wrap items-center gap-2">
                        <span role="alert" className="text-destructive">
                          Не удалось загрузить родителя.
                        </span>
                        <Button
                          type="button"
                          variant="outline"
                          size="xs"
                          onClick={() => void currentParentQuery.refetch()}
                        >
                          Повторить
                        </Button>
                      </span>
                    )}
                    {currentParentQuery.data && (
                      <span className="block space-y-1">
                        <span className="block font-medium">
                          {currentParentQuery.data.name}
                        </span>
                        <span className="block font-mono text-xs text-muted-foreground">
                          {currentParentQuery.data.path}
                        </span>
                        <span className="block font-mono text-xs text-muted-foreground">
                          ID: {currentParentQuery.data.id}
                        </span>
                      </span>
                    )}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-muted-foreground">
                    Slug
                  </dt>
                  <dd className="mt-1 break-all font-mono text-sm">
                    {currentDepartmentQuery.data.slug}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-muted-foreground">
                    Уровень
                  </dt>
                  <dd className="mt-1 text-sm">
                    {currentDepartmentQuery.data.depth}
                  </dd>
                </div>
                <div className="sm:col-span-2">
                  <dt className="text-xs font-medium text-muted-foreground">
                    Путь в структуре
                  </dt>
                  <dd className="mt-1 break-all font-mono text-sm">
                    {currentDepartmentQuery.data.path}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-muted-foreground">
                    Создан
                  </dt>
                  <dd className="mt-1 text-sm">
                    <time dateTime={currentDepartmentQuery.data.createdAt}>
                      {new Date(
                        currentDepartmentQuery.data.createdAt,
                      ).toLocaleDateString("ru-RU", { timeZone: "UTC" })}
                    </time>
                  </dd>
                </div>
                <div>
                  <dt className="text-xs font-medium text-muted-foreground">
                    Обновлён
                  </dt>
                  <dd className="mt-1 text-sm">
                    <time dateTime={currentDepartmentQuery.data.updatedAt}>
                      {new Date(
                        currentDepartmentQuery.data.updatedAt,
                      ).toLocaleDateString("ru-RU", { timeZone: "UTC" })}
                    </time>
                  </dd>
                </div>
                <div className="sm:col-span-2 border-t pt-3">
                  <dt className="text-xs font-medium text-muted-foreground">
                    ID подразделения
                  </dt>
                  <dd className="mt-1 break-all font-mono text-xs sm:text-sm">
                    {currentDepartmentQuery.data.id}
                  </dd>
                </div>
              </dl>
            </div>
          )}
        </section>

        <section className="min-w-0 rounded-2xl border bg-card p-5 shadow-sm sm:p-6">
          <div className="mb-5 flex items-start gap-3">
            <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-muted">
              <GitBranch
                aria-hidden="true"
                className="size-5 text-sky-700 dark:text-sky-400"
              />
            </div>
            <div>
              <h2 className="text-lg font-semibold">Новый родитель</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                В этом сценарии можно выбрать только одно подразделение.
              </p>
            </div>
          </div>

          <DepartmentSelect
            mode="single"
            label="Родительское подразделение"
            value={selectedParent}
            onChange={setSelectedParent}
            excludeSubtreeOf={departmentId}
            disabled={withoutParent}
          />

          <div className="mt-5 flex items-start gap-3 rounded-xl border bg-sky-50/70 p-4 dark:bg-sky-950/40">
            <Checkbox
              id="department-without-parent"
              onCheckedChange={handleWithoutParentChange}
              className="mt-0.5"
            />
            <label
              htmlFor="department-without-parent"
              className="cursor-pointer space-y-1"
            >
              <span className="block text-sm font-medium">Без родителя</span>
              <span className="block text-xs text-muted-foreground">
                Сделать подразделение корневым в структуре.
              </span>
            </label>
          </div>
        </section>
      </div>

      <div className="flex flex-col gap-3 rounded-2xl border bg-muted/25 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
        <p className="text-sm text-muted-foreground">
          Изменение родителя вступит в силу после сохранения.
        </p>
        <div className="flex flex-wrap gap-2">
          <Link
            href="/departments"
            className={buttonVariants({ variant: "outline", size: "lg" })}
          >
            Отмена
          </Link>
          <Button
            type="button"
            size="lg"
            onClick={() => void handleEdit()}
            disabled={
              editParent.isPending ||
              (!withoutParent && selectedParent.length === 0)
            }
          >
            <Save data-icon="inline-start" />
            {editParent.isPending ? "Сохраняем..." : "Сохранить"}
          </Button>
        </div>
      </div>
    </div>
  );
}
