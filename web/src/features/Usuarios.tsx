import { api } from "../lib/api";
import type { UsuarioGeneral } from "../lib/types";
import { Badge, Banner, Card, EmptyState, Skeleton, useAsync } from "../components/ui";

export function Usuarios() {
  const { data, loading, error } = useAsync(() => api.get<UsuarioGeneral[]>("/usuarios/generales"));

  return (
    <Card title="Usuarios generales">
      {loading && <Skeleton rows={4} />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && <EmptyState icon="👤">No hay usuarios registrados.</EmptyState>}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Documento</th><th>Nombre</th><th>Correo</th><th>Verificado</th></tr>
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
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
