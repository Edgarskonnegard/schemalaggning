import { get, post, put, del } from "./client";

export function getEmployees() {
  return get("/api/employees");
}

export function getEmployeeDetails(id) {
  return get(`/api/employees/${id}/details`);
}

export function createEmployee(employee) {
  return post("/api/employees", employee);
}

export function updateEmployee(id, employee) {
  return put(`/api/employees/${id}`, employee);
}

export function deleteEmployee(id) {
  return del(`/api/employees/${id}`);
}
