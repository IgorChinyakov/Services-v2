import z from "zod";

export const createLocationSchema = z.object({
  name: z
    .string()
    .trim()
    .min(3, "location name must have at least 3 symbols")
    .max(120, "location name must be no longer than 120 symbols"),
  address: z.object({
    city: z
      .string()
      .trim()
      .min(2, "address city must have at least 2 symbols")
      .max(100, "address city must be no longer than 100 symbols"),
    street: z
      .string()
      .trim()
      .min(2, "address street must have at least 2 symbols")
      .max(100, "address street must be no longer than 100 symbols"),
    house: z
      .string()
      .trim()
      .min(2, "address house must have at least 2 symbols")
      .max(100, "address house must be no longer than 100 symbols"),
    apartment: z
      .string()
      .trim()
      .min(2, "address apartment must have at least 2 symbols")
      .max(100, "address apartment must be no longer than 100 symbols"),
  }),
});

export type CreateLocationRequest = z.infer<typeof createLocationSchema>;
