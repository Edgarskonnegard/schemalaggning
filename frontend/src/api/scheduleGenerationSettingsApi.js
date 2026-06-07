import { get, put } from "./client";

export function getScheduleGenerationSettings(storeId) {
  return get(`/api/stores/${storeId}/schedule-generation-settings`);
}

export function updateScheduleGenerationSettings(storeId, settings) {
  return put(`/api/stores/${storeId}/schedule-generation-settings`, settings);
}
