import { get, post, put } from "./client";

export function getSchedules() {
  return get("/api/schedules");
}

export function getSchedule(id) {
  return get(`/api/schedules/${id}`);
}

export function generateStoreSchedule(storeId, schedule) {
  return post(`/api/stores/${storeId}/schedules/generate`, schedule);
}

export function publishSchedule(id) {
  return put(`/api/schedules/${id}/publish`);
}

export function createScheduleShift(scheduleId, shift) {
  return post(`/api/schedules/${scheduleId}/shifts`, shift);
}

export function updateShift(id, shift) {
  return put(`/api/shifts/${id}`, shift);
}

export function swapShiftEmployees(id, targetShiftId) {
  return put(`/api/shifts/${id}/swap`, { targetShiftId });
}
