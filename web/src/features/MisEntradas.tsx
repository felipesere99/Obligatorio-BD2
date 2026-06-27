import { Fragment, useState } from "react";
import { api } from "../lib/api";
import { useSession } from "../lib/session";
import type { EntradaTenencia, Transferencia } from "../lib/types";
import {
  Banner,
  Card,
  EmptyState,
  Field,
  Skeleton,
  errorMessage,
  useAsync,
  useToast,
} from "../components/ui";

export function MisEntradas() {
  const { session } = useSession();
  const toast = useToast();
  const doc = session!.documento;
  const { data, loading, error, reload } = useAsync(
    () => api.get<EntradaTenencia[]>(`/usuarios/${encodeURIComponent(doc)}/entradas`),
    [doc],
  );

  const [transfEntrada, setTransfEntrada] = useState<number | null>(null);
  const [docReceptor, setDocReceptor] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  function openTransf(nro: number) {
    setTransfEntrada(nro);
    setDocReceptor("");
    setErr(null);
  }

  function cancelTransf() {
    setTransfEntrada(null);
    setDocReceptor("");
    setErr(null);
  }

  async function transferir(e: React.FormEvent) {
    e.preventDefault();
    if (!transfEntrada) return;
    setBusy(true);
    setErr(null);
    try {
      await api.post<Transferencia>("/transferencias", {
        nroEntrada: transfEntrada,
        docReceptor: docReceptor.trim(),
      });
      toast.success(`Transferencia iniciada para la entrada #${transfEntrada}. El receptor debe aceptarla.`);
      setTransfEntrada(null);
      setDocReceptor("");
      reload();
    } catch (ex) {
      setErr(errorMessage(ex));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Mis entradas" subtitle="Entradas a tu nombre. Podés transferirlas a otro usuario.">
      {loading && <Skeleton rows={3} />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && (
        <EmptyState icon="🎫">Todavía no tenés entradas.</EmptyState>
      )}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Evento</th>
                <th>Estadio / Sector</th>
                <th>Fila</th>
                <th>Asiento</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.map((e) => (
                <Fragment key={e.nroEntrada}>
                  <tr>
                    <td>#{e.nroEntrada}</td>
                    <td>#{e.idEvento}</td>
                    <td>{e.nombreEstadio} / {e.nombreSector}</td>
                    <td>{e.fila ?? "—"}</td>
                    <td>{e.asiento ?? "—"}</td>
                    <td>
                      <button
                        className="link"
                        onClick={() =>
                          transfEntrada === e.nroEntrada ? cancelTransf() : openTransf(e.nroEntrada)
                        }
                      >
                        {transfEntrada === e.nroEntrada ? "cancelar" : "transferir"}
                      </button>
                    </td>
                  </tr>
                  {transfEntrada === e.nroEntrada && (
                    <tr>
                      <td colSpan={6}>
                        <form onSubmit={transferir} className="grid-form">
                          <Field
                            label="Documento del receptor"
                            value={docReceptor}
                            onChange={(ev) => setDocReceptor(ev.target.value)}
                            placeholder="ej. 12345678"
                            required
                            autoFocus
                          />
                          {err && <Banner kind="error">{err}</Banner>}
                          <button type="submit" disabled={busy || !docReceptor.trim()}>
                            {busy ? "Enviando…" : "Iniciar transferencia"}
                          </button>
                        </form>
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
