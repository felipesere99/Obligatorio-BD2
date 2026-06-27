import { useState } from "react";
import { api } from "../lib/api";
import type { RegistrarUsuarioRequest } from "../lib/types";
import { Banner, Field, errorMessage, useToast } from "../components/ui";

const EMPTY: RegistrarUsuarioRequest = {
  documento: "",
  nombre: "",
  apellido: "",
  correo: "",
  contrasenia: "",
  dirPais: "",
  dirLocalidad: "",
  dirCalle: "",
  dirNumero: "",
  dirCodigoPostal: "",
};

export function Registro({ onBack }: { onBack: () => void }) {
  const toast = useToast();
  const [form, setForm] = useState<RegistrarUsuarioRequest>(EMPTY);
  const [confirmacion, setConfirmacion] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function set<K extends keyof RegistrarUsuarioRequest>(key: K, value: string) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (form.contrasenia.length < 8) {
      setError("La contraseña debe tener al menos 8 caracteres.");
      return;
    }
    if (form.contrasenia !== confirmacion) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    setBusy(true);
    try {
      // Los campos de dirección vacíos viajan como null.
      const body: RegistrarUsuarioRequest = {
        ...form,
        dirPais: form.dirPais || null,
        dirLocalidad: form.dirLocalidad || null,
        dirCalle: form.dirCalle || null,
        dirNumero: form.dirNumero || null,
        dirCodigoPostal: form.dirCodigoPostal || null,
      };
      const res = await api.post<{ documento: string }>("/usuarios/generales", body);
      setForm(EMPTY);
      setConfirmacion("");
      toast.success(`Usuario ${res.documento} registrado. Ya podés ingresar con tu documento y contraseña.`);
      onBack();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth">
      <div className="card auth-card">
        <h1>Registro</h1>
        <form onSubmit={submit}>
          <Field label="Documento" value={form.documento} onChange={(e) => set("documento", e.target.value)} required />
          <Field label="Nombre" value={form.nombre} onChange={(e) => set("nombre", e.target.value)} required />
          <Field label="Apellido" value={form.apellido} onChange={(e) => set("apellido", e.target.value)} required />
          <Field label="Correo" type="email" value={form.correo} onChange={(e) => set("correo", e.target.value)} required />
          <Field
            label="Contraseña"
            type="password"
            value={form.contrasenia}
            onChange={(e) => set("contrasenia", e.target.value)}
            minLength={8}
            required
          />
          <Field
            label="Confirmar contraseña"
            type="password"
            value={confirmacion}
            onChange={(e) => setConfirmacion(e.target.value)}
            minLength={8}
            required
          />

          <fieldset>
            <legend>Dirección (opcional)</legend>
            <Field label="País" value={form.dirPais ?? ""} onChange={(e) => set("dirPais", e.target.value)} />
            <Field label="Localidad" value={form.dirLocalidad ?? ""} onChange={(e) => set("dirLocalidad", e.target.value)} />
            <Field label="Calle" value={form.dirCalle ?? ""} onChange={(e) => set("dirCalle", e.target.value)} />
            <Field label="Número" value={form.dirNumero ?? ""} onChange={(e) => set("dirNumero", e.target.value)} />
            <Field label="Código postal" value={form.dirCodigoPostal ?? ""} onChange={(e) => set("dirCodigoPostal", e.target.value)} />
          </fieldset>

          {error && <Banner kind="error">{error}</Banner>}

          <div className="row">
            <button type="button" className="secondary" onClick={onBack}>
              Volver
            </button>
            <button type="submit" disabled={busy}>
              {busy ? "Registrando…" : "Registrarme"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
