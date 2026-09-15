import { Temporal } from "@js-temporal/polyfill";

export interface Student {
  readonly id: string;
  name: string;
  enrollmentDate: Temporal.Instant;
  gpa?: number;
}

export function isStudent(value: unknown): value is Student {
  if (typeof value !== "object" || value === null || !("id" in value) || !("name" in value)) {
    return false;
  }
  const obj = value as Record<string, unknown>;
  return typeof obj['id'] === "string" && typeof obj['name'] === "string";
}

export function parseStudent(raw: unknown): Student {
  if (typeof raw !== "object" || raw === null) {
    throw new TypeError(`Expected an object, received ${raw === null ? "null" : typeof raw}`);
  }
  const obj = raw as Record<string, unknown>;
  const id = obj['id'];
  const name = obj['name'];
  const gpa = obj['gpa'];

  if (typeof id !== "string") {
    throw new TypeError(`Expected id to be a string, received ${typeof id}`);
  }
  if (typeof name !== "string") {
    throw new TypeError(`Expected name to be a string, received ${typeof name}`);
  }

  return {
    id,
    name,
    enrollmentDate: Temporal.Now.instant(),
    gpa: typeof gpa === "number" ? gpa : undefined
  };
}
