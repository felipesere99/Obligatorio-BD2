import { useState } from "react";
import { api } from "../lib/api";
import type { CompraItem, VentaCreada } from "../lib/types";
import { Banner, Card, Field, errorMessage } from "../components/ui";

const MAX_ITEMS = 5;

export function Comprar() {
  const [items, setItems] = useState<CompraItem[]>([]);
  const [draft, setDraft] = useState({ idEvento: "", estadio: "", sector: "", fila: "", asiento: "" });
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function addItem(e: React.FormEvent) {
    e.preventDefault();
    if (items.length >= MAX_ITEMS) return;
    setItems((prev) => [
      ...prev,
      {
        idEvento: Number(draft.idEvento),
        estadio: draft.estadio.trim(),
        sector: draft.sector.trim(),
        fila: draft.fila.trim() || null,
        asiento: draft.asiento.trim() || null,
      },
    ]);
    setDraft({ idEvento: "", estadio: "", sector: "", fila: "", asiento: "" });
    setOk(null);
  }

  function removeItem(i: number) {
    setItems((prev) => prev.filter((_, idx) => idx !== i));
  }

  async function comprar() {
    setErr(null);
    setOk(null);
    setBusy(true);
    try {
      const venta = await api.post<VentaCreada>("/ventas", { items });
      setOk(`Compra OK. Venta #${venta.nroVenta} — total $${venta.montoTotal} (${items.length} entrada(s)).`);
      setItems([]);
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  function set(k: keyof typeof draft, v: string) {
    setDraft((d) => ({ ...d, [k]: v }));
  }

  return (
    <div className="stack">
      <Card title="Comprar entradas">
        <p className="muted small">
          Agregá hasta {MAX_ITEMS} entradas. Necesitás el id de evento, estadio y sector
          (pedíselos al administrador del evento).
        </p>
        <form onSubmit={addItem} className="grid-form">
          <Field label="Id de evento" type="number" min={1} value={draft.idEvento} onChange={(e) => set("idEvento", e.target.value)} required />
          <Field label="Estadio" value={draft.estadio} onChange={(e) => set("estadio", e.target.value)} required />
          <Field label="Sector" value={draft.sector} onChange={(e) => set("sector", e.target.value)} required />
          <Field label="Fila (opcional)" value={draft.fila} onChange={(e) => set("fila", e.target.value)} />
          <Field label="Asiento (opcional)" value={draft.asiento} onChange={(e) => set("asiento", e.target.value)} />
          <button type="submit" disabled={items.length >= MAX_ITEMS}>Agregar ítem</button>
        </form>
      </Card>

      <Card title={`Carrito (${items.length}/${MAX_ITEMS})`}>
        {items.length === 0 ? (
          <p className="muted">Sin ítems.</p>
        ) : (
          <table>
            <thead>
              <tr><th>Evento</th><th>Estadio</th><th>Sector</th><th>Fila</th><th>Asiento</th><th></th></tr>
            </thead>
            <tbody>
              {items.map((it, i) => (
                <tr key={i}>
                  <td>{it.idEvento}</td><td>{it.estadio}</td><td>{it.sector}</td>
                  <td>{it.fila ?? "-"}</td><td>{it.asiento ?? "-"}</td>
                  <td><button className="link" onClick={() => removeItem(i)}>quitar</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {err && <Banner kind="error">{err}</Banner>}
        {ok && <Banner kind="ok">{ok}</Banner>}
        <button onClick={comprar} disabled={busy || items.length === 0}>
          {busy ? "Comprando…" : "Confirmar compra"}
        </button>
      </Card>
    </div>
  );
}
