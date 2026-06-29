import { useState } from "react";
import { api } from "../lib/api";
import type { UsuarioGeneral } from "../lib/types";
import { Badge, Banner, Card, EmptyState, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

export function Usuarios() {
  const { data, loading, error, reload } = useAsync(() => api.get<UsuarioGeneral[]>("/usuarios/generales"));
  const toast = useToast();
  const [busy, setBusy] = useState<string | null>(null);

  async function verificar(documento: string) {
    setBusy(documento);
    try {
      await api.patch(`/usuarios/generales/${encodeURIComponent(documento)}/verificacion`);
      toast.success(`Usuario ${documento} verificado.`);
      reload();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  return (
    <Card title="Usuarios generales">
      {loading && <Skeleton rows={4} />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && <EmptyState icon="👤">No hay usuarios registrados.</EmptyState>}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Documento</th><th>Nombre</th><th>Correo</th><th>Verificado</th><th>Acciones</th></tr>
            </thead>
            <tbody>
              {data.map((u) => (
                <tr key={u.documento}>
                  <td>{u.documento}</td>
                  <td>{u.nombre} {u.apellido}</td>
                  <td>{u.correo}</td>
                  <td>
                    <Badge tone={u.estadoVerificacion ? "ok" : "neutral"}>
                      {u.estadoVerificacion ? "verificado" : "pendiente"}
                    </Badge>
                  </td>
                  <td>
                    {!u.estadoVerificacion ? (
                      <button
                        className="secondary"
                        type="button"
                        onClick={() => verificar(u.documento)}
                        disabled={busy !== null}
                      >
                        {busy === u.documento ? "…" : "Verificar"}
                      </button>
                    ) : (
                      <span className="muted small">Sin acciones</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
