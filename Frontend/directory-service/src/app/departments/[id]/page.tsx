import type { Metadata } from "next";
import DepartmentParentEditor from "./department-parent-editor";

export const metadata: Metadata = {
  title: "Редактирование подразделения | Directory Service",
};

export default async function DepartmentEditPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  return <DepartmentParentEditor departmentId={id} />;
}
