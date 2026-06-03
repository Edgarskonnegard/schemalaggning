import { del, get, put } from "./client";

export function getEmployeeBaseSchedule(employeeId) {
  return get(`/api/employees/${employeeId}/base-schedule`);
}

export function setEmployeeBaseScheduleRule(employeeId, rule) {
  return put(`/api/employees/${employeeId}/base-schedule`, rule);
}

export function deleteEmployeeBaseScheduleRule(employeeId, weekInCycle, dayOfWeek) {
  return del(
    `/api/employees/${employeeId}/base-schedule/${weekInCycle}/${dayOfWeek}`
  );
}
