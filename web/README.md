# Front (React + Vite)

Front web del sistema de ticketing. Cubre lo implementado en el server hoy:

- **Login** por documento y **registro** público de usuario general.
- **Administrador**: usuarios, equipos, estadios + sectores, eventos + habilitar
  sectores, comisión vigente / setear comisión.
- **Usuario general**: comprar entradas y ver mis compras / entradas.
- **Funcionario**: sin funcionalidades todavía (no hay endpoints aún).

## Correr

El server .NET tiene que estar levantado en `http://localhost:5050` (tiene CORS
habilitado para `http://localhost:5173`).

```bash
cd web
npm install
npm run dev
```

Abre `http://localhost:5173`.

Para apuntar a otro server, copiá `.env.example` a `.env` y ajustá `VITE_SERVER_URL`.

## Build de producción

```bash
npm run build && npm run preview
```
