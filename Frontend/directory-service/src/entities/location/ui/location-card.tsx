import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import {
  Building2,
  CalendarDays,
  Clock3,
  MapPin,
  UsersRound,
} from "lucide-react";
import type { LocationDto } from "../model/types";

export default function LocationCard({ dto }: { dto: LocationDto }) {
  const address = [
    dto.address.country,
    dto.address.city,
    dto.address.street,
    dto.address.building,
  ]
    .filter(Boolean)
    .join(", ");

  const createdAt = dto.createdAt
    ? new Intl.DateTimeFormat("ru-RU", {
        day: "numeric",
        month: "long",
        year: "numeric",
        timeZone: "UTC",
      }).format(new Date(dto.createdAt))
    : "Дата не указана";

  return (
    <Card className="group relative min-h-72 overflow-hidden border-0 bg-linear-to-br from-card via-card to-emerald-50/70 shadow-sm ring-1 ring-foreground/10 transition duration-300 hover:-translate-y-1 hover:shadow-lg dark:to-emerald-950/20">
      <div className="absolute inset-x-0 top-0 h-1 bg-linear-to-r from-emerald-500 via-teal-400 to-transparent" />

      <CardHeader className="grid-cols-[auto_1fr_auto] items-center gap-3 pt-2">
        <div className="flex size-11 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700 ring-1 ring-emerald-200 dark:bg-emerald-950 dark:text-emerald-300 dark:ring-emerald-800">
          <Building2 className="size-5" />
        </div>

        <div className="min-w-0">
          <p className="text-xs font-medium tracking-[0.16em] text-muted-foreground uppercase">
            Локация
          </p>
          <CardTitle className="truncate text-lg">{dto.name}</CardTitle>
        </div>

        <span
          className={
            dto.isActive
              ? "rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300"
              : "rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground"
          }
        >
          {dto.isActive ? "Активна" : "Неактивна"}
        </span>
      </CardHeader>

      <CardContent className="flex flex-1 flex-col gap-5">
        <div className="flex gap-3 rounded-xl bg-background/70 p-3 ring-1 ring-foreground/7">
          <MapPin className="mt-0.5 size-4 shrink-0 text-emerald-600" />
          <div className="min-w-0">
            <p className="text-xs text-muted-foreground">Адрес</p>
            <p className="mt-1 leading-relaxed">
              {address || "Адрес не указан"}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-xl bg-background/60 p-3 ring-1 ring-foreground/7">
            <Clock3 className="mb-2 size-4 text-teal-600" />
            <p className="text-xs text-muted-foreground">Часовой пояс</p>
            <p className="mt-1 truncate font-medium">
              {dto.timezone || "Не указан"}
            </p>
          </div>

          <div className="rounded-xl bg-background/60 p-3 ring-1 ring-foreground/7">
            <UsersRound className="mb-2 size-4 text-teal-600" />
            <p className="text-xs text-muted-foreground">Подразделений</p>
            <p className="mt-1 text-base font-semibold">
              {dto.departmentCount ?? 0}
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
