import { useEffect, useState } from "react";
import { api } from "../lib/api";
import type { Equipo } from "../lib/types";
import { Banner, Card, EmptyState, Field, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

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
        {loading && <Skeleton rows={3} />}
        {error && <Banner kind="error">{error}</Banner>}
        {data && data.length === 0 && <EmptyState icon="⚽">No hay equipos registrados.</EmptyState>}
        {data && data.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr><th>País</th><th>Nombre</th><th></th></tr>
              </thead>
              <tbody>
                {data.map((e) => (
                  <EquipoRow key={e.pais} equipo={e} onDone={reload} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}

function EquipoRow({ equipo, onDone }: { equipo: Equipo; onDone: () => void }) {
  const toast = useToast();
  const [editing, setEditing] = useState(false);
  const [nombre, setNombre] = useState(equipo.nombre);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!editing) setNombre(equipo.nombre);
  }, [equipo, editing]);

  async function guardar(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/equipos/${encodeURIComponent(equipo.pais)}`, { nombre: nombre.trim() });
      setEditing(false);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  async function eliminar() {
    setErr(null);
    setBusy(true);
    try {
      await api.delete(`/equipos/${encodeURIComponent(equipo.pais)}`);
      toast.success(`Equipo "${equipo.pais}" eliminado.`);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  if (editing) {
    return (
      <tr>
        <td>{equipo.pais}</td>
        <td colSpan={2}>
          <form onSubmit={guardar} className="inline-form">
            <Field label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
            <button type="submit" disabled={busy}>{busy ? "…" : "Guardar"}</button>
            <button className="secondary" type="button" onClick={() => setEditing(false)} disabled={busy}>Cancelar</button>
          </form>
          {err && <Banner kind="error">{err}</Banner>}
        </td>
      </tr>
    );
  }

  return (
    <tr>
      <td>{equipo.pais}</td>
      <td>
        {equipo.nombre}
        {err && <Banner kind="error">{err}</Banner>}
      </td>
      <td>
        <div className="row">
          <button className="secondary" type="button" onClick={() => setEditing(true)} disabled={busy}>Editar</button>
          <button className="danger" type="button" onClick={eliminar} disabled={busy}>{busy ? "…" : "Eliminar"}</button>
        </div>
      </td>
    </tr>
  );
}
