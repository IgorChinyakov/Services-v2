import type { PagedResult } from "@/shared/api";

export type AddressDto = {
  city: string;
  street: string;
  house: string;
  apartment: string;
};

export type LocationDto = {
  id: string;
  name: string;
  address: AddressDto;
  isActive: boolean;
  createdAt: string;
  departmentCount: number;
};

export type LocationsPagedResultDto = PagedResult<LocationDto>;
