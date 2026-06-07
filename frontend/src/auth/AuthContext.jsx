import { createContext, useContext, useMemo, useState } from "react";

import { login as loginRequest } from "../api/authApi";

const AuthContext = createContext(null);

function readStoredAuth() {
  const token = localStorage.getItem("authToken");
  const userJson = localStorage.getItem("authUser");

  if (!token || !userJson) {
    return { token: "", user: null };
  }

  try {
    return {
      token,
      user: JSON.parse(userJson),
    };
  } catch {
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
    return { token: "", user: null };
  }
}

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(readStoredAuth);

  async function login(credentials) {
    const response = await loginRequest(credentials);
    localStorage.setItem("authToken", response.accessToken);
    localStorage.setItem("authUser", JSON.stringify(response.user));
    setAuth({
      token: response.accessToken,
      user: response.user,
    });
  }

  function logout() {
    localStorage.removeItem("authToken");
    localStorage.removeItem("authUser");
    setAuth({ token: "", user: null });
  }

  const value = useMemo(
    () => ({
      token: auth.token,
      user: auth.user,
      isAuthenticated: Boolean(auth.token && auth.user),
      isAdmin: auth.user?.accessRole === "Admin",
      login,
      logout,
    }),
    [auth]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}
