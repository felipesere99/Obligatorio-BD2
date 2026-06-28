import { Fragment, useState } from "react";
import { api } from "../lib/api";
import { useSession } from "../lib/session";
import type { Compra, Entrada } from "../lib/types";
import { Badge, Banner, Card, EmptyState, Loading, Skeleton, errorMessage, useAsync, type BadgeTone } from "../components/ui";
import { EntradaQr } from "./EntradaQr";

const ESTADO_TONE: Record<string, BadgeTone> = {
  paga: "ok",
  pagada: "ok",
  pagado: "ok",
  confirmada: "ok",
  pendiente: "warn",
  cancelada: "err",
  anulada: "err",
};

export function MisCompras() {
  const { session } = useSession();
  const doc = session!.documento;
  const { data, loading, error } = useAsync(
    () => api.get<Compra[]>(`/usuarios/${encodeURIComponent(doc)}/compras`),
    [doc],
  );

  const [sel, setSel] = useState<number | null>(null);
  const [entradas, setEntradas] = useState<Entrada[] | null>(null);
  const [entErr, setEntErr] = useState<string | null>(null);
  const [qrEntrada, setQrEntrada] = useState<number | null>(null);

  async function verEntradas(nro: number) {
    if (sel === nro) {
      setSel(null);
      setEntradas(null);
      return;
    }
    setSel(nro);
    setEntradas(null);
    setEntErr(null);
    try {
      setEntradas(await api.get<Entrada[]>(`/ventas/${nro}/entradas`));
    } catch (e) {
      setEntErr(errorMessage(e));
    }
  }

  return (
    <Card title="Mis compras">
      {loading && <Skeleton rows={3} />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && <EmptyState icon="🧾">Todavía no realizaste compras.</EmptyState>}
      {data && data.length > 0 && (
        <div className="table-wrap">
        <table>
          <thead>
            <tr><th>Venta</th><th className="num">Total</th><th>Estado</th><th>Fecha</th><th className="num">Entradas</th><th></th></tr>
          </thead>
          <tbody>
            {data.map((c) => (
              <Fragment key={c.nroVenta}>
                <tr>
                  <td>#{c.nroVenta}</td>
                  <td className="num">${c.montoTotal}</td>
                  <td><Badge tone={ESTADO_TONE[c.estado?.toLowerCase()] ?? "neutral"}>{c.estado}</Badge></td>
                  <td>{new Date(c.fecha).toLocaleString("es-UY", { dateStyle: "short", timeStyle: "short" })}</td>
                  <td className="num">{c.cantidadEntradas}</td>
                  <td><button className="link" onClick={() => verEntradas(c.nroVenta)}>{sel === c.nroVenta ? "ocultar" : "ver entradas"}</button></td>
                </tr>
                {sel === c.nroVenta && (
                  <tr>
                    <td colSpan={6}>
                      {entErr && <Banner kind="error">{entErr}</Banner>}
                      {!entErr && !entradas && <Loading />}
                      {entradas && (
                        <ul className="entradas">
                          {entradas.map((e) => (
                            <li key={e.nroEntrada}>
                              #{e.nroEntrada} · evento {e.idEvento} · {e.nombreEstadio}/{e.nombreSector}
                              {(e.fila || e.asiento) && ` (fila ${e.fila ?? "-"}, asiento ${e.asiento ?? "-"})`}
                              {" "}
                              <button
                                className="link"
                                onClick={() =>
                                  setQrEntrada((prev) => (prev === e.nroEntrada ? null : e.nroEntrada))
                                }
                              >
                                {qrEntrada === e.nroEntrada ? "ocultar QR" : "mostrar QR"}
                              </button>
                              {qrEntrada === e.nroEntrada && <EntradaQr nroEntrada={e.nroEntrada} />}
                            </li>
                          ))}
                        </ul>
                      )}
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
        </div>
      )}
    </Card>
  );
}
