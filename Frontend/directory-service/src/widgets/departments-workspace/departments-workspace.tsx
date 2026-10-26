"use client";

import DepartmentPositions from "@/features/department-positions/department-positions";
import DepartmentTree from "@/features/department-tree/department-tree";
import { useDepartmentTreeStore } from "@/features/department-tree/store/department-tree-store";

export default function DepartmentsWorkspace() {
  const selected = useDepartmentTreeStore((state) => state.selected);

  return (
    <section className="mx-auto w-full max-w-7xl space-y-6">
      <header className="space-y-2 border-b pb-5">
        <p className="text-sm font-medium text-sky-700 dark:text-sky-400">
          Структура компании
        </p>
        <h1 className="text-3xl font-semibold tracking-tight">
          Подразделения и позиции
        </h1>
        <p className="text-sm text-muted-foreground">
          Раскрой структуру и выбери отдел, чтобы посмотреть его позиции.
        </p>
      </header>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(18rem,0.9fr)_minmax(0,1.1fr)]">
        <DepartmentTree />
        <DepartmentPositions departmentId={selected.id} name={selected.name} />
      </div>
    </section>
  );
}
