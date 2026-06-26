import { useEffect, useState } from "react";
import { api } from "../lib/api";
import type { Funcionario } from "../lib/types";
import { Banner, Card, EmptyState, Field, Skeleton, errorMessage, useAsync, useToast } from "../components/ui";

interface FuncionarioForm {
  nombre: string;
  apellido: string;
  correo: string;
  nroLegajo: string;
}

interface CrearForm extends FuncionarioForm {
  documento: string;
  contrasenia: string;
}

const EMPTY_CREAR: CrearForm = {
  documento: "",
  nombre: "",
  apellido: "",
  correo: "",
  contrasenia: "",
  nroLegajo: "",
};

function formFromFuncionario(f: Funcionario): FuncionarioForm {
  return { nombre: f.nombre, apellido: f.apellido, correo: f.correo, nroLegajo: f.nroLegajo };
}

export function Funcionarios() {
  const funcionarios = useAsync(() => api.get<Funcionario[]>("/usuarios/funcionarios"));

  return (
    <div className="stack">
      <CrearFuncionario onDone={funcionarios.reload} />

      <Card title="Funcionarios registrados">
        {funcionarios.loading && <Skeleton rows={3} />}
        {funcionarios.error && <Banner kind="error">{funcionarios.error}</Banner>}
        {funcionarios.data && funcionarios.data.length === 0 && (
          <EmptyState icon="🪪">No hay funcionarios registrados.</EmptyState>
        )}
        {funcionarios.data && funcionarios.data.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Documento</th>
                  <th>Legajo</th>
                  <th>Nombre</th>
                  <th>Correo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {funcionarios.data.map((f) => (
                  <FuncionarioRow key={f.documento} funcionario={f} onDone={funcionarios.reload} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}

function CrearFuncionario({ onDone }: { onDone: () => void }) {
  const toast = useToast();
  const [form, setForm] = useState<CrearForm>(EMPTY_CREAR);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.post("/usuarios/funcionarios", {
        documento: form.documento.trim(),
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        correo: form.correo.trim(),
        contrasenia: form.contrasenia,
        nroLegajo: form.nroLegajo.trim(),
      });
      toast.success(`Funcionario ${form.documento.trim()} registrado.`);
      setForm(EMPTY_CREAR);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Registrar funcionario">
      <form onSubmit={submit} className="grid-form">
        <Field
          label="Documento"
          value={form.documento}
          onChange={(e) => setForm({ ...form, documento: e.target.value })}
          required
        />
        <Field
          label="Nombre"
          value={form.nombre}
          onChange={(e) => setForm({ ...form, nombre: e.target.value })}
          required
        />
        <Field
          label="Apellido"
          value={form.apellido}
          onChange={(e) => setForm({ ...form, apellido: e.target.value })}
          required
        />
        <Field
          label="Correo"
          type="email"
          value={form.correo}
          onChange={(e) => setForm({ ...form, correo: e.target.value })}
          required
        />
        <Field
          label="Contraseña (mín. 8 caracteres)"
          type="password"
          value={form.contrasenia}
          onChange={(e) => setForm({ ...form, contrasenia: e.target.value })}
          required
          minLength={8}
        />
        <Field
          label="Nro. de legajo"
          value={form.nroLegajo}
          onChange={(e) => setForm({ ...form, nroLegajo: e.target.value })}
          required
        />
        <button type="submit" disabled={busy}>
          {busy ? "..." : "Registrar"}
        </button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
    </Card>
  );
}

function FuncionarioRow({ funcionario, onDone }: { funcionario: Funcionario; onDone: () => void }) {
  const toast = useToast();
  const [editing, setEditing] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [form, setForm] = useState<FuncionarioForm>(formFromFuncionario(funcionario));
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!editing) setForm(formFromFuncionario(funcionario));
  }, [funcionario, editing]);

  async function guardar(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/usuarios/funcionarios/${encodeURIComponent(funcionario.documento)}`, {
        nombre: form.nombre.trim(),
        apellido: form.apellido.trim(),
        correo: form.correo.trim(),
        nroLegajo: form.nroLegajo.trim(),
      });
      setEditing(false);
      toast.success(`Funcionario ${funcionario.documento} actualizado.`);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  async function eliminar() {
    setErr(null);
    setBusy(true);
    try {
      await api.delete(`/usuarios/funcionarios/${encodeURIComponent(funcionario.documento)}`);
      toast.success(`Funcionario ${funcionario.documento} eliminado.`);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
      setConfirmDelete(false);
    } finally {
      setBusy(false);
    }
  }

  if (editing) {
    return (
      <tr>
        <td>{funcionario.documento}</td>
        <td colSpan={4}>
          <form onSubmit={guardar} className="grid-form">
            <Field
              label="Nombre"
              value={form.nombre}
              onChange={(e) => setForm({ ...form, nombre: e.target.value })}
              required
            />
            <Field
              label="Apellido"
              value={form.apellido}
              onChange={(e) => setForm({ ...form, apellido: e.target.value })}
              required
            />
            <Field
              label="Correo"
              type="email"
              value={form.correo}
              onChange={(e) => setForm({ ...form, correo: e.target.value })}
              required
            />
            <Field
              label="Nro. de legajo"
              value={form.nroLegajo}
              onChange={(e) => setForm({ ...form, nroLegajo: e.target.value })}
              required
            />
            <div className="row">
              <button type="submit" disabled={busy}>
                {busy ? "..." : "Guardar"}
              </button>
              <button className="secondary" type="button" onClick={() => setEditing(false)} disabled={busy}>
                Cancelar
              </button>
            </div>
          </form>
          {err && <Banner kind="error">{err}</Banner>}
        </td>
      </tr>
    );
  }

  return (
    <tr>
      <td>{funcionario.documento}</td>
      <td>{funcionario.nroLegajo}</td>
      <td>
        {funcionario.nombre} {funcionario.apellido}
      </td>
      <td>{funcionario.correo}</td>
      <td>
        {confirmDelete ? (
          <div className="row">
            <span className="muted small">¿Eliminar?</span>
            <button className="danger" type="button" onClick={eliminar} disabled={busy}>
              {busy ? "..." : "Confirmar"}
            </button>
            <button className="secondary" type="button" onClick={() => setConfirmDelete(false)} disabled={busy}>
              Cancelar
            </button>
          </div>
        ) : (
          <div className="row">
            <button className="secondary" type="button" onClick={() => setEditing(true)}>
              Editar
            </button>
            <button className="danger" type="button" onClick={() => setConfirmDelete(true)}>
              Eliminar
            </button>
          </div>
        )}
        {err && <Banner kind="error">{err}</Banner>}
      </td>
    </tr>
  );
}
