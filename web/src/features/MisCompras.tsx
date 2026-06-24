import { Fragment, useState } from "react";
import { api } from "../lib/api";
import { useSession } from "../lib/session";
import type { Compra, Entrada } from "../lib/types";
import { Banner, Card, Loading, errorMessage, useAsync } from "../components/ui";
import { EntradaQr } from "./EntradaQr";

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
      {loading && <Loading />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && <p className="muted">No tenés compras.</p>}
      {data && data.length > 0 && (
        <table>
          <thead>
            <tr><th>Venta</th><th>Total</th><th>Estado</th><th>Fecha</th><th>Entradas</th><th></th></tr>
          </thead>
          <tbody>
            {data.map((c) => (
              <Fragment key={c.nroVenta}>
                <tr>
                  <td>#{c.nroVenta}</td>
                  <td>${c.montoTotal}</td>
                  <td>{c.estado}</td>
                  <td>{new Date(c.fecha).toLocaleString("es-UY", { dateStyle: "short", timeStyle: "short" })}</td>
                  <td>{c.cantidadEntradas}</td>
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
      )}
    </Card>
  );
}
