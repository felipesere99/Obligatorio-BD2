import { api } from "../lib/api";
import type { ReporteComprador, ReporteEventoVentas, ReporteSectorVentas } from "../lib/types";
import { Banner, Card, Loading, useAsync } from "../components/ui";

const money = new Intl.NumberFormat("es-UY", {
  style: "currency",
  currency: "UYU",
});

export function Reportes() {
  const eventos = useAsync(() => api.get<ReporteEventoVentas[]>("/reportes/eventos"));
  const sectores = useAsync(() => api.get<ReporteSectorVentas[]>("/reportes/sectores"));
  const compradores = useAsync(() => api.get<ReporteComprador[]>("/reportes/compradores"));

  return (
    <div className="stack">
      <Card title="Top eventos por ventas">
        {eventos.loading && <Loading />}
        {eventos.error && <Banner kind="error">{eventos.error}</Banner>}
        {eventos.data && eventos.data.length === 0 && <p className="muted">No hay ventas registradas.</p>}
        {eventos.data && eventos.data.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Estadio</th>
                <th>Entradas</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {eventos.data.map((r) => (
                <tr key={r.idEvento}>
                  <td>#{r.idEvento} — {r.nombreEvento}</td>
                  <td>{r.nombreEstadio}</td>
                  <td>{r.cantidadEntradas}</td>
                  <td>{money.format(r.totalVentas)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      <Card title="Top sectores por ventas">
        {sectores.loading && <Loading />}
        {sectores.error && <Banner kind="error">{sectores.error}</Banner>}
        {sectores.data && sectores.data.length === 0 && <p className="muted">No hay sectores habilitados.</p>}
        {sectores.data && sectores.data.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Evento</th>
                <th>Sector</th>
                <th>Entradas</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {sectores.data.map((r) => (
                <tr key={`${r.idEvento}-${r.nombreEstadio}-${r.nombreSector}`}>
                  <td>#{r.idEvento} — {r.nombreEvento}</td>
                  <td>{r.nombreEstadio} / {r.nombreSector}</td>
                  <td>{r.cantidadEntradas}</td>
                  <td>{money.format(r.totalVentas)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      <Card title="Ranking de compradores">
        {compradores.loading && <Loading />}
        {compradores.error && <Banner kind="error">{compradores.error}</Banner>}
        {compradores.data && compradores.data.length === 0 && <p className="muted">No hay compradores.</p>}
        {compradores.data && compradores.data.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Comprador</th>
                <th>Compras</th>
                <th>Entradas</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {compradores.data.map((r) => (
                <tr key={r.documento}>
                  <td>
                    {r.nombre} {r.apellido}
                    <br />
                    <span className="muted small">{r.documento}</span>
                  </td>
                  <td>{r.cantidadCompras}</td>
                  <td>{r.cantidadEntradas}</td>
                  <td>{money.format(r.totalGastado)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
