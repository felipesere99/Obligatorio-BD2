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
}
