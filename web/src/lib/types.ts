// Tipos que reflejan los contratos del server (shared/Contracts).
// El JSON del server viene en camelCase (minimal APIs por defecto).

export type Rol = "administrador" | "funcionario" | "usuario_general";

export interface UserSession {
  documento: string;
  rol: Rol;
  nombre: string;
}

export interface RegistrarUsuarioRequest {
  documento: string;
  nombre: string;
  apellido: string;
  correo: string;
  dirPais?: string | null;
  dirLocalidad?: string | null;
  dirCalle?: string | null;
  dirNumero?: string | null;
  dirCodigoPostal?: string | null;
}

export interface UsuarioGeneral {
  documento: string;
  nombre: string;
  apellido: string;
  correo: string;
  estadoVerificacion: boolean;
}

export interface Funcionario {
  documento: string;
  nombre: string;
  apellido: string;
  correo: string;
  nroLegajo: string;
}

export interface Equipo {
  pais: string;
  nombre: string;
}

export interface Sector {
  nombre: string;
  capacidad: number;
  costoEntrada: number;
}

export interface Estadio {
  nombre: string;
  direccion: string | null;
  sectores: Sector[];
}

export interface Evento {
  idEvento: number;
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  paisLocal: string | null;
  paisVisitante: string | null;
  nombreEstadio: string;
  sectoresHabilitados: string[];
}

export interface Comision {
  idComision: number;
  porcentaje: number;
  vigenteDesde: string;
}

export interface CompraItem {
  idEvento: number;
  estadio: string;
  sector: string;
  fila?: string | null;
  asiento?: string | null;
}

export interface VentaCreada {
  nroVenta: number;
  montoTotal: number;
}

export interface Compra {
  nroVenta: number;
  montoTotal: number;
  estado: string;
  fecha: string;
  cantidadEntradas: number;
}

export interface Entrada {
  nroEntrada: number;
  idEvento: number;
  nombreEstadio: string;
  nombreSector: string;
  fila: string | null;
  asiento: string | null;
}

export interface Asignacion {
  docFuncionario: string;
  nombreFuncionario: string;
  idEvento: number;
  nombreEstadio: string;
  nombreSector: string;
}

export interface Dispositivo {
  idDispositivo: number;
  nroSerie: string;
  marca: string;
  modelo: string;
  habilitado: boolean;
  funcionariosAsignados: string[];
}

export interface ReporteEventoVentas {
  idEvento: number;
  nombreEvento: string;
  nombreEstadio: string;
  cantidadEntradas: number;
  totalVentas: number;
}

export interface ReporteSectorVentas {
  idEvento: number;
  nombreEvento: string;
  nombreEstadio: string;
  nombreSector: string;
  cantidadEntradas: number;
  totalVentas: number;
}

export interface Qr {
  idCodigo: number;
  nroEntrada: number;
  codigo: string;
  generadoEn: string;
  expiraEnSegundos: number;
}

export interface Dispositivo {
  idDispositivo: number;
}

export interface Validacion {
  nroEntrada: number;
  idEvento: number;
  nombreEstadio: string;
  nombreSector: string;
  fechaHora: string;
  docFuncionario: string;
  idDispositivo: number;
}

export interface Transferencia {
  nroEntrada: number;
  fechaHora: string;
  contador: number;
  docEmisor: string;
  docReceptor: string;
  estado: "pendiente" | "aceptada" | "rechazada" | "cancelada";
}

export interface EntradaTenencia {
  nroEntrada: number;
  idEvento: number;
  nombreEstadio: string;
  nombreSector: string;
  fila: string | null;
  asiento: string | null;
}

export interface ReporteComprador {
  documento: string;
  nombre: string;
  apellido: string;
  cantidadCompras: number;
  cantidadEntradas: number;
  totalGastado: number;
}
