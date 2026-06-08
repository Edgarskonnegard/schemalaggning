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

export function getPendingLeaveRequestCount() {
  return get("/api/admin/leave-requests/pending-count");
}

export function getPendingLeaveRequests() {
  return get("/api/admin/leave-requests");
}

export function getScheduleLeaveBlocks(storeId, periodStart, periodEnd) {
  return get(
    `/api/admin/leave-requests/schedule-blocks?storeId=${storeId}&periodStart=${periodStart}&periodEnd=${periodEnd}`
  );
}

export function approveLeaveRequest(id, adminComment = "") {
  return post(`/api/admin/leave-requests/${id}/approve`, { adminComment });
}

export function rejectLeaveRequest(id, adminComment = "") {
  return post(`/api/admin/leave-requests/${id}/reject`, { adminComment });
}
