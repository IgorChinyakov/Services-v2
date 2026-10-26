"use client";

import type { DepartmentListItemDto } from "@/entities/department";
import DepartmentSelect from "@/features/department-select/department-select";
import { useState } from "react";

export default function DepartmentSelectDemo() {
  const [parent, setParent] = useState<DepartmentListItemDto[]>([]);
  const [members, setMembers] = useState<DepartmentListItemDto[]>([]);

  return (
    <main className="mx-auto w-full max-w-6xl space-y-8 pb-24">
      <header className="border-b pb-6">
        <p className="mb-2 text-sm font-medium text-sky-700 dark:text-sky-400">Компонент выбора</p>
        <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">DepartmentSelect</h1>
        <p className="mt-3 max-w-2xl text-sm leading-relaxed text-muted-foreground">
          Два независимых сценария: один родительский отдел или несколько подразделений.
        </p>
      </header>
      <div className="grid gap-6 lg:grid-cols-2">
        <section className="min-w-0 rounded-2xl border bg-linear-to-br from-sky-50/60 via-background to-background p-5 shadow-sm dark:from-sky-950/20 sm:p-6">
          <span className="text-xs font-semibold tracking-widest text-sky-700 uppercase dark:text-sky-400">Single</span>
          <h2 className="mt-2 text-xl font-semibold">Выбор родителя</h2>
          <p className="mt-2 mb-6 text-sm text-muted-foreground">Можно выбрать только одно подразделение.</p>
          <DepartmentSelect mode="single" label="Новый родитель" value={parent} onChange={setParent} />
        </section>
        <section className="min-w-0 rounded-2xl border bg-linear-to-br from-emerald-50/50 via-background to-background p-5 shadow-sm dark:from-emerald-950/20 sm:p-6">
          <span className="text-xs font-semibold tracking-widest text-emerald-700 uppercase dark:text-emerald-400">Multiple</span>
          <h2 className="mt-2 text-xl font-semibold">Несколько отделов</h2>
          <p className="mt-2 mb-6 text-sm text-muted-foreground">Каждый выбранный отдел можно убрать отдельно.</p>
          <DepartmentSelect mode="multiple" label="Выбранные подразделения" value={members} onChange={setMembers} />
        </section>
      </div>
    </main>
  );
}
