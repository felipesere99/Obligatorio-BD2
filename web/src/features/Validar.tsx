import { useEffect, useState } from "react";
import { api } from "../lib/api";
import type { Dispositivo, Validacion } from "../lib/types";
import { Banner, Card, Field, Loading, errorMessage, useAsync } from "../components/ui";

/**
 * Pantalla del funcionario: escanea (o pega) el código del QR de una entrada y
 * la valida con uno de sus dispositivos (POST /validaciones).
 */
export function Validar() {
  const dispositivos = useAsync(() => api.get<Dispositivo[]>("/funcionario/dispositivos"));

  const [codigo, setCodigo] = useState("");
  const [idDispositivo, setIdDispositivo] = useState<number | "">("");
  const [enviando, setEnviando] = useState(false);
  const [ok, setOk] = useState<Validacion | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Default: primer dispositivo cuando carga la lista (efecto, no durante el render).
  const disp = dispositivos.data;
  useEffect(() => {
    if (disp && disp.length > 0 && idDispositivo === "") {
      setIdDispositivo(disp[0].idDispositivo);
    }
  }, [disp, idDispositivo]);

  async function validar(e: React.FormEvent) {
    e.preventDefault();
    setOk(null);
    setError(null);

    if (!codigo.trim()) {
      setError("Ingresá el código del QR.");
      return;
    }
    if (idDispositivo === "") {
      setError("Seleccioná un dispositivo.");
      return;
    }

    setEnviando(true);
    try {
      const res = await api.post<Validacion>("/validaciones", {
        codigo: codigo.trim(),
        idDispositivo,
      });
      setOk(res);
      setCodigo("");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <Card title="Validar ingreso" subtitle="Escaneá o pegá el código del QR de la entrada.">
      {dispositivos.loading && <Loading />}
      {dispositivos.error && <Banner kind="error">{dispositivos.error}</Banner>}
      {disp && disp.length === 0 && (
        <Banner kind="error">No tenés dispositivos asignados.</Banner>
      )}

      {disp && disp.length > 0 && (
        <form onSubmit={validar} className="form">
          <Field
            label="Código del QR"
            value={codigo}
            autoFocus
            placeholder="Escaneá o pegá el código"
            onChange={(e) => setCodigo(e.target.value)}
          />

          <label className="field">
            <span>Dispositivo</span>
            <select
              value={idDispositivo}
              onChange={(e) => setIdDispositivo(Number(e.target.value))}
            >
              {disp.map((d) => (
                <option key={d.idDispositivo} value={d.idDispositivo}>
                  #{d.idDispositivo}
                </option>
              ))}
            </select>
          </label>

          <button type="submit" disabled={enviando || !codigo.trim()}>
            {enviando ? "Validando…" : "Validar"}
          </button>
        </form>
      )}

      {ok && (
        <Banner kind="ok">
          Ingreso validado · entrada #{ok.nroEntrada} · {ok.nombreEstadio}/{ok.nombreSector} ·{" "}
          {new Date(ok.fechaHora).toLocaleString("es-UY", {
            dateStyle: "short",
            timeStyle: "short",
          })}
        </Banner>
      )}
      {error && <Banner kind="error">{error}</Banner>}
    </Card>
  );
}
