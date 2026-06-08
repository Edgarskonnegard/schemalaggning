import { del, get, put } from "./client";

export function getStoreCoverageRules(storeId) {
  return get(`/api/stores/${storeId}/coverage-rules`);
}

export function setStoreCoverageRule(storeId, rule) {
  return put(`/api/stores/${storeId}/coverage-rules`, rule);
}

export function deleteStoreCoverageRule(storeId, ruleId) {
  return del(`/api/stores/${storeId}/coverage-rules/${ruleId}`);
}
