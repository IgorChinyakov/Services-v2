"use client";

import {
  locationFormSchema,
  type LocationDto,
  type LocationFormValues,
} from "@/entities/location";
import { Button } from "@/shared/ui/button";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Input } from "@/shared/ui/input";
import { CircleAlert } from "lucide-react";
import useEditLocation from "./use-edit-location";
import { useEffect, useRef } from "react";
import { toast } from "@/shared/ui/toast";

export default function EditLocationForm({
  dto,
  onSuccess,
}: {
  dto: LocationDto;
  onSuccess: () => void;
}) {
  const form = useForm<LocationFormValues>({
    defaultValues: {
      name: dto.name,
      address: dto.address,
    },
    resolver: zodResolver(locationFormSchema),
    mode: "onBlur",
  });

  const editLocationMutation = useEditLocation(form.setError);
  const controllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    controllerRef.current = controller;

    return () => {
      controller.abort();
    };
  }, []);

  const handleSubmit = async (values: LocationFormValues) => {
    const controller = controllerRef.current;
    if (!controller) return;

    try {
      await editLocationMutation.mutateAsync({
        id: dto.id,
        values,
        signal: controller.signal,
      });
    } catch {
      return;
    }

    toast.add({
      type: "success",
      description: "Location edited successfully",
    });
    onSuccess();
  };

  return (
    <form
      className="space-y-8"
      noValidate
      onSubmit={(event) => {
        void form.handleSubmit(handleSubmit)(event);
      }}
    >
      <div className="space-y-2">
        <label htmlFor="edit-location-name" className="text-sm font-medium">
          Название
        </label>
        <Input
          id="edit-location-name"
          type="text"
          autoComplete="organization"
          aria-invalid={Boolean(form.formState.errors.name)}
          aria-describedby={
            form.formState.errors.name ? "edit-location-name-error" : undefined
          }
          className="h-10"
          {...form.register("name")}
        />
        {form.formState.errors.name && (
          <p id="edit-location-name-error" className="text-sm text-destructive">
            {form.formState.errors.name.message}
          </p>
        )}
      </div>

      <fieldset className="space-y-4 border-t pt-6">
        <legend className="mb-2 text-base font-semibold">Адрес</legend>
        <p className="text-sm text-muted-foreground">
          Измени нужные поля адреса. Остальные значения останутся прежними.
        </p>
        <div className="grid gap-5 sm:grid-cols-2">
          <div className="space-y-2">
            <label htmlFor="edit-location-city" className="text-sm font-medium">
              Город
            </label>
            <Input
              id="edit-location-city"
              type="text"
              autoComplete="address-level2"
              aria-invalid={Boolean(form.formState.errors.address?.city)}
              aria-describedby={
                form.formState.errors.address?.city
                  ? "edit-location-city-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.city")}
            />
            {form.formState.errors.address?.city && (
              <p
                id="edit-location-city-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.city.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label
              htmlFor="edit-location-street"
              className="text-sm font-medium"
            >
              Улица
            </label>
            <Input
              id="edit-location-street"
              type="text"
              autoComplete="address-line1"
              aria-invalid={Boolean(form.formState.errors.address?.street)}
              aria-describedby={
                form.formState.errors.address?.street
                  ? "edit-location-street-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.street")}
            />
            {form.formState.errors.address?.street && (
              <p
                id="edit-location-street-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.street.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label
              htmlFor="edit-location-house"
              className="text-sm font-medium"
            >
              Дом
            </label>
            <Input
              id="edit-location-house"
              type="text"
              aria-invalid={Boolean(form.formState.errors.address?.house)}
              aria-describedby={
                form.formState.errors.address?.house
                  ? "edit-location-house-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.house")}
            />
            {form.formState.errors.address?.house && (
              <p
                id="edit-location-house-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.house.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label
              htmlFor="edit-location-apartment"
              className="text-sm font-medium"
            >
              Помещение
            </label>
            <Input
              id="edit-location-apartment"
              type="text"
              autoComplete="address-line2"
              aria-invalid={Boolean(form.formState.errors.address?.apartment)}
              aria-describedby={
                form.formState.errors.address?.apartment
                  ? "edit-location-apartment-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.apartment")}
            />
            {form.formState.errors.address?.apartment && (
              <p
                id="edit-location-apartment-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.apartment.message}
              </p>
            )}
          </div>
        </div>
        {form.formState.errors.address?.message && (
          <div
            role="alert"
            className="flex items-start gap-3 rounded-xl border border-destructive/25 bg-destructive/5 px-4 py-3 text-sm text-destructive"
          >
            <CircleAlert
              className="mt-0.5 size-4 shrink-0"
              aria-hidden="true"
            />
            <p>{form.formState.errors.address.message}</p>
          </div>
        )}
      </fieldset>

      <div className="flex justify-end border-t pt-6">
        <Button
          type="submit"
          size="lg"
          className="min-w-40"
          disabled={editLocationMutation.isPending}
        >
          Сохранить изменения
        </Button>
      </div>
    </form>
  );
}
