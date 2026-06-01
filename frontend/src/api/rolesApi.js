import { get, post } from "./client";

export function getRoles() {
  return get("/api/roles");
}

export function createRole(role) {
  return post("/api/roles", role);
}
