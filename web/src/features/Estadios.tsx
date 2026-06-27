import { useEffect, useState } from "react";
import { api } from "../lib/api";
import type { Estadio, Sector } from "../lib/types";
import { Banner, Card, EmptyState, Field, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

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
          <EstadioCard key={e.nombre} estadio={e} onDone={reload} />
        ))}
      </Card>
    </div>
  );
}

function EstadioCard({ estadio, onDone }: { estadio: Estadio; onDone: () => void }) {
  const toast = useToast();
  const [editing, setEditing] = useState(false);
  const [direccion, setDireccion] = useState(estadio.direccion ?? "");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!editing) setDireccion(estadio.direccion ?? "");
  }, [estadio, editing]);

  async function guardar(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/estadios/${encodeURIComponent(estadio.nombre)}`, { direccion: direccion.trim() || null });
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
      await api.delete(`/estadios/${encodeURIComponent(estadio.nombre)}`);
      toast.success(`Estadio "${estadio.nombre}" eliminado.`);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="subcard">
      <div className="row" style={{ justifyContent: "space-between", alignItems: "baseline" }}>
        <h3>{estadio.nombre} {estadio.direccion && !editing && <span className="muted">— {estadio.direccion}</span>}</h3>
        <div className="row">
          <button className="secondary" type="button" onClick={() => setEditing((v) => !v)} disabled={busy}>
            {editing ? "Cancelar" : "Editar"}
          </button>
          <button className="danger" type="button" onClick={eliminar} disabled={busy}>{busy ? "…" : "Eliminar"}</button>
        </div>
      </div>

      {editing && (
        <form onSubmit={guardar} className="inline-form">
          <Field label="Dirección" value={direccion} onChange={(e) => setDireccion(e.target.value)} />
          <button type="submit" disabled={busy}>{busy ? "…" : "Guardar"}</button>
        </form>
      )}

      {err && <Banner kind="error">{err}</Banner>}

      {estadio.sectores.length === 0 ? (
        <p className="muted small">(sin sectores)</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Sector</th><th className="num">Capacidad</th><th className="num">Costo</th><th></th></tr>
            </thead>
            <tbody>
              {estadio.sectores.map((s) => (
                <SectorRow key={s.nombre} estadio={estadio.nombre} sector={s} onDone={onDone} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function SectorRow({ estadio, sector, onDone }: { estadio: string; sector: Sector; onDone: () => void }) {
  const toast = useToast();
  const [editing, setEditing] = useState(false);
  const [capacidad, setCapacidad] = useState(String(sector.capacidad));
  const [costo, setCosto] = useState(String(sector.costoEntrada));
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!editing) {
      setCapacidad(String(sector.capacidad));
      setCosto(String(sector.costoEntrada));
    }
  }, [sector, editing]);

  async function guardar(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/estadios/${encodeURIComponent(estadio)}/sectores/${encodeURIComponent(sector.nombre)}`, {
        capacidad: Number(capacidad),
        costoEntrada: Number(costo),
      });
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
      await api.delete(`/estadios/${encodeURIComponent(estadio)}/sectores/${encodeURIComponent(sector.nombre)}`);
      toast.success(`Sector "${sector.nombre}" eliminado.`);
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
        <td>{sector.nombre}</td>
        <td colSpan={3}>
          <form onSubmit={guardar} className="inline-form">
            <Field label="Capacidad" type="number" min={1} value={capacidad} onChange={(e) => setCapacidad(e.target.value)} required />
            <Field label="Costo entrada" type="number" min={0} step="0.01" value={costo} onChange={(e) => setCosto(e.target.value)} required />
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
      <td>
        {sector.nombre}
        {err && <Banner kind="error">{err}</Banner>}
      </td>
      <td className="num">{sector.capacidad}</td>
      <td className="num">${sector.costoEntrada}</td>
      <td>
        <div className="row">
          <button className="secondary" type="button" onClick={() => setEditing(true)} disabled={busy}>Editar</button>
          <button className="danger" type="button" onClick={eliminar} disabled={busy}>{busy ? "…" : "Eliminar"}</button>
        </div>
      </td>
    </tr>
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
