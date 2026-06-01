import { get, post } from "./client";

export function getShiftTypes() {
  return get("/api/shift-types");
}

export function createShiftType(shiftType) {
  return post("/api/shift-types", shiftType);
}
