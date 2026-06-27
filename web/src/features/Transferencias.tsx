import { useState } from "react";
import { api } from "../lib/api";
import { useSession } from "../lib/session";
import type { Transferencia } from "../lib/types";
import {
  Badge,
  Banner,
  Card,
  EmptyState,
  Skeleton,
  errorMessage,
  useAsync,
  useToast,
  type BadgeTone,
} from "../components/ui";

const ESTADO_TONE: Record<string, BadgeTone> = {
  pendiente: "warn",
  aceptada: "ok",
  rechazada: "err",
  cancelada: "neutral",
};

export function Transferencias() {
  const { session } = useSession();
  const toast = useToast();
  const doc = session!.documento;
  const { data, loading, error, reload } = useAsync(
    () => api.get<Transferencia[]>(`/usuarios/${encodeURIComponent(doc)}/transferencias`),
    [doc],
  );

  const [busy, setBusy] = useState<string | null>(null); // "nro-accion"

  async function resolver(nroEntrada: number, accion: string) {
    setBusy(`${nroEntrada}-${accion}`);
    try {
      await api.patch(`/transferencias/${nroEntrada}`, { accion });
      const labels: Record<string, string> = { aceptar: "aceptada", rechazar: "rechazada", cancelar: "cancelada" };
      toast.success(`Transferencia de entrada #${nroEntrada} ${labels[accion] ?? accion}.`);
      reload();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  return (
    <Card title="Mis transferencias" subtitle="Transferencias enviadas y recibidas.">
      {loading && <Skeleton rows={3} />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && (
        <EmptyState icon="🔁">No tenés transferencias.</EmptyState>
      )}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Entrada</th>
                <th>Rol</th>
                <th>Contraparte</th>
                <th className="num">#</th>
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
                    <td>{esEmisor ? `→ ${t.docReceptor}` : `← ${t.docEmisor}`}</td>
                    <td className="num">{t.contador}/3</td>
                    <td>
                      {new Date(t.fechaHora).toLocaleString("es-UY", {
                        dateStyle: "short",
                        timeStyle: "short",
                      })}
                    </td>
                    <td><Badge tone={ESTADO_TONE[t.estado] ?? "neutral"}>{t.estado}</Badge></td>
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
        </div>
      )}
    </Card>
  );
}
