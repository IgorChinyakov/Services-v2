"use client";

import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  locationFormSchema,
  type LocationFormValues,
} from "@/entities/location";
import { toast } from "@/shared/ui/toast";
import { useEffect, useRef } from "react";
import { useCreateLocation } from "./use-create-location";
import { CircleAlert } from "lucide-react";

export default function CreateLocationForm({
  onSuccess,
}: {
  onSuccess: () => void;
}) {
  const form = useForm<LocationFormValues>({
    defaultValues: {
      name: "",
      address: {
        city: "",
        street: "",
        house: "",
        apartment: "",
      },
    },
    resolver: zodResolver(locationFormSchema),
  });

  const createLocationMutation = useCreateLocation(form.setError);

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
      await createLocationMutation.mutateAsync({
        values,
        signal: controller.signal,
      });
    } catch {
      return;
    }

    toast.add({
      type: "success",
      description: "Location created successfully",
    });
    onSuccess();
  };

  return (
    <form
      className="space-y-8"
      onSubmit={(event) => {
        void form.handleSubmit(handleSubmit)(event);
      }}
      noValidate
    >
      <div className="space-y-2">
        <label htmlFor="location-name" className="text-sm font-medium">
          Название
        </label>
        <Input
          id="location-name"
          type="text"
          placeholder="Например, Главный офис"
          autoComplete="organization"
          aria-invalid={Boolean(form.formState.errors.name)}
          aria-describedby={
            form.formState.errors.name ? "location-name-error" : undefined
          }
          className="h-10"
          {...form.register("name")}
        />
        {form.formState.errors.name && (
          <p id="location-name-error" className="text-sm text-destructive">
            {form.formState.errors.name.message}
          </p>
        )}
      </div>

      <fieldset
        className="space-y-4 border-t pt-6"
        aria-describedby={
          form.formState.errors.address?.message
            ? "location-address-error"
            : undefined
        }
        onChange={() => {
          if (form.formState.isSubmitted) {
            void form.trigger("address");
          }
        }}
      >
        <legend className="mb-2 text-base font-semibold">Адрес</legend>
        <p className="text-sm text-muted-foreground">
          Заполни адрес так, как он должен отображаться в справочнике.
        </p>
        <div className="grid gap-5 sm:grid-cols-2">
          <div className="space-y-2">
            <label htmlFor="location-city" className="text-sm font-medium">
              Город
            </label>
            <Input
              id="location-city"
              type="text"
              placeholder="Например, Ереван"
              autoComplete="address-level2"
              aria-invalid={Boolean(form.formState.errors.address?.city)}
              aria-describedby={
                form.formState.errors.address?.city
                  ? "location-city-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.city")}
            />
            {form.formState.errors.address?.city && (
              <p id="location-city-error" className="text-sm text-destructive">
                {form.formState.errors.address.city.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="location-street" className="text-sm font-medium">
              Улица
            </label>
            <Input
              id="location-street"
              type="text"
              placeholder="Например, Арама"
              autoComplete="address-line1"
              aria-invalid={Boolean(form.formState.errors.address?.street)}
              aria-describedby={
                form.formState.errors.address?.street
                  ? "location-street-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.street")}
            />
            {form.formState.errors.address?.street && (
              <p
                id="location-street-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.street.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="location-house" className="text-sm font-medium">
              Дом
            </label>
            <Input
              id="location-house"
              type="text"
              placeholder="Например, 12"
              aria-invalid={Boolean(form.formState.errors.address?.house)}
              aria-describedby={
                form.formState.errors.address?.house
                  ? "location-house-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.house")}
            />
            {form.formState.errors.address?.house && (
              <p id="location-house-error" className="text-sm text-destructive">
                {form.formState.errors.address.house.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="location-apartment" className="text-sm font-medium">
              Помещение
            </label>
            <Input
              id="location-apartment"
              type="text"
              placeholder="Например, 34"
              autoComplete="address-line2"
              aria-invalid={Boolean(form.formState.errors.address?.apartment)}
              aria-describedby={
                form.formState.errors.address?.apartment
                  ? "location-apartment-error"
                  : undefined
              }
              className="h-10"
              {...form.register("address.apartment")}
            />
            {form.formState.errors.address?.apartment && (
              <p
                id="location-apartment-error"
                className="text-sm text-destructive"
              >
                {form.formState.errors.address.apartment.message}
              </p>
            )}
          </div>
        </div>
        {form.formState.errors.address?.message && (
          <div
            id="location-address-error"
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
          disabled={form.formState.isSubmitting}
        >
          Создать локацию
        </Button>
      </div>
    </form>
  );
}
