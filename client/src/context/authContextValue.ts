import { createContext } from "react";

export type AuthUser = { id: string; email: string; name: string | null };

export type AuthContextValue = {
  user: AuthUser | null;
  token: string | null;
  signIn: (token: string, user: AuthUser) => void;
  signOut: () => void;
};

export const AuthContext = createContext<AuthContextValue | null>(null);
