import { buttonVariants } from "@/shared/ui/button";
import Link from "next/link";

export function DepartmentMoveLink({ id, name }: { id: string; name: string }) {
  return (
    <Link
      href={`/departments/${id}?from=tree`}
      prefetch={false}
      aria-label={`Перенести подразделение: ${name}`}
      className={buttonVariants({
        variant: "ghost",
        size: "sm",
        className: "ml-1 text-muted-foreground",
      })}
    >
      Перенести
    </Link>
  );
}
