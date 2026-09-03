import { LocationsList } from "@/widgets/locations-list";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Locations | Directory Service",
};

export default function Locations() {
  return (
    <>
      <LocationsList />
    </>
  );
}
