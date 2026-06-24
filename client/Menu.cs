using Shared;
using Shared.Contracts;

namespace Client;

/// <summary>Una opción de menú: etiqueta + acción que recibe el cliente.</summary>
public record MenuItem(string Label, Func<ApiClient, Task> Action);

/// <summary>
/// Arma el menú según el rol. Cada persona agrega sus MenuItem en el bloque
/// correspondiente; la acción típicamente llama a un endpoint vía <see cref="ApiClient"/>.
/// </summary>
public static class Menu
{
    public static List<MenuItem> For(string rol) => rol switch
    {
        Roles.Administrador => new()
        {
            new("Verificar conexión (health)", Health),
            new("Listar usuarios generales", ListarUsuarios),
            new("Registrar equipo", RegistrarEquipo),
            new("Listar equipos", ListarEquipos),
            new("Registrar estadio", RegistrarEstadio),
            new("Agregar sector a un estadio", RegistrarSector),
            new("Listar estadios", ListarEstadios),
            new("Crear evento", CrearEvento),
            new("Habilitar sector en evento", HabilitarSector),
            new("Listar eventos", ListarEventos),
            new("Setear comisión", SetComision),
            new("Ver comisión vigente", VerComisionVigente),
        },
        Roles.Funcionario => new()
        {
            new("Verificar conexión (health)", Health),
            // Persona B: new("Validar entrada", ...),
            //            new("Mis asignaciones", ...),
        },
        Roles.UsuarioGeneral => new()
        {
            new("Verificar conexión (health)", Health),
            new("Comprar entradas", ComprarEntradas),
            new("Mis compras", MisCompras),
            new("Transferir entrada", TransferirEntrada),
            new("Mis transferencias", MisTransferencias),
            new("Mis entradas", MisEntradas),
        },
        _ => new() { new("Verificar conexión (health)", Health) },
    };

    private static async Task Health(ApiClient api)
    {
        var pong = await api.GetAsync<PingResult>("/health");
        Console.WriteLine($"db: {pong.Db}");
    }

    private static async Task ListarUsuarios(ApiClient api)
    {
        var usuarios = await api.GetAsync<List<UsuarioGeneralResponse>>("/usuarios/generales");
        if (usuarios.Count == 0)
        {
            Console.WriteLine("No hay usuarios generales.");
            return;
        }

        Console.WriteLine($"{"Documento",-12} {"Nombre",-20} {"Correo",-28} Verificado");
        foreach (var u in usuarios)
            Console.WriteLine($"{u.Documento,-12} {$"{u.Nombre} {u.Apellido}",-20} {u.Correo,-28} {(u.EstadoVerificacion ? "sí" : "no")}");
    }

    private static async Task RegistrarEquipo(ApiClient api)
    {
        Console.WriteLine("\n-- Alta de equipo --");
        var pais = Prompt("País (código, ej. URU)");
        var nombre = Prompt("Nombre");

        var creado = await api.PostAsync<EquipoResponse>("/equipos", new RegistrarEquipoRequest(pais, nombre));
        Console.WriteLine($"Equipo {creado.Nombre} ({creado.Pais}) registrado.");
    }

    private static async Task ListarEquipos(ApiClient api)
    {
        var equipos = await api.GetAsync<List<EquipoResponse>>("/equipos");
        if (equipos.Count == 0)
        {
            Console.WriteLine("No hay equipos.");
            return;
        }

        Console.WriteLine($"{"País",-8} Nombre");
        foreach (var e in equipos)
            Console.WriteLine($"{e.Pais,-8} {e.Nombre}");
    }

    private static async Task RegistrarEstadio(ApiClient api)
    {
        Console.WriteLine("\n-- Alta de estadio --");
        var nombre = Prompt("Nombre");
        var direccion = PromptOptional("Dirección (opcional)");

        await api.PostAsync<object>("/estadios", new RegistrarEstadioRequest(nombre, direccion));
        Console.WriteLine($"Estadio {nombre} registrado.");
    }

    private static async Task RegistrarSector(ApiClient api)
    {
        Console.WriteLine("\n-- Alta de sector --");
        var estadio = Prompt("Estadio");
        var nombre = Prompt("Nombre del sector");
        var capacidad = PromptInt("Capacidad");
        var costo = PromptDecimal("Costo de entrada");

        var creado = await api.PostAsync<SectorResponse>(
            $"/estadios/{Uri.EscapeDataString(estadio)}/sectores",
            new RegistrarSectorRequest(nombre, capacidad, costo));
        Console.WriteLine($"Sector {creado.Nombre} agregado a {estadio} (cap. {creado.Capacidad}, ${creado.CostoEntrada}).");
    }

    private static async Task ListarEstadios(ApiClient api)
    {
        var estadios = await api.GetAsync<List<EstadioResponse>>("/estadios");
        if (estadios.Count == 0)
        {
            Console.WriteLine("No hay estadios.");
            return;
        }

        foreach (var e in estadios)
        {
            Console.WriteLine($"\n{e.Nombre}{(e.Direccion is null ? "" : $" — {e.Direccion}")}");
            if (e.Sectores.Count == 0)
                Console.WriteLine("  (sin sectores)");
            foreach (var s in e.Sectores)
                Console.WriteLine($"  · {s.Nombre,-10} cap. {s.Capacidad,-5} ${s.CostoEntrada}");
        }
    }

    private static async Task CrearEvento(ApiClient api)
    {
        Console.WriteLine("\n-- Alta de evento --");
        var nombre = Prompt("Nombre");
        var inicio = PromptDate("Fecha/hora inicio (ej. 2026-06-20 18:00)");
        var fin = PromptDate("Fecha/hora fin    (ej. 2026-06-20 20:00)");
        var local = Prompt("País local (código, ej. URU)");
        var visitante = Prompt("País visitante (código, ej. ARG)");
        var estadio = Prompt("Estadio");

        var creado = await api.PostAsync<EventoCreadoResponse>(
            "/eventos",
            new CrearEventoRequest(nombre, inicio, fin, local, visitante, estadio));
        Console.WriteLine($"Evento creado con id {creado.IdEvento}.");
    }

    private static async Task HabilitarSector(ApiClient api)
    {
        Console.WriteLine("\n-- Habilitar sector en evento --");
        var idEvento = PromptInt("Id del evento");
        var estadio = Prompt("Estadio");
        var sector = Prompt("Sector");

        await api.PostAsync<object>(
            $"/eventos/{idEvento}/sectores",
            new HabilitarSectorRequest(estadio, sector));
        Console.WriteLine($"Sector {sector} habilitado para el evento {idEvento}.");
    }

    private static async Task ListarEventos(ApiClient api)
    {
        var eventos = await api.GetAsync<List<EventoResponse>>("/eventos");
        if (eventos.Count == 0)
        {
            Console.WriteLine("No hay eventos.");
            return;
        }

        foreach (var e in eventos)
        {
            Console.WriteLine($"\n#{e.IdEvento} {e.Nombre} — {e.PaisLocal} vs {e.PaisVisitante} @ {e.NombreEstadio}");
            Console.WriteLine($"   {e.FechaInicio:yyyy-MM-dd HH:mm} → {e.FechaFin:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"   Sectores habilitados: {(e.SectoresHabilitados.Count == 0 ? "(ninguno)" : string.Join(", ", e.SectoresHabilitados))}");
        }
    }

    private static async Task SetComision(ApiClient api)
    {
        Console.WriteLine("\n-- Setear comisión --");
        var porcentaje = PromptDecimal("Porcentaje (ej. 7.00)");

        var c = await api.PostAsync<ComisionResponse>("/comisiones", new SetComisionRequest(porcentaje));
        Console.WriteLine($"Comisión vigente ahora: {c.Porcentaje}% (id {c.IdComision}, desde {c.VigenteDesde:yyyy-MM-dd HH:mm}).");
    }

    private static async Task VerComisionVigente(ApiClient api)
    {
        var c = await api.GetAsync<ComisionResponse>("/comisiones/vigente");
        Console.WriteLine($"Comisión vigente: {c.Porcentaje}% (id {c.IdComision}, desde {c.VigenteDesde:yyyy-MM-dd HH:mm}).");
    }

    private static async Task ComprarEntradas(ApiClient api)
    {
        Console.WriteLine("\n-- Comprar entradas (hasta 5) --");

        var eventos = await api.GetAsync<List<EventoResponse>>("/eventos");
        if (eventos.Count == 0)
        {
            Console.WriteLine("No hay eventos disponibles.");
            return;
        }

        var estadios = await api.GetAsync<List<EstadioResponse>>("/estadios");
        var estadiosPorNombre = estadios.ToDictionary(e => e.Nombre);

        var items = new List<CompraItem>();
        while (items.Count < 5)
        {
            Console.WriteLine($"\nItem #{items.Count + 1}:");
            var evento = PromptEvento(eventos);
            if (evento is null)
                break;

            var sector = PromptSectorEvento(evento, estadiosPorNombre);
            if (sector is null)
                continue;

            var fila = PromptOptional("  Fila (opcional)");
            var asiento = PromptOptional("  Asiento (opcional)");
            items.Add(new CompraItem(evento.IdEvento, evento.NombreEstadio, sector, fila, asiento));
        }

        if (items.Count == 0)
        {
            Console.WriteLine("Compra cancelada (sin items).");
            return;
        }

        var venta = await api.PostAsync<VentaCreadaResponse>("/ventas", new CrearVentaRequest(items));
        Console.WriteLine($"Compra OK. Venta #{venta.NroVenta} — total ${venta.MontoTotal} ({items.Count} entrada(s)).");
    }

    private static EventoResponse? PromptEvento(List<EventoResponse> eventos)
    {
        Console.WriteLine("  Eventos disponibles:");
        for (var i = 0; i < eventos.Count; i++)
        {
            var e = eventos[i];
            Console.WriteLine(
                $"    {i + 1}) #{e.IdEvento} {e.Nombre} — {e.PaisLocal} vs {e.PaisVisitante} @ {e.NombreEstadio}");
        }
        Console.WriteLine("    0) Terminar");

        while (true)
        {
            var sel = PromptOptional("  Elegí evento (número)");
            if (sel is null || sel == "0")
                return null;
            if (int.TryParse(sel, out var n) && n >= 1 && n <= eventos.Count)
                return eventos[n - 1];
            Console.WriteLine("  Opción inválida, reintentá.");
        }
    }

    private static string? PromptSectorEvento(EventoResponse evento, Dictionary<string, EstadioResponse> estadios)
    {
        if (evento.SectoresHabilitados.Count == 0)
        {
            Console.WriteLine($"  El evento #{evento.IdEvento} no tiene sectores habilitados.");
            return null;
        }

        estadios.TryGetValue(evento.NombreEstadio, out var estadio);
        var costos = estadio?.Sectores.ToDictionary(s => s.Nombre, s => s.CostoEntrada)
            ?? new Dictionary<string, decimal>();

        Console.WriteLine($"  Estadio: {evento.NombreEstadio}");
        Console.WriteLine("  Sectores habilitados:");
        for (var i = 0; i < evento.SectoresHabilitados.Count; i++)
        {
            var nombre = evento.SectoresHabilitados[i];
            var precio = costos.TryGetValue(nombre, out var c) ? $" (${c})" : "";
            Console.WriteLine($"    {i + 1}) {nombre}{precio}");
        }

        while (true)
        {
            var sel = Prompt("  Elegí sector (número)");
            if (int.TryParse(sel, out var n) && n >= 1 && n <= evento.SectoresHabilitados.Count)
                return evento.SectoresHabilitados[n - 1];
            Console.WriteLine("  Opción inválida, reintentá.");
        }
    }

    private static async Task TransferirEntrada(ApiClient api)
    {
        Console.WriteLine("\n-- Transferir entrada --");
        var nroEntrada = PromptInt("Nro. de entrada");
        var docReceptor = Prompt("Documento del receptor");

        var t = await api.PostAsync<TransferenciaResponse>(
            "/transferencias", new IniciarTransferenciaRequest(nroEntrada, docReceptor));
        Console.WriteLine(
            $"Transferencia iniciada: entrada #{t.NroEntrada} → {t.DocReceptor} (estado {t.Estado}, contador {t.Contador}).");
    }

    private static async Task MisTransferencias(ApiClient api)
    {
        var doc = api.Session!.Documento;
        var transferencias = await api.GetAsync<List<TransferenciaResponse>>($"/usuarios/{doc}/transferencias");
        if (transferencias.Count == 0)
        {
            Console.WriteLine("No tenés transferencias.");
            return;
        }

        Console.WriteLine($"\n{"Entrada",-8} {"Fecha",-18} {"Cont.",-5} {"Emisor",-12} {"Receptor",-12} Estado");
        foreach (var t in transferencias)
            Console.WriteLine(
                $"#{t.NroEntrada,-7} {t.FechaHora:yyyy-MM-dd HH:mm,-18} {t.Contador,-5} {t.DocEmisor,-12} {t.DocReceptor,-12} {t.Estado}");

        var sel = PromptOptional("\nResolver transferencia pendiente de qué entrada (vacío para volver)");
        if (sel is null || !int.TryParse(sel, out var nro))
            return;

        var pendiente = transferencias.Find(t => t.NroEntrada == nro && t.Estado == "pendiente");
        if (pendiente is null)
        {
            Console.WriteLine("No hay transferencia pendiente para esa entrada.");
            return;
        }

        Console.WriteLine("Acción: aceptar | rechazar | cancelar");
        var accion = Prompt("Acción");
        var res = await api.PatchAsync<TransferenciaResponse>(
            $"/transferencias/{nro}", new ResolverTransferenciaRequest(accion));
        Console.WriteLine($"Transferencia #{res.NroEntrada} → estado {res.Estado}.");
    }

    private static async Task MisEntradas(ApiClient api)
    {
        var doc = api.Session!.Documento;
        var entradas = await api.GetAsync<List<EntradaTenenciaResponse>>($"/usuarios/{doc}/entradas");
        if (entradas.Count == 0)
        {
            Console.WriteLine("No tenés entradas.");
            return;
        }

        Console.WriteLine($"\n{"Entrada",-8} {"Evento",-7} Estadio / Sector");
        foreach (var e in entradas)
            Console.WriteLine(
                $"#{e.NroEntrada,-7} #{e.IdEvento,-6} {e.NombreEstadio}/{e.NombreSector}" +
                $"{(e.Fila is null && e.Asiento is null ? "" : $" (fila {e.Fila ?? "-"}, asiento {e.Asiento ?? "-"})")}");
    }

    private static async Task MisCompras(ApiClient api)
    {
        var doc = api.Session!.Documento;
        var compras = await api.GetAsync<List<CompraResponse>>($"/usuarios/{doc}/compras");
        if (compras.Count == 0)
        {
            Console.WriteLine("No tenés compras.");
            return;
        }

        Console.WriteLine($"\n{"Venta",-7} {"Total",-10} {"Estado",-12} {"Fecha",-18} Entradas");
        foreach (var c in compras)
            Console.WriteLine($"#{c.NroVenta,-6} ${c.MontoTotal,-9} {c.Estado,-12} {c.Fecha:yyyy-MM-dd HH:mm,-18} {c.CantidadEntradas}");

        var sel = PromptOptional("\nVer entradas de qué venta (vacío para volver)");
        if (sel is null || !int.TryParse(sel, out var nro))
            return;

        var entradas = await api.GetAsync<List<EntradaResponse>>($"/ventas/{nro}/entradas");
        Console.WriteLine($"\nEntradas de la venta #{nro}:");
        foreach (var e in entradas)
            Console.WriteLine($"  #{e.NroEntrada} evento {e.IdEvento} — {e.NombreEstadio}/{e.NombreSector}" +
                $"{(e.Fila is null && e.Asiento is null ? "" : $" (fila {e.Fila ?? "-"}, asiento {e.Asiento ?? "-"})")}");
    }

    /// <summary>Registro de un usuario general (no requiere sesión).</summary>
    public static async Task Registrarme(ApiClient api)
    {
        Console.WriteLine("\n-- Registro de usuario general --");
        var documento = Prompt("Documento");
        var nombre = Prompt("Nombre");
        var apellido = Prompt("Apellido");
        var correo = Prompt("Correo");
        Console.WriteLine("Dirección (opcional, Enter para omitir):");
        var pais = PromptOptional("  País");
        var localidad = PromptOptional("  Localidad");
        var calle = PromptOptional("  Calle");
        var numero = PromptOptional("  Número");
        var cp = PromptOptional("  Código postal");

        var req = new RegistrarUsuarioRequest(
            documento, nombre, apellido, correo, pais, localidad, calle, numero, cp);
        var creado = await api.PostAsync<UsuarioRegistradoResponse>("/usuarios/generales", req);
        Console.WriteLine($"Usuario {creado.Documento} registrado. Ya podés iniciar sesión con ese documento.");
    }

    private static string Prompt(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
            Console.WriteLine($"{label} es obligatorio.");
        }
    }

    private static string? PromptOptional(string label)
    {
        Console.Write($"{label}: ");
        var value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int PromptInt(string label)
    {
        while (true)
        {
            if (int.TryParse(Prompt(label), out var value))
                return value;
            Console.WriteLine("Ingresá un número entero.");
        }
    }

    private static decimal PromptDecimal(string label)
    {
        while (true)
        {
            if (decimal.TryParse(Prompt(label), out var value))
                return value;
            Console.WriteLine("Ingresá un número (ej. 150.00).");
        }
    }

    private static DateTimeOffset PromptDate(string label)
    {
        while (true)
        {
            if (DateTimeOffset.TryParse(Prompt(label), out var value))
                return value;
            Console.WriteLine("Fecha inválida. Usá el formato 2026-06-20 18:00.");
        }
    }
}
