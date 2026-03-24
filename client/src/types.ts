export const Priority = {
  LOW: "LOW",
  MEDIUM: "MEDIUM",
  HIGH: "HIGH",
} as const;

export type Priority = typeof Priority[keyof typeof Priority];

export interface Todo {
  id: string;
  title: string;
  description: string | null;
  priority: Priority;
  dueDate: string | null;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}
