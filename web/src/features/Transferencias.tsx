import { useState } from "react";
import { api } from "../lib/api";
import { useSession } from "../lib/session";
import type { Transferencia } from "../lib/types";
import { Banner, Card, Loading, errorMessage, useAsync } from "../components/ui";

const ESTADO_LABEL: Record<string, string> = {
  pendiente: "pendiente",
  aceptada: "aceptada",
  rechazada: "rechazada",
  cancelada: "cancelada",
};

export function Transferencias() {
  const { session } = useSession();
  const doc = session!.documento;
  const { data, loading, error, reload } = useAsync(
    () => api.get<Transferencia[]>(`/usuarios/${encodeURIComponent(doc)}/transferencias`),
    [doc],
  );

  const [busy, setBusy] = useState<string | null>(null); // "nro-accion"
  const [actionErr, setActionErr] = useState<string | null>(null);
  const [actionOk, setActionOk] = useState<string | null>(null);

  async function resolver(nroEntrada: number, accion: string) {
    setBusy(`${nroEntrada}-${accion}`);
    setActionErr(null);
    setActionOk(null);
    try {
      await api.patch(`/transferencias/${nroEntrada}`, { accion });
      const labels: Record<string, string> = { aceptar: "aceptada", rechazar: "rechazada", cancelar: "cancelada" };
      setActionOk(`Transferencia de entrada #${nroEntrada} ${labels[accion] ?? accion}.`);
      reload();
    } catch (e) {
      setActionErr(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  return (
    <Card title="Mis transferencias">
      {loading && <Loading />}
      {error && <Banner kind="error">{error}</Banner>}
      {actionOk && <Banner kind="ok">{actionOk}</Banner>}
      {actionErr && <Banner kind="error">{actionErr}</Banner>}
      {data && data.length === 0 && <p className="muted">No tenés transferencias.</p>}
      {data && data.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Entrada</th>
              <th>Rol</th>
              <th>Contrapartes</th>
              <th>#</th>
              <th>Fecha</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data.map((t) => {
              const esEmisor = t.docEmisor === doc;
              const esPendiente = t.estado === "pendiente";
              const isBusy = (accion: string) => busy === `${t.nroEntrada}-${accion}`;

              return (
                <tr key={`${t.nroEntrada}-${t.fechaHora}`}>
                  <td>#{t.nroEntrada}</td>
                  <td>{esEmisor ? "enviaste" : "recibiste"}</td>
                  <td>
                    {esEmisor ? `→ ${t.docReceptor}` : `← ${t.docEmisor}`}
                  </td>
                  <td>{t.contador}/3</td>
                  <td>
                    {new Date(t.fechaHora).toLocaleString("es-UY", {
                      dateStyle: "short",
                      timeStyle: "short",
                    })}
                  </td>
                  <td>{ESTADO_LABEL[t.estado] ?? t.estado}</td>
                  <td>
                    {esPendiente && !esEmisor && (
                      <>
                        <button
                          className="link"
                          disabled={busy !== null}
                          onClick={() => resolver(t.nroEntrada, "aceptar")}
                        >
                          {isBusy("aceptar") ? "…" : "aceptar"}
                        </button>
                        {" · "}
                        <button
                          className="link"
                          disabled={busy !== null}
                          onClick={() => resolver(t.nroEntrada, "rechazar")}
                        >
                          {isBusy("rechazar") ? "…" : "rechazar"}
                        </button>
                      </>
                    )}
                    {esPendiente && esEmisor && (
                      <button
                        className="link"
                        disabled={busy !== null}
                        onClick={() => resolver(t.nroEntrada, "cancelar")}
                      >
                        {isBusy("cancelar") ? "…" : "cancelar"}
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </Card>
  );
}
