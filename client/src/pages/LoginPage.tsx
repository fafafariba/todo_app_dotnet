import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import { login, register, ApiError } from "../lib/api";

export function LoginPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();

  const [isRegister, setIsRegister] = useState(false);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const { data } = isRegister
        ? await register(email, password, name)
        : await login(email, password);
      signIn(data.token, data.user);
      navigate("/");
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError("Something went wrong");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <h1>{isRegister ? "Create account" : "Sign in"}</h1>

      <form onSubmit={handleSubmit} className="auth-form">
        {error && <p className="error">{error}</p>}

        {isRegister && (
          <label>
            Name (optional)
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoFocus
            />
          </label>
        )}

        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoFocus
          />
        </label>

        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={isRegister ? 8 : 1}
          />
        </label>

        <button type="submit" disabled={loading}>
          {loading ? "Loading..." : isRegister ? "Register" : "Sign in"}
        </button>
      </form>

      <p className="auth-switch">
        {isRegister ? "Already have an account?" : "Don't have an account?"}{" "}
        <button
          type="button"
          className="link-button"
          onClick={() => {
            setIsRegister(!isRegister);
            setName("");
            setEmail("");
            setPassword("");
            setError("");
          }}
        >
          {isRegister ? "Sign in" : "Register"}
        </button>
      </p>
    </div>
  );
}
