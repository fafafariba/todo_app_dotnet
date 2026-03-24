import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { TodoPage } from "./TodoPage";
import { AuthProvider } from "../context/AuthContext";

const mockTodo = {
  id: "1",
  title: "Test todo",
  description: "A description",
  priority: "MEDIUM",
  dueDate: null,
  completedAt: null,
  createdAt: "2026-03-24T00:00:00.000Z",
  updatedAt: "2026-03-24T00:00:00.000Z",
};

function renderTodoPage() {
  // Seed auth state in localStorage
  localStorage.setItem("auth_token", "fake-token");
  localStorage.setItem("auth_user", JSON.stringify({ id: "u1", email: "test@test.com", name: "Test" }));

  return render(
    <MemoryRouter>
      <AuthProvider>
        <TodoPage />
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("TodoPage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("renders greeting with user name", () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: [] }),
    } as Response);

    renderTodoPage();
    expect(screen.getByText(/Test!/)).toBeInTheDocument();
  });

  it("shows empty state when no todos", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: [] }),
    } as Response);

    renderTodoPage();
    expect(await screen.findByText(/No todos yet/)).toBeInTheDocument();
  });

  it("renders todos from API", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: [mockTodo] }),
    } as Response);

    renderTodoPage();
    expect(await screen.findByText("Test todo")).toBeInTheDocument();
    expect(screen.getByText("A description")).toBeInTheDocument();
    expect(screen.getByText("MEDIUM")).toBeInTheDocument();
  });

  it("creates a new todo", async () => {
    const newTodo = { ...mockTodo, id: "2", title: "New todo", description: null };

    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ data: [] }),
      } as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: () => Promise.resolve({ data: newTodo }),
      } as Response);

    renderTodoPage();
    const user = userEvent.setup();

    await screen.findByText(/No todos yet/);

    await user.type(screen.getByPlaceholderText("What needs to be done?"), "New todo");
    await user.click(screen.getByRole("button", { name: "Add" }));

    expect(await screen.findByText("New todo")).toBeInTheDocument();
  });

  it("toggles todo completion", async () => {
    const completedTodo = { ...mockTodo, completedAt: "2026-03-24T12:00:00.000Z" };

    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ data: [mockTodo] }),
      } as Response)
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ data: completedTodo }),
      } as Response);

    renderTodoPage();
    const user = userEvent.setup();

    await screen.findByText("Test todo");
    await user.click(screen.getByRole("button", { name: "Mark complete" }));

    expect(await screen.findByText("Completed")).toBeInTheDocument();
  });

  it("shows sign out button", () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: [] }),
    } as Response);

    renderTodoPage();
    expect(screen.getByRole("button", { name: "Sign out" })).toBeInTheDocument();
  });
});
