"use client";

import { Button } from "@/shared/ui/button";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
} from "./pagination";

export default function PaginationControls({
  page,
  totalPages,
  hasNextPage,
  setPage,
}: {
  page: number;
  totalPages: number;
  hasNextPage: boolean;
  setPage: (page: number) => void;
}) {
  const middleStart = Math.max(2, Math.min(page - 2, totalPages - 5));
  const middleEnd = Math.min(totalPages - 1, middleStart + 4);
  const middlePages = Array.from(
    { length: Math.max(0, middleEnd - middleStart + 1) },
    (_, index) => middleStart + index,
  );

  const pageButton = (pageNumber: number) => (
    <PaginationItem key={pageNumber}>
      <Button
        type="button"
        className="min-w-8 px-2"
        variant={pageNumber === page ? "default" : "ghost"}
        aria-current={pageNumber === page ? "page" : undefined}
        aria-label={`Страница ${pageNumber}`}
        onClick={() => setPage(pageNumber)}
      >
        {pageNumber}
      </Button>
    </PaginationItem>
  );

  return (
    <Pagination className="justify-start overflow-x-auto sm:justify-center">
      <PaginationContent className="shrink-0">
        <PaginationItem>
          <Button
            type="button"
            onClick={() => setPage(page - 1)}
            disabled={page === 1}
          >
            Назад
          </Button>
        </PaginationItem>
        {pageButton(1)}
        {middleStart > 2 && (
          <PaginationItem>
            <PaginationEllipsis />
          </PaginationItem>
        )}
        {middlePages.map(pageButton)}
        {middleEnd < totalPages - 1 && (
          <PaginationItem>
            <PaginationEllipsis />
          </PaginationItem>
        )}
        {totalPages > 1 && pageButton(totalPages)}
        <PaginationItem>
          <Button
            type="button"
            onClick={() => setPage(page + 1)}
            disabled={!hasNextPage}
          >
            Вперед
          </Button>
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}
