import type { Metadata } from "next";
import DepartmentParentEditor from "./department-parent-editor";

export const metadata: Metadata = {
  title: "Редактирование подразделения | Directory Service",
};

export default async function DepartmentEditPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ from?: string | string[] }>;
}) {
  const { id } = await params;
  const { from } = await searchParams;
  const returnTo =
    from === "tree"
      ? "/departments-workspace-page"
      : from === "search"
        ? `/departments?focusId=${id}`
        : "/departments";

  return <DepartmentParentEditor departmentId={id} returnTo={returnTo} />;
}
