import * as z from "zod";

const locationSchema = z.object({
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

const validLocation = {
  name: "Main Office",
  address: {
    city: "Yerevan",
    street: "Aram Street",
    house: "12",
    apartment: "34",
  },
};

const testCases = [
  { label: "valid location", value: validLocation },
  { label: "blank name", value: { ...validLocation, name: "   " } },
  {
    label: "name too long",
    value: { ...validLocation, name: "A".repeat(121) },
  },
  {
    label: "city too short",
    value: {
      ...validLocation,
      address: { ...validLocation.address, city: "A" },
    },
  },
  {
    label: "street too long",
    value: {
      ...validLocation,
      address: { ...validLocation.address, street: "S".repeat(101) },
    },
  },
  {
    label: "house too long",
    value: {
      ...validLocation,
      address: { ...validLocation.address, house: "H".repeat(101) },
    },
  },
  {
    label: "apartment too long",
    value: {
      ...validLocation,
      address: { ...validLocation.address, apartment: "A".repeat(101) },
    },
  },
  { label: "missing address", value: { name: "Main Office" } },
];

for (const { label, value } of testCases) {
  const result = locationSchema.safeParse(value);
  console.log(label, result.success ? result.data : result.error.issues);
}
