import { routes } from "./routes";

export const navigationItems = [
  { label: "Локации", href: routes.locations },
  { label: "Подразделения", href: routes.departments },
  { label: "Позиции", href: routes.positions },
] as const;
