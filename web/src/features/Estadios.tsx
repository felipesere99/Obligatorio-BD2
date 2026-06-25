import { useState } from "react";
import { api } from "../lib/api";
import type { Estadio } from "../lib/types";
import { Banner, Card, EmptyState, Field, Skeleton, errorMessage, useAsync } from "../components/ui";

export function Estadios() {
  const { data, loading, error, reload } = useAsync(() => api.get<Estadio[]>("/estadios"));

  return (
    <div className="stack">
      <AltaEstadio onDone={reload} />
      <AltaSector estadios={data ?? []} onDone={reload} />

      <Card title="Estadios">
        {loading && <Skeleton rows={3} />}
        {error && <Banner kind="error">{error}</Banner>}
        {data && data.length === 0 && <EmptyState icon="🏟">No hay estadios registrados.</EmptyState>}
        {data?.map((e) => (
          <div key={e.nombre} className="subcard">
            <h3>{e.nombre} {e.direccion && <span className="muted">— {e.direccion}</span>}</h3>
            {e.sectores.length === 0 ? (
              <p className="muted small">(sin sectores)</p>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr><th>Sector</th><th className="num">Capacidad</th><th className="num">Costo</th></tr>
                  </thead>
                  <tbody>
                    {e.sectores.map((s) => (
                      <tr key={s.nombre}>
                        <td>{s.nombre}</td><td className="num">{s.capacidad}</td><td className="num">${s.costoEntrada}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ))}
      </Card>
    </div>
  );
}

function AltaEstadio({ onDone }: { onDone: () => void }) {
  const [nombre, setNombre] = useState("");
  const [direccion, setDireccion] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.post("/estadios", { nombre: nombre.trim(), direccion: direccion.trim() || null });
      setNombre("");
      setDireccion("");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Registrar estadio">
      <form onSubmit={submit} className="inline-form">
        <Field label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
        <Field label="Dirección (opcional)" value={direccion} onChange={(e) => setDireccion(e.target.value)} />
        <button type="submit" disabled={busy}>{busy ? "…" : "Registrar"}</button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}

function AltaSector({ estadios, onDone }: { estadios: Estadio[]; onDone: () => void }) {
  const [estadio, setEstadio] = useState("");
  const [nombre, setNombre] = useState("");
  const [capacidad, setCapacidad] = useState("");
  const [costo, setCosto] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.post(`/estadios/${encodeURIComponent(estadio)}/sectores`, {
        nombre: nombre.trim(),
        capacidad: Number(capacidad),
        costoEntrada: Number(costo),
      });
      setNombre("");
      setCapacidad("");
      setCosto("");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Agregar sector a un estadio">
      <form onSubmit={submit} className="inline-form">
        <label className="field">
          <span>Estadio</span>
          <select value={estadio} onChange={(e) => setEstadio(e.target.value)} required>
            <option value="">— elegir —</option>
            {estadios.map((e) => (
              <option key={e.nombre} value={e.nombre}>{e.nombre}</option>
            ))}
          </select>
        </label>
        <Field label="Nombre del sector" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
        <Field label="Capacidad" type="number" min={1} value={capacidad} onChange={(e) => setCapacidad(e.target.value)} required />
        <Field label="Costo entrada" type="number" min={0} step="0.01" value={costo} onChange={(e) => setCosto(e.target.value)} required />
        <button type="submit" disabled={busy || !estadio}>{busy ? "…" : "Agregar"}</button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}
