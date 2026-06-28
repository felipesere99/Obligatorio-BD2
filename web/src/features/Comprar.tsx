import { useState } from "react";
import { api } from "../lib/api";
import type { CompraItem, Evento, SectorDisponibilidad, VentaCreada } from "../lib/types";
import {
  Badge,
  Banner,
  Card,
  EmptyState,
  Field,
  Loading,
  errorMessage,
  useAsync,
  useToast,
} from "../components/ui";

const MAX_ITEMS = 5;
const POCOS_CUPOS = 5;

export function Comprar() {
  const toast = useToast();
  const eventos = useAsync(() => api.get<Evento[]>("/eventos"));

  const [items, setItems] = useState<CompraItem[]>([]);
  const [idEvento, setIdEvento] = useState("");
  const [sector, setSector] = useState("");
  const [fila, setFila] = useState("");
  const [asiento, setAsiento] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const disponibilidad = useAsync(
    () =>
      idEvento
        ? api.get<SectorDisponibilidad[]>(`/eventos/${idEvento}/disponibilidad`)
        : Promise.resolve([]),
    [idEvento],
  );

  const evento = eventos.data?.find((e) => String(e.idEvento) === idEvento);
  const sectores = disponibilidad.data ?? [];
  const sectorSel = sectores.find((s) => s.nombreSector === sector);
  const sectorAgotado = sectorSel?.disponibles === 0;

  function resetDraft() {
    setIdEvento("");
    setSector("");
    setFila("");
    setAsiento("");
  }

  function addItem(e: React.FormEvent) {
    e.preventDefault();
    if (!evento || items.length >= MAX_ITEMS) return;
    setItems((prev) => [
      ...prev,
      {
        idEvento: evento.idEvento,
        estadio: evento.nombreEstadio,
        sector,
        fila: fila.trim() || null,
        asiento: asiento.trim() || null,
      },
    ]);
    resetDraft();
  }

  function removeItem(i: number) {
    setItems((prev) => prev.filter((_, idx) => idx !== i));
  }

  async function comprar() {
    setErr(null);
    setBusy(true);
    const cantidad = items.length;
    try {
      const venta = await api.post<VentaCreada>("/ventas", { items });
      toast.success(`Compra confirmada · Venta #${venta.nroVenta} · total $${venta.montoTotal} (${cantidad} entrada(s)).`);
      setItems([]);
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  function labelEvento(id: number) {
    const ev = eventos.data?.find((e) => e.idEvento === id);
    return ev ? `#${ev.idEvento} — ${ev.nombre}` : String(id);
  }

  const loading = eventos.loading;
  const loadErr = eventos.error;

  return (
    <div className="stack">
      <Card title="Comprar entradas">
        <p className="muted small">Agregá hasta {MAX_ITEMS} entradas al carrito.</p>
        {loading && <Loading />}
        {loadErr && <Banner kind="error">{loadErr}</Banner>}
        {eventos.data && eventos.data.length === 0 && (
          <p className="muted">No hay eventos disponibles.</p>
        )}
        {eventos.data && eventos.data.length > 0 && (
          <form onSubmit={addItem} className="grid-form">
            <label className="field">
              <span>Evento</span>
              <select
                value={idEvento}
                onChange={(e) => { setIdEvento(e.target.value); setSector(""); }}
                required
              >
                <option value="">— elegir evento —</option>
                {eventos.data.map((ev) => (
                  <option key={ev.idEvento} value={ev.idEvento}>
                    #{ev.idEvento} — {ev.nombre} ({ev.paisLocal} vs {ev.paisVisitante})
                  </option>
                ))}
              </select>
            </label>
            <Field
              label="Estadio"
              value={evento?.nombreEstadio ?? ""}
              readOnly
              placeholder="Elegí un evento"
            />
            <label className="field">
              <span>Sector{evento ? ` (${evento.nombreEstadio})` : ""}</span>
              <select
                value={sector}
                onChange={(e) => setSector(e.target.value)}
                required
                disabled={!evento || disponibilidad.loading || sectores.length === 0}
              >
                <option value="">
                  {!evento
                    ? "— elegir evento primero —"
                    : disponibilidad.loading
                      ? "— cargando sectores —"
                      : sectores.length === 0
                        ? "— sin sectores habilitados —"
                        : "— elegir sector —"}
                </option>
                {sectores.map((s) => (
                  <option
                    key={s.nombreSector}
                    value={s.nombreSector}
                    disabled={s.disponibles === 0}
                  >
                    {s.nombreSector} — ${s.costoEntrada} —{" "}
                    {s.disponibles === 0 ? "Agotado" : `${s.disponibles} disponibles`}
                  </option>
                ))}
              </select>
              {disponibilidad.error && (
                <span className="muted small">{disponibilidad.error}</span>
              )}
              {sectorSel && sectorSel.disponibles > 0 && sectorSel.disponibles <= POCOS_CUPOS && (
                <span className="muted small">
                  Quedan {sectorSel.disponibles} entradas en este sector.
                </span>
              )}
            </label>
            <Field label="Fila (opcional)" value={fila} onChange={(e) => setFila(e.target.value)} disabled={!evento} />
            <Field label="Asiento (opcional)" value={asiento} onChange={(e) => setAsiento(e.target.value)} disabled={!evento} />
            <button
              type="submit"
              disabled={items.length >= MAX_ITEMS || !evento || !sector || sectorAgotado}
            >
              Agregar ítem
            </button>
          </form>
        )}
      </Card>

      <Card title={`Carrito (${items.length}/${MAX_ITEMS})`}>
        {items.length === 0 ? (
          <EmptyState icon="🛒">Tu carrito está vacío. Agregá entradas desde el formulario de arriba.</EmptyState>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr><th>Evento</th><th>Estadio</th><th>Sector</th><th>Fila</th><th>Asiento</th><th>Estado</th><th></th></tr>
              </thead>
              <tbody>
                {items.map((it, i) => (
                  <tr key={i}>
                    <td>{labelEvento(it.idEvento)}</td>
                    <td>{it.estadio}</td>
                    <td>{it.sector}</td>
                    <td>{it.fila ?? "—"}</td>
                    <td>{it.asiento ?? "—"}</td>
                    <td><Badge tone="warn">pendiente</Badge></td>
                    <td><button className="link" onClick={() => removeItem(i)}>quitar</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {err && <Banner kind="error">{err}</Banner>}
        <div className="row">
          <button onClick={comprar} disabled={busy || items.length === 0}>
            {busy ? "Comprando…" : "Confirmar compra"}
          </button>
        </div>
      </Card>
    </div>
  );
}
