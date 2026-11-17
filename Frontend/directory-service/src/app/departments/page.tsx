import { DepartmentsList } from "@/widgets/departments-list";
import { FocusedDepartment } from "@/widgets/focused-department";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Departments | Directory Service",
};

export default async function Departments({
  searchParams,
}: {
  searchParams: Promise<{ focusId?: string | null }>;
}) {
  const { focusId } = await searchParams;
  const departmentId = Array.isArray(focusId) ? focusId[0] : focusId;

  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-7xl rounded-2xl border bg-muted/30 p-6 text-sm text-muted-foreground">
          Загрузка подразделений...
        </div>
      }
    >
      <DepartmentsList
        focusedDepartment={
          departmentId ? (
            <FocusedDepartment departmentId={departmentId}></FocusedDepartment>
          ) : null
        }
      />
    </Suspense>
  );
}
