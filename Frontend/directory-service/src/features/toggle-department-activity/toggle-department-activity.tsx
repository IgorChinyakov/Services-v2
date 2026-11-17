import { Switch } from "@/shared/ui/switch";
import { useToggleDepartmentActivity } from "./use-toggle-department-activity";
import { useEffect, useId, useRef } from "react";

type ToggleDepartmentActivityProps = {
  departmentId: string;
  isActive: boolean;
};

export default function ToggleDepartmentActivity({
  departmentId,
  isActive,
}: ToggleDepartmentActivityProps) {
  const toggleDepartmentActivityMutation = useToggleDepartmentActivity();
  const switchId = useId();

  const controllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    controllerRef.current = controller;

    return () => {
      controller.abort();
      controllerRef.current = null;
    };
  }, []);

  const handleToggle = (nextIsActive: boolean) => {
    const controller = controllerRef.current;
    if (!controller) return;

    try {
      toggleDepartmentActivityMutation.mutate({
        departmentId: departmentId,
        signal: controller.signal,
        isActive: nextIsActive,
      });
    } catch {}
  };

  return (
    <div className="inline-flex min-h-8 items-center gap-2 rounded-lg border bg-muted/30 px-3 py-1.5">
      <Switch
        id={switchId}
        disabled={toggleDepartmentActivityMutation.isPending}
        checked={isActive}
        onCheckedChange={(nextIsActive) => handleToggle(nextIsActive)}
      />
      <label
        htmlFor={switchId}
        className={`select-none text-xs font-medium ${toggleDepartmentActivityMutation.isPending ? "cursor-not-allowed text-muted-foreground" : "cursor-pointer"}`}
      >
        <span className="sr-only">Активность подразделения: </span>
        {isActive ? "Активно" : "Неактивно"}
      </label>
    </div>
  );
}
