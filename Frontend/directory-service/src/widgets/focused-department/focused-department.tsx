"use client";

import {
  DepartmentCard,
  fetchDepartmentById,
  type DepartmentDetailsDto,
} from "@/entities/department";
import { buttonVariants } from "@/shared/ui/button";
import { FocusedResultPanel } from "@/shared/ui/focused-result-panel";
import { useQuery } from "@tanstack/react-query";
import { Pencil } from "lucide-react";
import Link from "next/link";

export default function FocusedDepartment({
  departmentId,
}: {
  departmentId: string;
}) {
  const departmentQuery = useQuery({
    queryKey: ["department", departmentId],
    queryFn: ({ signal }) => fetchDepartmentById(departmentId, signal),
  });

  function toDto(dto: DepartmentDetailsDto) {
    return {
      id: dto.id,
      name: dto.name,
      slug: dto.slug,
      path: dto.path,
      isActive: dto.isActive,
      createdAt: dto.createdAt,
      updatedAt: dto.updatedAt,
      deletedAt: null,
    };
  }

  return (
    <FocusedResultPanel
      tone="sky"
      isPending={departmentQuery.isPending}
      isFetching={departmentQuery.isFetching}
      error={departmentQuery.error}
      hasData={departmentQuery.data !== undefined}
      onRetry={() => void departmentQuery.refetch()}
    >
      {departmentQuery.data && (
        <DepartmentCard
          dto={toDto(departmentQuery.data)}
          actions={
            <div className="flex flex-wrap items-center gap-1">
              <Link
                href={`/departments/${departmentId}?from=search`}
                className={buttonVariants({
                  variant: "outline",
                  size: "sm",
                })}
              >
                <Pencil data-icon="inline-start" />
                Редактировать
              </Link>
            </div>
          }
        />
      )}
    </FocusedResultPanel>
  );
}
