"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";

export function useDepartmentArchiveView() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  function changeArchiveView(isArchived: boolean) {
    const params = new URLSearchParams(searchParams.toString());
    if (isArchived) params.set("isArchived", "true");
    else params.delete("isArchived");

    const query = params.toString();
    router.replace(`${pathname}${query ? `?${query}` : ""}`, { scroll: false });
  }

  return {
    isArchived: searchParams.get("isArchived") === "true",
    changeArchiveView,
  };
}
