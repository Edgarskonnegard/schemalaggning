import { get, post } from "./client";

export function login(credentials) {
  return post("/api/auth/login", credentials);
}

export function getAccounts() {
  return get("/api/auth/accounts");
}

export function createAccount(account) {
  return post("/api/auth/accounts", account);
}
