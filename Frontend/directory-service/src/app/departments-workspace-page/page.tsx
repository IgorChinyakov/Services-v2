import DepartmentsWorkspace from "@/widgets/departments-workspace/departments-workspace";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Дерево отделов и должностей | Directory Service",
};

export default function DepartmentsWorkspacePage() {
  return <DepartmentsWorkspace />;
}
