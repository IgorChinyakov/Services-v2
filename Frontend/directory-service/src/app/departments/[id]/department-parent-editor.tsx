"use client";

import {
  type DepartmentListItemDto,
  type DepartmentDetailsDto,
} from "@/entities/department";
import { UpdateDepartmentParentRequest } from "@/entities/department/model/types";
import DepartmentSelect from "@/features/department-select/department-select";
import { useEditDepartmentParent } from "@/features/edit-department-parent/use-edit-department-parent";
import { ApiRequestError } from "@/shared/api/error";
import GetResult from "@/shared/api/get-result";
import { cn } from "@/shared/lib/utils";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Checkbox } from "@/shared/ui/checkbox";
import { toast } from "@/shared/ui/toast";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  Building2,
  CircleAlert,
  GitBranch,
  Save,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";

function getParentErrorMessage(code: string): string {
  switch (code) {
    case "department.move.cycle":
      return "Нельзя перенести подразделение под его потомка. Выбери другого родителя.";
    case "department.move.parent_is_self":
      return "Подразделение не может быть своим родителем. Выбери другого родителя.";
    case "department.move.parent_deleted":
      return "Выбранное подразделение в архиве или неактивно. Выбери другого родителя.";
    case "department.parent.not_found":
      return "Выбранное подразделение не найдено. Выбери другого родителя.";
    default:
      return "Не удалось выбрать этого родителя. Попробуй выбрать другого.";
  }
}

export default function DepartmentParentEditor({
  departmentId,
  returnTo,
}: {
  departmentId: string;
  returnTo: string;
}) {
  const router = useRouter();

  // TODO: Initialize the current parent selection when its details are available.
  const [selectedParent, setSelectedParent] = useState<DepartmentListItemDto[]>(
    [],
  );
  const [withoutParent, setWithoutParent] = useState(false);

  const controllerRef = useRef<AbortController | null>(null);

  const [parentError, setParentError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

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

  const isRoot = parentId === null;
  const rootOptionDisabled =
    isRoot || currentDepartmentQuery.isPending || editParent.isPending;

  const currentParentName =
    parentId === null
      ? "Корень структуры"
      : (currentParentQuery.data?.name ??
        (currentDepartmentQuery.isError || currentParentQuery.isError
          ? "Не удалось загрузить"
          : "Загрузка..."));
  const destinationName = withoutParent
    ? "Корень структуры"
    : selectedParent[0]?.name;

  const handleEdit = async () => {
    const controller = controllerRef.current;
    if (!controller || editParent.isPending) return;

    setParentError(null);
    setSaveError(null);

    const request: UpdateDepartmentParentRequest = {
      parentId: withoutParent ? null : selectedParent[0]?.id,
    };
    try {
      await editParent.mutateAsync({
        departmentId,
        values: request,
        signal: controller.signal,
      });

      router.replace(returnTo);

      toast.add({
        type: "success",
        description: "Родитель успешно заменен",
      });
    } catch (error) {
      if (controller.signal.aborted) return;

      if (error instanceof ApiRequestError) {
        const fieldError = error.errors.find(
          (item) => item.invalidField === "parentId",
      );

        if (fieldError) {
          setParentError(getParentErrorMessage(fieldError.code));
          return;
        }
      }

      setSaveError("Не удалось сохранить перенос. Попробуй ещё раз.");
    }
  };

  function handleParentChange(departments: DepartmentListItemDto[]) {
    setParentError(null);
    setSelectedParent(departments);
  }

  function handleWithoutParentChange(checked: boolean) {
    setParentError(null);
    setWithoutParent(checked);
    if (checked) setSelectedParent([]);
  }

  return (
    <div className="mx-auto w-full max-w-5xl space-y-7 pb-12">
      <Link
        href={returnTo}
        replace
        className="inline-flex items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft aria-hidden="true" className="size-4" />
        Назад
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
            onChange={handleParentChange}
            excludeSubtreeOf={departmentId}
            excludeIds={parentId ? [parentId] : []}
            disabled={withoutParent || editParent.isPending}
          />
          {parentError && (
            <div
              role="alert"
              className="mt-2 flex items-start gap-2 rounded-lg border border-destructive/25 bg-destructive/5 px-3 py-2 text-sm text-destructive"
            >
              <CircleAlert
                aria-hidden="true"
                className="mt-0.5 size-4 shrink-0"
              />
              <p className="min-w-0 wrap-break-word">{parentError}</p>
            </div>
          )}
          <div
            className={cn(
              "mt-5 flex items-start gap-3 rounded-xl border p-4",
              rootOptionDisabled
                ? "bg-muted/40"
                : "border-sky-200/80 bg-sky-50/70 dark:border-sky-900 dark:bg-sky-950/40",
            )}
          >
            <Checkbox
              id="department-without-parent"
              aria-describedby="department-without-parent-description"
              onCheckedChange={handleWithoutParentChange}
              disabled={rootOptionDisabled}
              className="mt-0.5"
            />
            <div className="min-w-0 flex-1">
              <label
                htmlFor="department-without-parent"
                className={cn(
                  "block text-sm font-medium",
                  rootOptionDisabled
                    ? "cursor-not-allowed text-muted-foreground"
                    : "cursor-pointer",
                )}
              >
                Без родителя
              </label>
              <p
                id="department-without-parent-description"
                className="mt-1 text-xs leading-relaxed text-muted-foreground"
              >
                {isRoot
                  ? "Подразделение уже находится в корне структуры."
                  : "Сделать подразделение корневым в структуре."}
              </p>
            </div>
          </div>
        </section>
      </div>

      {destinationName && (
        <section
          aria-labelledby="department-move-preview"
          className="space-y-3 rounded-2xl border border-sky-200/80 bg-linear-to-br from-sky-50/80 via-card to-card p-4 shadow-sm sm:p-5 dark:border-sky-900 dark:from-sky-950/30"
        >
          <h2 id="department-move-preview" className="text-sm font-semibold">
            Предпросмотр переноса
          </h2>
          <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] sm:items-center sm:gap-3">
            <div className="min-w-0 rounded-xl border bg-background/80 px-4 py-3">
              <p className="text-xs text-muted-foreground">Текущий родитель</p>
              <p className="mt-1 wrap-break-word text-sm font-medium">
                {currentParentName}
              </p>
            </div>
            <div className="mx-auto flex size-8 items-center justify-center rounded-full bg-sky-100 text-sky-700 dark:bg-sky-950 dark:text-sky-300">
              <ArrowRight
                className="size-4 rotate-90 sm:rotate-0"
                aria-hidden="true"
              />
            </div>
            <div className="min-w-0 rounded-xl border border-sky-200 bg-sky-50 px-4 py-3 dark:border-sky-800 dark:bg-sky-950/50">
              <p className="text-xs text-muted-foreground">Новое место</p>
              <p className="mt-1 wrap-break-word text-sm font-semibold">
                {destinationName}
              </p>
            </div>
          </div>
        </section>
      )}

      <div className="grid gap-3 rounded-2xl border bg-muted/25 p-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:p-5">
        <p className="text-sm text-muted-foreground">
          Изменение родителя вступит в силу после сохранения.
        </p>
        <div className="flex flex-wrap gap-2">
          <Link
            href={returnTo}
            replace
            className={buttonVariants({ variant: "outline", size: "lg" })}
          >
            Отмена
          </Link>
          <Button
            type="button"
            size="lg"
            aria-describedby={
              saveError ? "department-move-save-error" : undefined
            }
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
        {saveError && (
          <div
            id="department-move-save-error"
            role="alert"
            className="flex items-start gap-2 rounded-lg border border-destructive/25 bg-destructive/5 px-3 py-2 text-sm text-destructive sm:col-span-2 sm:justify-self-end"
          >
            <CircleAlert
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0"
            />
            <p className="min-w-0 wrap-break-word">{saveError}</p>
          </div>
        )}
      </div>
    </div>
  );
}
