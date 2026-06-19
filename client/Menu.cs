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
            // Persona A: new("Comprar entradas", ...),
            //            new("Mis compras", ...),
            // Persona B: new("Transferir entrada", ...),
            //            new("Mis transferencias", ...),
            //            new("Mis entradas", ...),
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
