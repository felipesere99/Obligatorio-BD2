import { useState } from "react";
import { useSession } from "../lib/session";
import { Banner, Field, errorMessage } from "../components/ui";

const DEMO = [
  ["ADM-1", "administrador"],
  ["FUN-1", "funcionario"],
  ["UG-1", "usuario general (Ana)"],
];

export function Login({ onRegister }: { onRegister: () => void }) {
  const { login } = useSession();
  const [documento, setDocumento] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(documento.trim());
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth">
      <div className="card auth-card">
        <h1>Ticketing</h1>
        <p className="muted">Ingresá con tu documento (no hay contraseña).</p>
        <form onSubmit={submit}>
          <Field
            label="Documento"
            value={documento}
            onChange={(e) => setDocumento(e.target.value)}
            placeholder="ej. UG-1"
            autoFocus
          />
          {error && <Banner kind="error">{error}</Banner>}
          <button type="submit" disabled={busy || !documento.trim()}>
            {busy ? "Ingresando…" : "Ingresar"}
          </button>
        </form>

        <p className="muted small">
          ¿No tenés cuenta?{" "}
          <button className="link" type="button" onClick={onRegister}>
            Registrate
          </button>
        </p>

        <details className="demo">
          <summary>Usuarios de prueba</summary>
          <ul>
            {DEMO.map(([doc, rol]) => (
              <li key={doc}>
                <button className="link" type="button" onClick={() => setDocumento(doc)}>
                  {doc}
                </button>{" "}
                — {rol}
              </li>
            ))}
          </ul>
        </details>
      </div>
    </div>
  );
}
