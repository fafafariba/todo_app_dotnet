import type { Todo } from "../types";

const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:3000";

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

type AuthResponse = {
  data: {
    token: string;
    user: { id: string; email: string; name: string | null };
  };
};

type TodoListResponse = { data: Todo[] };
type TodoResponse = { data: Todo };

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, options);

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new ApiError(res.status, body.error ?? "Request failed");
  }

  return res.json();
}

function authHeaders(token: string): HeadersInit {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  };
}

// Auth

export function login(email: string, password: string): Promise<AuthResponse> {
  return request("/api/v1/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
}

export function register(email: string, password: string, name?: string): Promise<AuthResponse> {
  return request("/api/v1/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, name: name || undefined }),
  });
}

// Todos

export function fetchTodos(token: string): Promise<TodoListResponse> {
  return request("/api/v1/todos", {
    headers: authHeaders(token),
  });
}

export function createTodo(
  token: string,
  data: { title: string; description?: string; priority?: string; dueDate?: string },
): Promise<TodoResponse> {
  return request("/api/v1/todos", {
    method: "POST",
    headers: authHeaders(token),
    body: JSON.stringify(data),
  });
}

export function updateTodo(
  token: string,
  id: string,
  data: Partial<Pick<Todo, "title" | "description" | "priority" | "dueDate" | "completedAt">>,
): Promise<TodoResponse> {
  return request(`/api/v1/todos/${id}`, {
    method: "PATCH",
    headers: authHeaders(token),
    body: JSON.stringify(data),
  });
}

export function deleteTodo(token: string, id: string): Promise<{ data: { id: string } }> {
  return request(`/api/v1/todos/${id}`, {
    method: "DELETE",
    headers: authHeaders(token),
  });
}
