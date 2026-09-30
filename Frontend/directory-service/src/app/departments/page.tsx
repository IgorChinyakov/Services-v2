import { DepartmentsList } from "@/widgets/departments-list";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Departments | Directory Service",
};

export default function Departments() {
  return <DepartmentsList />;
}
