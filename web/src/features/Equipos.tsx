import { useState } from "react";
import { api } from "../lib/api";
import type { Equipo } from "../lib/types";
import { Banner, Card, Field, Loading, errorMessage, useAsync } from "../components/ui";

export function Equipos() {
  const { data, loading, error, reload } = useAsync(() => api.get<Equipo[]>("/equipos"));
  const [pais, setPais] = useState("");
  const [nombre, setNombre] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    setBusy(true);
    try {
      await api.post("/equipos", { pais: pais.trim(), nombre: nombre.trim() });
      setPais("");
      setNombre("");
      reload();
    } catch (err) {
      setFormError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="stack">
      <Card title="Registrar equipo">
        <form onSubmit={submit} className="inline-form">
          <Field label="País (código)" value={pais} onChange={(e) => setPais(e.target.value)} placeholder="ej. URU" required />
          <Field label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
          <button type="submit" disabled={busy}>{busy ? "…" : "Registrar"}</button>
        </form>
        {formError && <Banner kind="error">{formError}</Banner>}
      </Card>

      <Card title="Equipos">
        {loading && <Loading />}
        {error && <Banner kind="error">{error}</Banner>}
        {data && data.length === 0 && <p className="muted">No hay equipos.</p>}
        {data && data.length > 0 && (
          <table>
            <thead>
              <tr><th>País</th><th>Nombre</th></tr>
            </thead>
            <tbody>
              {data.map((e) => (
                <tr key={e.pais}><td>{e.pais}</td><td>{e.nombre}</td></tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
