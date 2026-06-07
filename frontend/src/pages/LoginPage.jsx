import { useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";

import { useAuth } from "../auth/AuthContext";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import "./LoginPage.css";

function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, login } = useAuth();
  const [form, setForm] = useState({
    email: "admin",
    password: "admin",
  });
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  function updateForm(field, value) {
    setForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setIsSaving(true);

    try {
      await login(form);
      const redirectPath = location.state?.from?.pathname || "/";
      navigate(redirectPath, { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <div>
          <h1>Logga in</h1>
          <p>Använd ditt konto för att komma åt schemaläggningen.</p>
        </div>

        {error && <p className="page-error">{error}</p>}

        <form className="login-form" onSubmit={handleSubmit}>
          <Input
            label="Email"
            value={form.email}
            onChange={(event) => updateForm("email", event.target.value)}
          />

          <Input
            label="Lösenord"
            type="password"
            value={form.password}
            onChange={(event) => updateForm("password", event.target.value)}
          />

          <Button type="submit" disabled={isSaving}>
            {isSaving ? "Loggar in..." : "Logga in"}
          </Button>
        </form>
      </section>
    </main>
  );
}

export default LoginPage;
