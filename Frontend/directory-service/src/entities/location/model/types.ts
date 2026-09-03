import type { PagedResult } from "@/shared/api";

export type AddressDto = {
  country: string;
  city: string;
  street: string;
  building: string;
};

export type LocationDto = {
  id: string;
  name: string;
  address: AddressDto;
  timezone: string;
  isActive: boolean;
  createdAt: string;
  departmentCount: number;
};

export type LocationsPagedResultDto = PagedResult<LocationDto>;
