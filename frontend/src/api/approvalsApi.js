import { del, get, post } from "./client";

export function getPendingBaseScheduleApprovalCount() {
  return get("/api/approvals/base-schedules/pending-count");
}

export function getBaseScheduleApprovals() {
  return get("/api/approvals/base-schedules");
}

export function approveBaseScheduleApproval(id) {
  return post(`/api/approvals/base-schedules/${id}/approve`);
}

export function rejectBaseScheduleApproval(id) {
  return post(`/api/approvals/base-schedules/${id}/reject`);
}

export function approveBaseScheduleEmployee(batchId, employeeId) {
  return post(`/api/approvals/base-schedules/${batchId}/employees/${employeeId}/approve`);
}

export function rejectBaseScheduleEmployee(batchId, employeeId) {
  return post(`/api/approvals/base-schedules/${batchId}/employees/${employeeId}/reject`);
}

export function assignBaseScheduleDraftRule(batchId, employeeId, unassignedRuleId) {
  return post(`/api/approvals/base-schedules/${batchId}/employees/${employeeId}/rules`, {
    unassignedRuleId,
  });
}

export function deleteBaseScheduleDraftRule(batchId, employeeId, ruleId) {
  return del(`/api/approvals/base-schedules/${batchId}/employees/${employeeId}/rules/${ruleId}`);
}
