import { PositionsList } from "@/widgets/positions-list";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Positions | Directory Service",
};

export default function Positions() {
  return <PositionsList />;
}
