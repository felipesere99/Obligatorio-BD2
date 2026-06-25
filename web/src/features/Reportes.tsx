import type { ReactNode } from "react";
import { api } from "../lib/api";
import type { ReporteComprador, ReporteEventoVentas, ReporteSectorVentas } from "../lib/types";
import { Banner, Card, EmptyState, Skeleton, useAsync } from "../components/ui";

const money = new Intl.NumberFormat("es-UY", {
  style: "currency",
  currency: "UYU",
});

/** Tabla de reporte con estados de carga/error/vacío unificados. */
function ReportTable<T>({
  loading,
  error,
  rows,
  emptyIcon,
  emptyText,
  head,
  children,
}: {
  loading: boolean;
  error: string | null;
  rows: T[] | null;
  emptyIcon: string;
  emptyText: string;
  head: ReactNode;
  children: (rows: T[]) => ReactNode;
}) {
  if (loading) return <Skeleton rows={3} />;
  if (error) return <Banner kind="error">{error}</Banner>;
  if (rows && rows.length === 0) return <EmptyState icon={emptyIcon}>{emptyText}</EmptyState>;
  if (!rows) return null;
  return (
    <div className="table-wrap">
      <table>
        <thead>{head}</thead>
        <tbody>{children(rows)}</tbody>
      </table>
    </div>
  );
}

export function Reportes() {
  const eventos = useAsync(() => api.get<ReporteEventoVentas[]>("/reportes/eventos"));
  const sectores = useAsync(() => api.get<ReporteSectorVentas[]>("/reportes/sectores"));
  const compradores = useAsync(() => api.get<ReporteComprador[]>("/reportes/compradores"));

  return (
    <div className="stack">
      <Card title="Top eventos por ventas">
        <ReportTable
          loading={eventos.loading}
          error={eventos.error}
          rows={eventos.data}
          emptyIcon="📊"
          emptyText="No hay ventas registradas."
          head={
            <tr>
              <th>Evento</th>
              <th>Estadio</th>
              <th className="num">Entradas</th>
              <th className="num">Total</th>
            </tr>
          }
        >
          {(rows) =>
            rows.map((r) => (
              <tr key={r.idEvento}>
                <td>#{r.idEvento} — {r.nombreEvento}</td>
                <td>{r.nombreEstadio}</td>
                <td className="num">{r.cantidadEntradas}</td>
                <td className="num">{money.format(r.totalVentas)}</td>
              </tr>
            ))
          }
        </ReportTable>
      </Card>

      <Card title="Top sectores por ventas">
        <ReportTable
          loading={sectores.loading}
          error={sectores.error}
          rows={sectores.data}
          emptyIcon="📊"
          emptyText="No hay sectores habilitados."
          head={
            <tr>
              <th>Evento</th>
              <th>Sector</th>
              <th className="num">Entradas</th>
              <th className="num">Total</th>
            </tr>
          }
        >
          {(rows) =>
            rows.map((r) => (
              <tr key={`${r.idEvento}-${r.nombreEstadio}-${r.nombreSector}`}>
                <td>#{r.idEvento} — {r.nombreEvento}</td>
                <td>{r.nombreEstadio} / {r.nombreSector}</td>
                <td className="num">{r.cantidadEntradas}</td>
                <td className="num">{money.format(r.totalVentas)}</td>
              </tr>
            ))
          }
        </ReportTable>
      </Card>

      <Card title="Ranking de compradores">
        <ReportTable
          loading={compradores.loading}
          error={compradores.error}
          rows={compradores.data}
          emptyIcon="🏆"
          emptyText="No hay compradores."
          head={
            <tr>
              <th>Comprador</th>
              <th className="num">Compras</th>
              <th className="num">Entradas</th>
              <th className="num">Total</th>
            </tr>
          }
        >
          {(rows) =>
            rows.map((r) => (
              <tr key={r.documento}>
                <td>
                  {r.nombre} {r.apellido}
                  <br />
                  <span className="muted small">{r.documento}</span>
                </td>
                <td className="num">{r.cantidadCompras}</td>
                <td className="num">{r.cantidadEntradas}</td>
                <td className="num">{money.format(r.totalGastado)}</td>
              </tr>
            ))
          }
        </ReportTable>
      </Card>
    </div>
  );
}
