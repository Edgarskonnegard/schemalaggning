import { get, post } from "./client";

export function getMyShiftSwaps() {
  return get("/api/me/shift-swaps");
}

export function getShiftSwapCandidates(shiftId) {
  return get(`/api/me/shifts/${shiftId}/swap-candidates`);
}

export function createShiftSwapRequest(shiftId, request) {
  return post(`/api/me/shifts/${shiftId}/swap-requests`, request);
}

export function approveShiftSwap(id) {
  return post(`/api/me/shift-swaps/${id}/approve`);
}

export function rejectShiftSwap(id) {
  return post(`/api/me/shift-swaps/${id}/reject`);
}
