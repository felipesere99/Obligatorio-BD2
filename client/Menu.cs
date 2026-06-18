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
            // Persona A: new("Registrar equipo", Acciones.RegistrarEquipo),
            //            new("Crear evento", Acciones.CrearEvento),
            //            new("Habilitar sectores", ...),
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
}
