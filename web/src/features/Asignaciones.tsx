import { useState } from "react";
import { api } from "../lib/api";
import type { Asignacion, Evento, Funcionario } from "../lib/types";
import { Banner, Card, EmptyState, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

export function Asignaciones() {
  const asignaciones = useAsync(() => api.get<Asignacion[]>("/asignaciones"));
  const funcionarios = useAsync(() => api.get<Funcionario[]>("/usuarios/funcionarios"));
  const eventos = useAsync(() => api.get<Evento[]>("/eventos"));

  function reloadAsignaciones() {
    asignaciones.reload();
  }

  return (
    <div className="stack">
      <AsignarFuncionario
        funcionarios={funcionarios.data ?? []}
        eventos={eventos.data ?? []}
        onDone={reloadAsignaciones}
      />

      <Card title="Asignaciones">
        {(asignaciones.loading || funcionarios.loading || eventos.loading) && <Skeleton rows={3} />}
        {asignaciones.error && <Banner kind="error">{asignaciones.error}</Banner>}
        {funcionarios.error && <Banner kind="error">{funcionarios.error}</Banner>}
        {eventos.error && <Banner kind="error">{eventos.error}</Banner>}
        {asignaciones.data && asignaciones.data.length === 0 && <EmptyState icon="🧑‍✈️">No hay asignaciones.</EmptyState>}
        {asignaciones.data && asignaciones.data.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Funcionario</th>
                  <th>Evento</th>
                  <th>Estadio</th>
                  <th>Sector</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {asignaciones.data.map((a) => (
                  <AsignacionRow
                    key={`${a.docFuncionario}-${a.idEvento}-${a.nombreEstadio}-${a.nombreSector}`}
                    asignacion={a}
                    onDone={reloadAsignaciones}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}

function AsignarFuncionario({
  funcionarios,
  eventos,
  onDone,
}: {
  funcionarios: Funcionario[];
  eventos: Evento[];
  onDone: () => void;
}) {
  const toast = useToast();
  const [docFuncionario, setDocFuncionario] = useState("");
  const [idEvento, setIdEvento] = useState("");
  const [sector, setSector] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const evento = eventos.find((e) => String(e.idEvento) === idEvento);
  const sectores = evento?.sectoresHabilitados ?? [];

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!evento) return;
    setErr(null);
    setBusy(true);
    try {
      await api.post("/asignaciones", {
        docFuncionario,
        idEvento: evento.idEvento,
        nombreEstadio: evento.nombreEstadio,
        nombreSector: sector,
      });
      setDocFuncionario("");
      setIdEvento("");
      setSector("");
      toast.success("Asignación creada.");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Asignar funcionario">
      <form onSubmit={submit} className="grid-form">
        <label className="field">
          <span>Funcionario</span>
          <select value={docFuncionario} onChange={(e) => setDocFuncionario(e.target.value)} required>
            <option value="">— elegir —</option>
            {funcionarios.map((f) => (
              <option key={f.documento} value={f.documento}>
                {f.nombre} {f.apellido} ({f.documento})
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Evento</span>
          <select value={idEvento} onChange={(e) => { setIdEvento(e.target.value); setSector(""); }} required>
            <option value="">— elegir —</option>
            {eventos.map((ev) => (
              <option key={ev.idEvento} value={ev.idEvento}>
                #{ev.idEvento} — {ev.nombre}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Sector ({evento?.nombreEstadio ?? "—"})</span>
          <select value={sector} onChange={(e) => setSector(e.target.value)} required disabled={!evento || sectores.length === 0}>
            <option value="">— elegir —</option>
            {sectores.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </label>
        <button type="submit" disabled={busy || !docFuncionario || !evento || !sector}>
          {busy ? "…" : "Asignar"}
        </button>
      </form>
      {evento && sectores.length === 0 && <p className="muted small">El evento seleccionado no tiene sectores habilitados.</p>}
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}

function AsignacionRow({ asignacion, onDone }: { asignacion: Asignacion; onDone: () => void }) {
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function remove() {
    setErr(null);
    setBusy(true);
    try {
      await api.delete("/asignaciones", {
        docFuncionario: asignacion.docFuncionario,
        idEvento: asignacion.idEvento,
        nombreEstadio: asignacion.nombreEstadio,
        nombreSector: asignacion.nombreSector,
      });
      onDone();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <tr>
      <td>
        {asignacion.nombreFuncionario}
        <br />
        <span className="muted small">{asignacion.docFuncionario}</span>
        {err && <Banner kind="error">{err}</Banner>}
      </td>
      <td>#{asignacion.idEvento}</td>
      <td>{asignacion.nombreEstadio}</td>
      <td>{asignacion.nombreSector}</td>
      <td><button className="secondary" type="button" onClick={remove} disabled={busy}>{busy ? "…" : "Quitar"}</button></td>
    </tr>
  );
}
