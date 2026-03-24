import { useEffect, useRef, useState, type FormEvent } from "react";
import type { Todo } from "../types";
import { Priority } from "../types";
import { useAuth } from "../context/useAuth";
import { fetchTodos, createTodo, updateTodo, deleteTodo, ApiError } from "../lib/api";

function DueDatePicker({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const [showInput, setShowInput] = useState(!!value);

  useEffect(() => {
    if (!value) setShowInput(false);
  }, [value]);

  if (!showInput && !value) {
    return (
      <button
        type="button"
        className="add-due-date-button"
        onClick={() => setShowInput(true)}
      >
        + Add due date
      </button>
    );
  }

  return (
    <div className="due-date-picker">
      <input
        type="date"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        autoFocus={!value}
      />
      <button
        type="button"
        className="due-date-clear"
        onClick={() => {
          onChange("");
          setShowInput(false);
        }}
        aria-label="Clear due date"
      >
        &times;
      </button>
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return "Good morning";
  if (hour < 18) return "Good afternoon";
  return "Good evening";
}

export function TodoPage() {
  const { token, user, signOut } = useAuth();
  const [todos, setTodos] = useState<Todo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);

  // New todo form
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<string>(Priority.MEDIUM);
  const [dueDate, setDueDate] = useState("");

  useEffect(() => {
    if (!token) return;
    fetchTodos(token)
      .then(({ data }) => setTodos(data))
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load todos"))
      .finally(() => setLoading(false));
  }, [token]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!token || !title.trim()) return;

    try {
      const payload: Parameters<typeof createTodo>[1] = {
        title: title.trim(),
        priority,
      };
      if (description.trim()) payload.description = description.trim();
      if (dueDate) payload.dueDate = dueDate;

      const { data } = await createTodo(token, payload);
      setTodos((prev) => [data, ...prev]);
      setTitle("");
      setDescription("");
      setPriority(Priority.MEDIUM);
      setDueDate("");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to create todo");
    }
  }

  async function handleToggleComplete(todo: Todo) {
    if (!token) return;
    const completedAt = todo.completedAt ? null : new Date().toISOString();
    try {
      const { data } = await updateTodo(token, todo.id, { completedAt });
      setTodos((prev) => prev.map((t) => (t.id === data.id ? data : t)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to update todo");
    }
  }

  async function handleUpdate(id: string, data: Partial<Pick<Todo, "title" | "description" | "priority" | "dueDate">>) {
    if (!token) return;
    try {
      const { data: updated } = await updateTodo(token, id, data);
      setTodos((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to update todo");
    }
  }

  async function handleDelete(id: string) {
    if (!token) return;
    try {
      await deleteTodo(token, id);
      setTodos((prev) => prev.filter((t) => t.id !== id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to delete todo");
    }
  }

  const incomplete = todos.filter((t) => !t.completedAt);
  const completed = todos.filter((t) => t.completedAt);

  return (
    <div className="todo-page">
      <header className="todo-header">
        <div className="todo-header-top">
          <h1>{getGreeting()}, {user?.name || user?.email}!</h1>
          <button type="button" onClick={signOut} className="sign-out-button">
            Sign out
          </button>
        </div>
        <p className="todo-subtitle">Here are your todos:</p>
      </header>

      {error && <p className="error">{error}</p>}

      <form onSubmit={handleCreate} className="todo-form">
        <input
          type="text"
          placeholder="What needs to be done?"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          autoFocus
        />
        <textarea
          placeholder="Description (optional)"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={3}
        />
        <div className="todo-form-row">
          <select value={priority} onChange={(e) => setPriority(e.target.value)}>
            <option value={Priority.LOW}>Low</option>
            <option value={Priority.MEDIUM}>Medium</option>
            <option value={Priority.HIGH}>High</option>
          </select>
          <DueDatePicker value={dueDate} onChange={setDueDate} />
          <button type="submit">Add</button>
        </div>
      </form>

      {loading ? (
        <p className="loading">Loading...</p>
      ) : (
        <>
          {incomplete.length > 0 && <h2 className="section-heading">In Progress</h2>}
          <ul className="todo-list">
            {incomplete.map((todo) => (
              <TodoItem
                key={todo.id}
                todo={todo}
                editing={editingId === todo.id}
                onStartEdit={() => setEditingId(todo.id)}
                onStopEdit={() => setEditingId(null)}
                onToggle={handleToggleComplete}
                onUpdate={handleUpdate}
                onDelete={handleDelete}
              />
            ))}
          </ul>

          {completed.length > 0 && (
            <>
              <h2 className="section-heading">Completed</h2>
              <ul className="todo-list completed">
                {completed.map((todo) => (
                  <TodoItem
                    key={todo.id}
                    todo={todo}
                    editing={editingId === todo.id}
                    onStartEdit={() => setEditingId(todo.id)}
                    onStopEdit={() => setEditingId(null)}
                    onToggle={handleToggleComplete}
                    onUpdate={handleUpdate}
                    onDelete={handleDelete}
                  />
                ))}
              </ul>
            </>
          )}

          {todos.length === 0 && <p className="empty">No todos yet. Add one above!</p>}
        </>
      )}
    </div>
  );
}

function TodoItem({
  todo,
  editing,
  onStartEdit,
  onStopEdit,
  onToggle,
  onUpdate,
  onDelete,
}: {
  todo: Todo;
  editing: boolean;
  onStartEdit: () => void;
  onStopEdit: () => void;
  onToggle: (todo: Todo) => void;
  onUpdate: (id: string, data: Partial<Pick<Todo, "title" | "description" | "priority" | "dueDate">>) => void;
  onDelete: (id: string) => void;
}) {
  const cardRef = useRef<HTMLLIElement>(null);
  const [editTitle, setEditTitle] = useState(todo.title);
  const [editDescription, setEditDescription] = useState(todo.description ?? "");
  const [editPriority, setEditPriority] = useState(todo.priority);
  const [editDueDate, setEditDueDate] = useState(
    todo.dueDate ? new Date(todo.dueDate).toISOString().split("T")[0] : "",
  );

  const isComplete = !!todo.completedAt;

  // Reset form fields when entering edit mode
  useEffect(() => {
    if (editing) {
      setEditTitle(todo.title);
      setEditDescription(todo.description ?? "");
      setEditPriority(todo.priority);
      setEditDueDate(todo.dueDate ? new Date(todo.dueDate).toISOString().split("T")[0] : "");
    }
  }, [editing, todo]);

  // Click outside to cancel
  useEffect(() => {
    if (!editing) return;
    function handleClickOutside(e: MouseEvent) {
      if (cardRef.current && !cardRef.current.contains(e.target as Node)) {
        onStopEdit();
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [editing, onStopEdit]);

  function handleSave() {
    if (!editTitle.trim()) return;
    onUpdate(todo.id, {
      title: editTitle.trim(),
      description: editDescription.trim() || undefined,
      priority: editPriority,
      dueDate: editDueDate || undefined,
    });
    onStopEdit();
  }

  if (editing) {
    return (
      <li className="todo-item editing" ref={cardRef}>
        <div className="todo-edit-form">
          <input
            type="text"
            value={editTitle}
            onChange={(e) => setEditTitle(e.target.value)}
            autoFocus
            onKeyDown={(e) => {
              if (e.key === "Enter") handleSave();
              if (e.key === "Escape") onStopEdit();
            }}
          />
          <textarea
            placeholder="Description (optional)"
            value={editDescription}
            onChange={(e) => setEditDescription(e.target.value)}
            rows={2}
          />
          <div className="todo-edit-row">
            <select value={editPriority} onChange={(e) => setEditPriority(e.target.value as Todo["priority"])}>
              <option value={Priority.LOW}>Low</option>
              <option value={Priority.MEDIUM}>Medium</option>
              <option value={Priority.HIGH}>High</option>
            </select>
            <DueDatePicker value={editDueDate} onChange={setEditDueDate} />
            <button type="button" className="save-button" onClick={handleSave}>Save</button>
            <button type="button" className="cancel-button" onClick={onStopEdit}>Cancel</button>
            <button
              type="button"
              className="delete-button"
              onClick={() => onDelete(todo.id)}
            >
              Delete
            </button>
          </div>
        </div>
      </li>
    );
  }

  return (
    <li ref={cardRef} className={`todo-item ${isComplete ? "done" : ""} priority-${todo.priority.toLowerCase()}`}>
      <button
        type="button"
        className="todo-checkbox"
        onClick={() => onToggle(todo)}
        aria-label={isComplete ? "Mark incomplete" : "Mark complete"}
      >
        {isComplete ? "\u2713" : ""}
      </button>
      <div className="todo-content" onClick={onStartEdit} role="button" tabIndex={0}>
        <span className="todo-title">{todo.title}</span>
        {todo.description && <span className="todo-description">{todo.description}</span>}
        <div className="todo-meta">
          <span className={`priority-badge ${todo.priority.toLowerCase()}`}>{todo.priority}</span>
          {todo.dueDate && (
            <span className="due-date">Due {new Date(todo.dueDate).toLocaleDateString()}</span>
          )}
        </div>
      </div>
    </li>
  );
}
