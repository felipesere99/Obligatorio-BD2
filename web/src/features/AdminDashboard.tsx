import { api } from "../lib/api";
import type { CSSProperties } from "react";
import type {
  AdminDashboard as AdminDashboardData,
  AdminDashboardDistribucion,
  AdminDashboardEventoVentas,
  AdminDashboardFuncionariosEvento,
} from "../lib/types";
import { Banner, Card, EmptyState, Skeleton, useAsync } from "../components/ui";

const money = new Intl.NumberFormat("es-UY", {
  style: "currency",
  currency: "UYU",
  maximumFractionDigits: 0,
});

const number = new Intl.NumberFormat("es-UY");

function formatMoney(value: number | null | undefined) {
  const numericValue = typeof value === "number" ? value : 0;
  return money.format(Number.isFinite(numericValue) ? numericValue : 0);
}

export function AdminDashboard() {
  const dashboard = useAsync(() => api.get<AdminDashboardData>("/admin/dashboard"));

  if (dashboard.loading) {
    return (
      <div className="stack">
        <Skeleton rows={5} />
      </div>
    );
  }

  if (dashboard.error) {
    return <Banner kind="error">{dashboard.error}</Banner>;
  }

  if (!dashboard.data) {
    return <EmptyState>No hay datos para mostrar.</EmptyState>;
  }

  const { totales, ventasPorEvento, funcionariosPorEvento, dispositivos, usuarios } = dashboard.data;

  return (
    <div className="stack">
      <section className="metric-grid" aria-label="Resumen administrativo">
        <Metric label="Eventos totales" value={totales.eventosTotales} detail={`${totales.eventosProximos} vigentes o proximos`} />
        <Metric label="Entradas vendidas" value={totales.entradasVendidas} detail={`${totales.entradasValidadas} validadas`} />
        <Metric label="Ingresos" value={formatMoney(totales.ingresos)} detail="Ventas registradas" />
        <Metric label="Comisiones" value={formatMoney(totales.totalComisiones)} detail="Total aplicado" />
        <Metric label="Funcionarios" value={totales.funcionarios} detail={`${totales.funcionariosDisponibles} sin asignacion`} />
        <Metric label="Usuarios" value={totales.usuarios} detail="Usuarios generales" />
        <Metric label="Dispositivos" value={totales.dispositivosHabilitados} detail="Habilitados" />
      </section>

      <div className="dashboard-grid">
        <Card title="Ventas por evento">
          <BarList
            rows={ventasPorEvento}
            getKey={(row) => String(row.idEvento)}
            getLabel={(row) => row.nombreEvento}
            getValue={(row) => row.cantidadEntradas}
            getMeta={(row) => formatMoney(row.totalVentas)}
            emptyText="No hay ventas registradas."
          />
        </Card>

        <Card title="Funcionarios por evento">
          <BarList
            rows={funcionariosPorEvento}
            getKey={(row) => String(row.idEvento)}
            getLabel={(row) => row.nombreEvento}
            getValue={(row) => row.cantidadFuncionarios}
            getMeta={(row) => `${row.cantidadFuncionarios} asignados`}
            emptyText="No hay funcionarios asignados."
          />
        </Card>

        <Card title="Dispositivos">
          <DonutChart data={dispositivos} />
        </Card>

        <Card title="Usuarios">
          <DonutChart data={usuarios} />
        </Card>
      </div>
    </div>
  );
}

function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) {
  return (
    <article className="metric-card">
      <span className="metric-label">{label}</span>
      <strong>{typeof value === "number" ? number.format(value) : value}</strong>
      <span className="muted small">{detail}</span>
    </article>
  );
}

function BarList<T extends AdminDashboardEventoVentas | AdminDashboardFuncionariosEvento>({
  rows,
  getKey,
  getLabel,
  getValue,
  getMeta,
  emptyText,
}: {
  rows: T[];
  getKey: (row: T) => string;
  getLabel: (row: T) => string;
  getValue: (row: T) => number;
  getMeta: (row: T) => string;
  emptyText: string;
}) {
  if (rows.length === 0) return <EmptyState>{emptyText}</EmptyState>;

  const max = Math.max(...rows.map(getValue), 1);

  return (
    <div className="bar-list">
      {rows.map((row) => {
        const value = getValue(row);
        const width = Math.max((value / max) * 100, value > 0 ? 8 : 0);
        return (
          <div className="bar-row" key={getKey(row)}>
            <div className="bar-row-head">
              <span>{getLabel(row)}</span>
              <span className="muted small">{getMeta(row)}</span>
            </div>
            <div className="bar-track" aria-hidden="true">
              <div className="bar-fill" style={{ width: `${width}%` }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function DonutChart({ data }: { data: AdminDashboardDistribucion }) {
  const total = data.valorPrincipal + data.valorSecundario;
  const percent = total === 0 ? 0 : Math.round((data.valorPrincipal / total) * 100);

  return (
    <div className="donut-panel">
      <div className="donut" style={{ "--value": `${percent}%` } as CSSProperties}>
        <strong>{percent}%</strong>
      </div>
      <div className="donut-legend">
        <LegendItem label={data.etiquetaPrincipal} value={data.valorPrincipal} tone="primary" />
        <LegendItem label={data.etiquetaSecundaria} value={data.valorSecundario} tone="secondary" />
      </div>
    </div>
  );
}

function LegendItem({ label, value, tone }: { label: string; value: number; tone: "primary" | "secondary" }) {
  return (
    <div className="legend-item">
      <span className={`legend-dot legend-dot-${tone}`} />
      <span>{label}</span>
      <strong>{number.format(value)}</strong>
    </div>
  );
}
