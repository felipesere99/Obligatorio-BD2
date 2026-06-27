import { useState } from "react";
import { api } from "../lib/api";
import type { Equipo, Estadio, Evento } from "../lib/types";
import { Banner, Card, EmptyState, Field, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

function fmt(iso: string) {
  return new Date(iso).toLocaleString("es-UY", { dateStyle: "short", timeStyle: "short" });
}

export function Eventos() {
  const eventos = useAsync(() => api.get<Evento[]>("/eventos"));
  const equipos = useAsync(() => api.get<Equipo[]>("/equipos"));
  const estadios = useAsync(() => api.get<Estadio[]>("/estadios"));

  function reloadEventos() {
    eventos.reload();
  }

  return (
    <div className="stack">
      <AltaEvento equipos={equipos.data ?? []} estadios={estadios.data ?? []} onDone={reloadEventos} />
      <HabilitarSector eventos={eventos.data ?? []} estadios={estadios.data ?? []} onDone={reloadEventos} />

      <Card title="Eventos">
        {eventos.loading && <Skeleton rows={3} />}
        {eventos.error && <Banner kind="error">{eventos.error}</Banner>}
        {eventos.data && eventos.data.length === 0 && <EmptyState icon="📅">No hay eventos creados.</EmptyState>}
        {eventos.data?.map((ev) => (
          <EventoCard
            key={ev.idEvento}
            ev={ev}
            equipos={equipos.data ?? []}
            estadios={estadios.data ?? []}
            onDone={reloadEventos}
          />
        ))}
      </Card>
    </div>
  );
}

function AltaEvento({
  equipos,
  estadios,
  onDone,
}: {
  equipos: Equipo[];
  estadios: Estadio[];
  onDone: () => void;
}) {
  const [nombre, setNombre] = useState("");
  const [inicio, setInicio] = useState("");
  const [fin, setFin] = useState("");
  const [local, setLocal] = useState("");
  const [visitante, setVisitante] = useState("");
  const [estadio, setEstadio] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.post("/eventos", {
        nombre: nombre.trim(),
        fechaInicio: inicio,
        fechaFin: fin,
        paisLocal: local,
        paisVisitante: visitante,
        nombreEstadio: estadio,
      });
      setNombre("");
      setInicio("");
      setFin("");
      setLocal("");
      setVisitante("");
      setEstadio("");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Crear evento">
      <form onSubmit={submit} className="grid-form">
        <Field label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
        <Field label="Inicio" type="datetime-local" value={inicio} onChange={(e) => setInicio(e.target.value)} required />
        <Field label="Fin" type="datetime-local" value={fin} onChange={(e) => setFin(e.target.value)} required />
        <EquipoSelect label="País local" equipos={equipos} value={local} onChange={setLocal} />
        <EquipoSelect label="País visitante" equipos={equipos} value={visitante} onChange={setVisitante} />
        <label className="field">
          <span>Estadio</span>
          <select value={estadio} onChange={(e) => setEstadio(e.target.value)} required>
            <option value="">— elegir —</option>
            {estadios.map((es) => <option key={es.nombre} value={es.nombre}>{es.nombre}</option>)}
          </select>
        </label>
        <button type="submit" disabled={busy}>{busy ? "…" : "Crear evento"}</button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}

function toLocalInput(iso: string) {
  return iso.slice(0, 16);
}

function EventoCard({
  ev,
  equipos,
  estadios,
  onDone,
}: {
  ev: Evento;
  equipos: Equipo[];
  estadios: Estadio[];
  onDone: () => void;
}) {
  const toast = useToast();
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [editing, setEditing] = useState(false);

  async function deshabilitarSector(sector: string) {
    setErr(null);
    setBusy(true);
    try {
      await api.delete(`/eventos/${ev.idEvento}/sectores/${encodeURIComponent(sector)}`);
      toast.success(`Sector "${sector}" deshabilitado.`);
      onDone();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function eliminar() {
    setErr(null);
    setBusy(true);
    try {
      await api.delete(`/eventos/${ev.idEvento}`);
      toast.success(`Evento #${ev.idEvento} eliminado.`);
      onDone();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="subcard">
      <div className="row" style={{ justifyContent: "space-between", alignItems: "baseline" }}>
        <h3>#{ev.idEvento} — {ev.nombre}</h3>
        <div className="row">
          <button className="secondary" type="button" onClick={() => setEditing((v) => !v)} disabled={busy}>
            {editing ? "Cancelar" : "Editar"}
          </button>
          <button className="danger" type="button" onClick={eliminar} disabled={busy}>{busy ? "…" : "Eliminar"}</button>
        </div>
      </div>
      <p className="muted small">
        {ev.paisLocal} vs {ev.paisVisitante} · {ev.nombreEstadio}
        <br />
        {fmt(ev.fechaInicio)} → {fmt(ev.fechaFin)}
      </p>

      {editing && (
        <EditarEvento ev={ev} equipos={equipos} estadios={estadios} onDone={() => { setEditing(false); onDone(); }} />
      )}

      <p className="small">Sectores habilitados:</p>
      {ev.sectoresHabilitados.length === 0 ? (
        <span className="muted small">(ninguno)</span>
      ) : (
        <div className="chip-list">
          {ev.sectoresHabilitados.map((s) => (
            <span className="chip" key={s}>
              {s}
              <button type="button" className="chip-button" onClick={() => deshabilitarSector(s)} disabled={busy}>
                x
              </button>
            </span>
          ))}
        </div>
      )}
      {err && <Banner kind="error">{err}</Banner>}
    </div>
  );
}

function EditarEvento({
  ev,
  equipos,
  estadios,
  onDone,
}: {
  ev: Evento;
  equipos: Equipo[];
  estadios: Estadio[];
  onDone: () => void;
}) {
  const [nombre, setNombre] = useState(ev.nombre);
  const [inicio, setInicio] = useState(toLocalInput(ev.fechaInicio));
  const [fin, setFin] = useState(toLocalInput(ev.fechaFin));
  const [local, setLocal] = useState(ev.paisLocal ?? "");
  const [visitante, setVisitante] = useState(ev.paisVisitante ?? "");
  const [estadio, setEstadio] = useState(ev.nombreEstadio);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/eventos/${ev.idEvento}`, {
        nombre: nombre.trim(),
        fechaInicio: inicio,
        fechaFin: fin,
        paisLocal: local,
        paisVisitante: visitante,
        nombreEstadio: estadio,
      });
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <form onSubmit={submit} className="grid-form">
        <Field label="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} required />
        <Field label="Inicio" type="datetime-local" value={inicio} onChange={(e) => setInicio(e.target.value)} required />
        <Field label="Fin" type="datetime-local" value={fin} onChange={(e) => setFin(e.target.value)} required />
        <EquipoSelect label="País local" equipos={equipos} value={local} onChange={setLocal} />
        <EquipoSelect label="País visitante" equipos={equipos} value={visitante} onChange={setVisitante} />
        <label className="field">
          <span>Estadio</span>
          <select value={estadio} onChange={(e) => setEstadio(e.target.value)} required>
            <option value="">— elegir —</option>
            {estadios.map((es) => <option key={es.nombre} value={es.nombre}>{es.nombre}</option>)}
          </select>
        </label>
        <button type="submit" disabled={busy}>{busy ? "…" : "Guardar"}</button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </>
  );
}

function EquipoSelect({
  label,
  equipos,
  value,
  onChange,
}: {
  label: string;
  equipos: Equipo[];
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      <select value={value} onChange={(e) => onChange(e.target.value)} required>
        <option value="">— elegir —</option>
        {equipos.map((eq) => (
          <option key={eq.pais} value={eq.pais}>{eq.nombre} ({eq.pais})</option>
        ))}
      </select>
    </label>
  );
}

function HabilitarSector({
  eventos,
  estadios,
  onDone,
}: {
  eventos: Evento[];
  estadios: Estadio[];
  onDone: () => void;
}) {
  const [idEvento, setIdEvento] = useState("");
  const [sector, setSector] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const evento = eventos.find((e) => String(e.idEvento) === idEvento);
  const estadio = estadios.find((es) => es.nombre === evento?.nombreEstadio);
  const sectores = estadio?.sectores ?? [];

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!evento) return;
    setErr(null);
    setBusy(true);
    try {
      await api.post(`/eventos/${evento.idEvento}/sectores`, {
        nombreEstadio: evento.nombreEstadio,
        nombreSector: sector,
      });
      setSector("");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Habilitar sector en evento">
      <form onSubmit={submit} className="inline-form">
        <label className="field">
          <span>Evento</span>
          <select value={idEvento} onChange={(e) => { setIdEvento(e.target.value); setSector(""); }} required>
            <option value="">— elegir —</option>
            {eventos.map((ev) => (
              <option key={ev.idEvento} value={ev.idEvento}>#{ev.idEvento} — {ev.nombre}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Sector ({evento?.nombreEstadio ?? "—"})</span>
          <select value={sector} onChange={(e) => setSector(e.target.value)} required disabled={!evento}>
            <option value="">— elegir —</option>
            {sectores.map((s) => <option key={s.nombre} value={s.nombre}>{s.nombre}</option>)}
          </select>
        </label>
        <button type="submit" disabled={busy || !evento || !sector}>{busy ? "…" : "Habilitar"}</button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}
