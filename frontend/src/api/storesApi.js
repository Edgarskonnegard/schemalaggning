import { get, post, put } from "./client";

export function getStores() {
  return get("/api/stores");
}

export function createStore(store) {
  return post("/api/stores", store);
}

export function updateStore(id, store) {
  return put(`/api/stores/${id}`, store);
}

export function generateBaseSchedules(storeId) {
  return post(`/api/stores/${storeId}/base-schedules/generate`);
}
