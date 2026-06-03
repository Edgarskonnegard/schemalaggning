import { get, post, put } from "./client";

export function getShiftTypes() {
  return get("/api/shift-types");
}

export function createShiftType(shiftType) {
  return post("/api/shift-types", shiftType);
}

export function updateShiftType(id, shiftType) {
  return put(`/api/shift-types/${id}`, shiftType);
}
