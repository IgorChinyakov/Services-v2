"use client";

import { Checkbox } from "@/shared/ui/checkbox";
import { useSelectedDepartmentsStore } from "./store/selected-departments-store";

export default function DepartmentSelection({
  departmentId,
  departmentName,
  disabled = false,
}: {
  departmentId: string;
  departmentName?: string;
  disabled?: boolean;
}) {
  const isSelected = useSelectedDepartmentsStore((state) =>
    state.selectedIds.includes(departmentId),
  );

  const toggleSelected = useSelectedDepartmentsStore(
    (state) => state.toggleSelected,
  );

  return (
    <div data-department-selected={isSelected} className="shrink-0 self-start">
      <Checkbox
        checked={isSelected}
        disabled={disabled}
        aria-label={`Выбрать подразделение «${departmentName ?? departmentId}»`}
        title={isSelected ? "Снять выбор подразделения" : "Выбрать подразделение"}
        onCheckedChange={() => toggleSelected(departmentId)}
        className="size-10 rounded-xl border-sky-200 bg-background shadow-xs after:hidden hover:border-sky-400 hover:bg-sky-50 focus-visible:border-sky-500 focus-visible:ring-sky-400/30 data-checked:border-sky-600 data-checked:bg-sky-600 data-checked:text-white hover:data-checked:bg-sky-700 dark:border-sky-800 dark:hover:bg-sky-950 dark:data-checked:border-sky-500 dark:data-checked:bg-sky-500 dark:hover:data-checked:bg-sky-600 [&_svg]:size-5"
      />
    </div>
  );
}
