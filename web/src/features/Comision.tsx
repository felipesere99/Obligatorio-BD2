import { useState } from "react";
import { ApiError, api } from "../lib/api";
import type { Comision } from "../lib/types";
import { Badge, Banner, Card, EmptyState, Field, Loading, errorMessage, useAsync } from "../components/ui";

export function ComisionPanel() {
  // GET /comisiones/vigente da 404 si no hay ninguna: lo tratamos como "sin comisión".
  const { data, loading, error, reload } = useAsync<Comision | null>(async () => {
    try {
      return await api.get<Comision>("/comisiones/vigente");
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }
  });

  const [pct, setPct] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.post("/comisiones", { porcentaje: Number(pct) });
      setPct("");
      reload();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="stack">
      <Card title="Comisión vigente">
        {loading && <Loading />}
        {error && <Banner kind="error">{error}</Banner>}
        {!loading && !error && (
          data ? (
            <p style={{ display: "flex", alignItems: "center", gap: "0.6rem", margin: 0 }}>
              <Badge tone="ok">{data.porcentaje}% vigente</Badge>
              <span className="muted small">
                id {data.idComision} · desde {new Date(data.vigenteDesde).toLocaleString("es-UY")}
              </span>
            </p>
          ) : (
            <EmptyState icon="%">No hay una comisión vigente.</EmptyState>
          )
        )}
      </Card>

      <Card title="Setear comisión">
        <form onSubmit={submit} className="inline-form">
          <Field label="Porcentaje" type="number" min={0} step="0.01" value={pct} onChange={(e) => setPct(e.target.value)} placeholder="ej. 7.00" required />
          <button type="submit" disabled={busy}>{busy ? "…" : "Setear"}</button>
        </form>
        {err && <Banner kind="error">{err}</Banner>}
      </Card>
    </div>
  );
}
