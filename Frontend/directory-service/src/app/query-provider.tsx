"use client";

import { ApiRequestError } from "@/shared/api/error";
import {
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import axios from "axios";
import { useState, type ReactNode } from "react";

export function QueryProvider({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 1000 * 60 * 5,
            refetchOnWindowFocus: false,
            retry: (failureCount, error) => {
              if (failureCount >= 2) {
                return false;
              }

              if (error instanceof ApiRequestError) {
                return error.status >= 500;
              }

              return (
                axios.isAxiosError(error) &&
                !axios.isCancel(error) &&
                !error.response
              );
            },
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}
