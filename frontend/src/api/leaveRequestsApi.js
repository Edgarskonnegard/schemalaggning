import { get, post } from "./client";

export function getMyLeaveRequests() {
  return get("/api/me/leave-requests");
}

export function getMyLeaveBalance(year) {
  const query = year ? `?year=${year}` : "";
  return get(`/api/me/leave-requests/balance${query}`);
}

export function createMyLeaveRequest(request) {
  return post("/api/me/leave-requests", request);
}
