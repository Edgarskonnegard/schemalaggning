import { get, post } from "./client";

export function createShiftComment(shiftId, message) {
  return post(`/api/me/shifts/${shiftId}/comments`, { message });
}

export function getPendingShiftCommentCount() {
  return get("/api/admin/shift-comments/pending-count");
}

export function getPendingShiftComments() {
  return get("/api/admin/shift-comments");
}

export function resolveShiftComment(id) {
  return post(`/api/admin/shift-comments/${id}/resolve`);
}
