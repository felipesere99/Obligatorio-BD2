import { useEffect, useRef, useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { api } from "../lib/api";
import type { Qr } from "../lib/types";
import { Banner, errorMessage } from "../components/ui";

/**
 * QR dinámico de una entrada: pide un código fresco al montar y lo rota
 * automáticamente cada `expiraEnSegundos` (POST /entradas/{id}/qr).
 */
export function EntradaQr({ nroEntrada }: { nroEntrada: number }) {
  const [qr, setQr] = useState<Qr | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [restante, setRestante] = useState<number>(0);
  const timerRef = useRef<number | null>(null);

  useEffect(() => {
    let activo = true;

    async function rotar() {
      try {
        const fresco = await api.post<Qr>(`/entradas/${nroEntrada}/qr`);
        if (!activo) return;
        setQr(fresco);
        setError(null);
        setRestante(fresco.expiraEnSegundos);
      } catch (e) {
        if (!activo) return;
        setError(errorMessage(e));
      }
    }

    rotar();
    return () => {
      activo = false;
      if (timerRef.current) window.clearInterval(timerRef.current);
    };
  }, [nroEntrada]);

  // Cuenta regresiva; cuando llega a 0, rota el código.
  useEffect(() => {
    if (!qr) return;
    timerRef.current = window.setInterval(() => {
      setRestante((s) => {
        if (s <= 1) {
          api
            .post<Qr>(`/entradas/${nroEntrada}/qr`)
            .then((fresco) => {
              setQr(fresco);
              setError(null);
            })
            .catch((e) => setError(errorMessage(e)));
          return qr.expiraEnSegundos;
        }
        return s - 1;
      });
    }, 1000);
    return () => {
      if (timerRef.current) window.clearInterval(timerRef.current);
    };
  }, [qr, nroEntrada]);

  if (error) return <Banner kind="error">{error}</Banner>;
  if (!qr) return <p className="muted">Generando QR…</p>;

  return (
    <div className="qr-box">
      <QRCodeSVG value={qr.codigo} size={180} includeMargin />
      <p className="muted small">
        Se renueva en {restante}s · entrada #{qr.nroEntrada}
      </p>
      <code className="small">{qr.codigo}</code>
    </div>
  );
}
