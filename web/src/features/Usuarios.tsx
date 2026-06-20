import { api } from "../lib/api";
import type { UsuarioGeneral } from "../lib/types";
import { Banner, Card, Loading, useAsync } from "../components/ui";

export function Usuarios() {
  const { data, loading, error } = useAsync(() => api.get<UsuarioGeneral[]>("/usuarios/generales"));

  return (
    <Card title="Usuarios generales">
      {loading && <Loading />}
      {error && <Banner kind="error">{error}</Banner>}
      {data && data.length === 0 && <p className="muted">No hay usuarios.</p>}
      {data && data.length > 0 && (
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
                <td>{u.estadoVerificacion ? "sí" : "no"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  );
}
