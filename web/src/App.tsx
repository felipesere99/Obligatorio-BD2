import { useState } from "react";
import { useSession } from "./lib/session";
import type { Rol } from "./lib/types";
import { Login } from "./features/Login";
import { Registro } from "./features/Registro";
import { Usuarios } from "./features/Usuarios";
import { Funcionarios } from "./features/Funcionarios";
import { Equipos } from "./features/Equipos";
import { Estadios } from "./features/Estadios";
import { Eventos } from "./features/Eventos";
import { ComisionPanel } from "./features/Comision";
import { Comprar } from "./features/Comprar";
import { MisCompras } from "./features/MisCompras";
import { Asignaciones } from "./features/Asignaciones";
import { Dispositivos } from "./features/Dispositivos";
import { Reportes } from "./features/Reportes";
import { Validar } from "./features/Validar";
import { MisEntradas } from "./features/MisEntradas";
import { Transferencias } from "./features/Transferencias";

interface Tab {
  id: string;
  label: string;
  render: () => JSX.Element;
}

const TABS: Record<Rol, Tab[]> = {
  administrador: [
    { id: "usuarios", label: "Usuarios", render: () => <Usuarios /> },
    { id: "funcionarios", label: "Funcionarios", render: () => <Funcionarios /> },
    { id: "equipos", label: "Equipos", render: () => <Equipos /> },
    { id: "estadios", label: "Estadios", render: () => <Estadios /> },
    { id: "eventos", label: "Eventos", render: () => <Eventos /> },
    { id: "comision", label: "Comisión", render: () => <ComisionPanel /> },
    { id: "asignaciones", label: "Asignaciones", render: () => <Asignaciones /> },
    { id: "dispositivos", label: "Dispositivos", render: () => <Dispositivos /> },
    { id: "reportes", label: "Reportes", render: () => <Reportes /> },
  ],
  usuario_general: [
    { id: "comprar", label: "Comprar entradas", render: () => <Comprar /> },
    { id: "compras", label: "Mis compras", render: () => <MisCompras /> },
    { id: "entradas", label: "Mis entradas", render: () => <MisEntradas /> },
    { id: "transferencias", label: "Transferencias", render: () => <Transferencias /> },
  ],
  funcionario: [
    { id: "validar", label: "Validar ingreso", render: () => <Validar /> },
  ],
};

export default function App() {
  const { session, logout } = useSession();
  const [showRegistro, setShowRegistro] = useState(false);

  if (!session) {
    return showRegistro ? (
      <Registro onBack={() => setShowRegistro(false)} />
    ) : (
      <Login onRegister={() => setShowRegistro(true)} />
    );
  }

  return <Dashboard rol={session.rol} nombre={session.nombre} onLogout={logout} />;
}

function Dashboard({ rol, nombre, onLogout }: { rol: Rol; nombre: string; onLogout: () => void }) {
  const tabs = TABS[rol];
  const [active, setActive] = useState(tabs[0]?.id ?? "");
  const current = tabs.find((t) => t.id === active);

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <img className="brand-logo" src="/logo.png" alt="" aria-hidden="true" />
          Ticketing
        </div>
        <nav className="tabs" aria-label="Secciones">
          {tabs.map((t) => (
            <button
              key={t.id}
              className={t.id === active ? "tab active" : "tab"}
              aria-current={t.id === active ? "page" : undefined}
              onClick={() => setActive(t.id)}
            >
              {t.label}
            </button>
          ))}
        </nav>
        <div className="user">
          <span className="muted small">{nombre} · {rol}</span>
          <button className="secondary" onClick={onLogout}>Salir</button>
        </div>
      </header>

      <main className="content">
        {tabs.length === 0 ? (
          <p className="muted">No hay funcionalidades disponibles para tu rol todavía.</p>
        ) : (
          current?.render()
        )}
      </main>
    </div>
  );
}
