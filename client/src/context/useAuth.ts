import { useContext } from "react";
import { AuthContext } from "./authContextValue";

/**
 * react-refresh/only-export-components rule enforces that files only export React 
 * components — no hooks, constants, or contexts alongside them. This is required 
 * for Vite's Fast Refresh (hot module replacement) to work reliably.
 */
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
