import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import { Building2, CalendarDays, GitBranch } from "lucide-react";
import type { ReactNode } from "react";
import type { DepartmentListItemDto } from "../model/types";

export default function DepartmentCard({
  dto,
  actions,
}: {
  dto: DepartmentListItemDto;
  actions?: ReactNode;
}) {
  const createdAt = new Intl.DateTimeFormat("ru-RU", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(dto.createdAt));

  return (
    <Card className="group relative min-h-64 overflow-hidden border-0 bg-linear-to-br from-card via-card to-sky-50/70 shadow-sm ring-1 ring-foreground/10 transition duration-300 hover:-translate-y-1 hover:shadow-lg dark:to-sky-950/20">
      <div className="absolute inset-x-0 top-0 h-1 bg-linear-to-r from-sky-500 via-cyan-400 to-transparent" />

      <CardHeader className="grid-cols-[auto_1fr] items-center gap-3 pt-2">
        <div className="flex size-11 items-center justify-center rounded-xl bg-sky-100 text-sky-700 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
          <Building2 className="size-5" />
        </div>

        <div className="min-w-0">
          <p className="text-xs font-medium tracking-[0.16em] text-muted-foreground uppercase">
            Подразделение
          </p>
          <CardTitle className="truncate text-lg">{dto.name}</CardTitle>
        </div>
      </CardHeader>

      <CardContent className="flex flex-1 flex-col gap-4">
        <div className="w-fit max-w-full truncate rounded-full bg-sky-50 px-3 py-1 text-xs font-medium text-sky-800 ring-1 ring-sky-200 dark:bg-sky-950 dark:text-sky-300 dark:ring-sky-800">
          {dto.slug}
        </div>

        <div className="flex gap-3 rounded-xl bg-background/70 p-3 ring-1 ring-foreground/7">
          <GitBranch className="mt-0.5 size-4 shrink-0 text-sky-600" />
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Путь в структуре</p>
            <p className="mt-1 break-all leading-relaxed">{dto.path}</p>
          </div>
        </div>
      </CardContent>

      <CardFooter className="flex flex-wrap items-center justify-between gap-3 border-t-0 bg-transparent pt-0 text-xs text-muted-foreground">
        <span className="flex items-center gap-2">
          <CalendarDays className="size-4" />
          Создано {createdAt}
        </span>
        {actions}
      </CardFooter>
    </Card>
  );
}
