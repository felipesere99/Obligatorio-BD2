import { useEffect, useState } from "react";
import { api } from "../lib/api";
import type { Dispositivo, Funcionario } from "../lib/types";
import { Banner, Card, Field, Loading, errorMessage, useAsync } from "../components/ui";

interface DispositivoForm {
  nroSerie: string;
  marca: string;
  modelo: string;
  habilitado: boolean;
}

const EMPTY_FORM: DispositivoForm = {
  nroSerie: "",
  marca: "",
  modelo: "",
  habilitado: true,
};

export function Dispositivos() {
  const dispositivos = useAsync(() => api.get<Dispositivo[]>("/dispositivos"));
  const funcionarios = useAsync(() => api.get<Funcionario[]>("/usuarios/funcionarios"));

  function reload() {
    dispositivos.reload();
  }

  return (
    <div className="stack">
      <CrearDispositivo onDone={reload} />
      <AsignarDispositivo
        dispositivos={dispositivos.data ?? []}
        funcionarios={funcionarios.data ?? []}
        onDone={reload}
      />

      <Card title="Dispositivos">
        {(dispositivos.loading || funcionarios.loading) && <Loading />}
        {dispositivos.error && <Banner kind="error">{dispositivos.error}</Banner>}
        {funcionarios.error && <Banner kind="error">{funcionarios.error}</Banner>}
        {dispositivos.data && dispositivos.data.length === 0 && <p className="muted">No hay dispositivos.</p>}
        {dispositivos.data && dispositivos.data.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Serie</th>
                <th>Marca / modelo</th>
                <th>Estado</th>
                <th>Funcionarios asignados</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {dispositivos.data.map((d) => (
                <DispositivoRow key={d.idDispositivo} dispositivo={d} onDone={reload} />
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}

function CrearDispositivo({ onDone }: { onDone: () => void }) {
  const [form, setForm] = useState<DispositivoForm>(EMPTY_FORM);
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setOk(null);
    setBusy(true);
    try {
      const creado = await api.post<{ idDispositivo?: number; nroSerie?: string }>("/dispositivos", normalizeForm(form));
      setForm(EMPTY_FORM);
      setOk(`Dispositivo #${creado.idDispositivo ?? "nuevo"} (${creado.nroSerie ?? form.nroSerie.trim()}) creado.`);
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Registrar dispositivo">
      <DispositivoFormFields form={form} onChange={setForm} onSubmit={submit} busy={busy} submitLabel="Registrar" />
      {err && <Banner kind="error">{err}</Banner>}
      {ok && <Banner kind="ok">{ok}</Banner>}
    </Card>
  );
}

function DispositivoFormFields({
  form,
  onChange,
  onSubmit,
  busy,
  submitLabel,
}: {
  form: DispositivoForm;
  onChange: (form: DispositivoForm) => void;
  onSubmit: (e: React.FormEvent) => void;
  busy: boolean;
  submitLabel: string;
}) {
  return (
    <form onSubmit={onSubmit} className="grid-form">
      <Field
        label="Numero de serie"
        value={form.nroSerie}
        onChange={(e) => onChange({ ...form, nroSerie: e.target.value })}
        required
      />
      <Field
        label="Marca"
        value={form.marca}
        onChange={(e) => onChange({ ...form, marca: e.target.value })}
        required
      />
      <Field
        label="Modelo"
        value={form.modelo}
        onChange={(e) => onChange({ ...form, modelo: e.target.value })}
        required
      />
      <label className="check-field">
        <input
          type="checkbox"
          checked={form.habilitado}
          onChange={(e) => onChange({ ...form, habilitado: e.target.checked })}
        />
        <span>Habilitado para validar entradas</span>
      </label>
      <button type="submit" disabled={busy}>
        {busy ? "..." : submitLabel}
      </button>
    </form>
  );
}

function AsignarDispositivo({
  dispositivos,
  funcionarios,
  onDone,
}: {
  dispositivos: Dispositivo[];
  funcionarios: Funcionario[];
  onDone: () => void;
}) {
  const [docFuncionario, setDocFuncionario] = useState("");
  const [idDispositivo, setIdDispositivo] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setOk(null);
    setBusy(true);
    try {
      await api.post(`/funcionarios/${encodeURIComponent(docFuncionario)}/dispositivos`, {
        idDispositivo: Number(idDispositivo),
      });
      setDocFuncionario("");
      setIdDispositivo("");
      setOk("Dispositivo asignado.");
      onDone();
    } catch (e2) {
      setErr(errorMessage(e2));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title="Asignar dispositivo a funcionario">
      <form onSubmit={submit} className="grid-form">
        <label className="field">
          <span>Funcionario</span>
          <select value={docFuncionario} onChange={(e) => setDocFuncionario(e.target.value)} required>
            <option value="">-- elegir --</option>
            {funcionarios.map((f) => (
              <option key={f.documento} value={f.documento}>
                {f.nombre} {f.apellido} ({f.documento})
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Dispositivo</span>
          <select value={idDispositivo} onChange={(e) => setIdDispositivo(e.target.value)} required>
            <option value="">-- elegir --</option>
            {dispositivos.map((d) => (
              <option key={d.idDispositivo} value={d.idDispositivo}>
                #{d.idDispositivo} - {deviceLabel(d)}
              </option>
            ))}
          </select>
        </label>
        <button type="submit" disabled={busy || !docFuncionario || !idDispositivo}>
          {busy ? "..." : "Asignar"}
        </button>
      </form>
      {err && <Banner kind="error">{err}</Banner>}
      {ok && <Banner kind="ok">{ok}</Banner>}
    </Card>
  );
}

function DispositivoRow({ dispositivo, onDone }: { dispositivo: Dispositivo; onDone: () => void }) {
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<DispositivoForm>({
    nroSerie: dispositivo.nroSerie,
    marca: dispositivo.marca,
    modelo: dispositivo.modelo,
    habilitado: dispositivo.habilitado,
  });

  useEffect(() => {
    if (!editing) {
      setForm({
        nroSerie: dispositivo.nroSerie,
        marca: dispositivo.marca,
        modelo: dispositivo.modelo,
        habilitado: dispositivo.habilitado,
      });
    }
  }, [dispositivo, editing]);

  async function guardar(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setBusy(true);
    try {
      await api.put(`/dispositivos/${dispositivo.idDispositivo}`, normalizeForm(form));
      setEditing(false);
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
      await api.delete(`/dispositivos/${dispositivo.idDispositivo}`);
      onDone();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function quitar(docFuncionario: string) {
    setErr(null);
    setBusy(true);
    try {
      await api.delete(
        `/funcionarios/${encodeURIComponent(docFuncionario)}/dispositivos/${dispositivo.idDispositivo}`,
      );
      onDone();
    } catch (e) {
      setErr(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  if (editing) {
    return (
      <tr>
        <td>#{dispositivo.idDispositivo}</td>
        <td colSpan={5}>
          <DispositivoFormFields form={form} onChange={setForm} onSubmit={guardar} busy={busy} submitLabel="Guardar" />
          <div className="row">
            <button className="secondary" type="button" onClick={() => setEditing(false)} disabled={busy}>Cancelar</button>
          </div>
          {err && <Banner kind="error">{err}</Banner>}
        </td>
      </tr>
    );
  }

  return (
    <tr>
      <td>#{dispositivo.idDispositivo}</td>
      <td>{dispositivo.nroSerie || "Sin serie"}</td>
      <td>{deviceLabel(dispositivo)}</td>
      <td>{dispositivo.habilitado ? "Habilitado" : "Deshabilitado"}</td>
      <td>
        {dispositivo.funcionariosAsignados.length === 0 ? (
          <span className="muted">Sin asignar</span>
        ) : (
          <div className="chip-list">
            {dispositivo.funcionariosAsignados.map((doc) => (
              <span className="chip" key={doc}>
                {doc}
                <button type="button" className="chip-button" onClick={() => quitar(doc)} disabled={busy}>
                  x
                </button>
              </span>
            ))}
          </div>
        )}
        {err && <Banner kind="error">{err}</Banner>}
      </td>
      <td>
        <div className="row">
          <button className="secondary" type="button" onClick={() => setEditing(true)} disabled={busy}>Editar</button>
          <button className="secondary" type="button" onClick={eliminar} disabled={busy}>
            {busy ? "..." : "Eliminar"}
          </button>
        </div>
      </td>
    </tr>
  );
}

function normalizeForm(form: DispositivoForm) {
  return {
    nroSerie: form.nroSerie.trim(),
    marca: form.marca.trim(),
    modelo: form.modelo.trim(),
    habilitado: form.habilitado,
  };
}

function deviceLabel(dispositivo: Dispositivo) {
  const marcaModelo = [dispositivo.marca, dispositivo.modelo].filter(Boolean).join(" ");
  const serie = dispositivo.nroSerie ? `(${dispositivo.nroSerie})` : "";
  return [marcaModelo || "Sin marca/modelo", serie].filter(Boolean).join(" ");
}
