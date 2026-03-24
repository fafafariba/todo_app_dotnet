import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import { LoginPage } from "./LoginPage";
import { AuthProvider } from "../context/AuthContext";

function renderLoginPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("LoginPage", () => {
  it("renders sign in form by default", () => {
    renderLoginPage();
    expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeInTheDocument();
  });

  it("toggles to register form", async () => {
    renderLoginPage();
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Register" }));

    expect(screen.getByRole("heading", { name: "Create account" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /register/i })).toBeInTheDocument();
  });

  it("toggles back to sign in", async () => {
    renderLoginPage();
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Register" }));
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(screen.getByRole("heading", { name: "Sign in" })).toBeInTheDocument();
  });

  it("shows error on failed login", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({ error: "Invalid email or password" }),
    } as Response);

    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), "test@test.com");
    await user.type(screen.getByLabelText(/password/i), "wrongpassword");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Invalid email or password")).toBeInTheDocument();

    vi.restoreAllMocks();
  });
});
