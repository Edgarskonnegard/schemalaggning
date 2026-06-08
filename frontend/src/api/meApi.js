import { get } from "./client";

export function getMe() {
  return get("/api/me");
}
