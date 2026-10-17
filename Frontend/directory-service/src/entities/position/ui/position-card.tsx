import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import { BriefcaseBusiness, CalendarDays, FileText } from "lucide-react";
import type { PositionDto } from "../model/types";

export default function PositionCard({ dto }: { dto: PositionDto }) {
  const createdAt = new Intl.DateTimeFormat("ru-RU", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(dto.createdAt));

  return (
    <Card className="group relative min-h-64 overflow-hidden border-0 bg-linear-to-br from-card via-card to-amber-50/70 shadow-sm ring-1 ring-foreground/10 transition duration-300 hover:-translate-y-1 hover:shadow-lg dark:to-amber-950/20">
      <div className="absolute inset-x-0 top-0 h-1 bg-linear-to-r from-amber-500 via-orange-400 to-transparent" />

      <CardHeader className="grid-cols-[auto_1fr] items-center gap-3 pt-2">
        <div className="flex size-11 items-center justify-center rounded-xl bg-amber-100 text-amber-800 ring-1 ring-amber-200 dark:bg-amber-950 dark:text-amber-300 dark:ring-amber-800">
          <BriefcaseBusiness className="size-5" />
        </div>
        <div className="min-w-0">
          <p className="text-xs font-medium tracking-[0.16em] text-muted-foreground uppercase">
            Позиция
          </p>
          <CardTitle className="break-words text-lg">{dto.name}</CardTitle>
        </div>
      </CardHeader>

      <CardContent className="flex flex-1 flex-col">
        <div className="flex gap-3 rounded-xl bg-background/70 p-3 ring-1 ring-foreground/7">
          <FileText className="mt-0.5 size-4 shrink-0 text-amber-700 dark:text-amber-400" />
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Описание</p>
            <p className="mt-1 break-words leading-relaxed">
              {dto.description?.trim() || "Описание не указано"}
            </p>
          </div>
        </div>
      </CardContent>

      <CardFooter className="gap-2 border-t-0 bg-transparent pt-0 text-xs text-muted-foreground">
        <CalendarDays className="size-4" />
        Создана {createdAt}
      </CardFooter>
    </Card>
  );
}
