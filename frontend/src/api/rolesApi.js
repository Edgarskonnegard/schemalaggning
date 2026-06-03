import { get, post, put } from "./client";

export function getRoles() {
  return get("/api/roles");
}

export function createRole(role) {
  return post("/api/roles", role);
}

export function updateRole(id, role) {
  return put(`/api/roles/${id}`, role);
}
