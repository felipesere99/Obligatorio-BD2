using Client;

var baseUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5050";
var api = new ApiClient(baseUrl);

Console.WriteLine("=== Ticketing Mundial 2026 — Cliente ===");
Console.WriteLine("(documentos de prueba: ADM-1, FUN-1, UG-1, UG-2, UG-3)");

while (true)
{
    if (!api.IsLoggedIn)
    {
        if (!await TryLogin(api))
            return; // documento vacío -> salir
        continue;
    }

    var s = api.Session!;
    var options = Menu.For(s.Rol);

    Console.WriteLine($"\nSesión: {s.Nombre} ({s.Rol}) [{s.Documento}]");
    for (var i = 0; i < options.Count; i++)
        Console.WriteLine($"  {i + 1}. {options[i].Label}");
    Console.WriteLine("  0. Cerrar sesión");
    Console.Write("> ");

    var input = Console.ReadLine();
    if (input == "0")
    {
        api.Logout();
        continue;
    }

    if (int.TryParse(input, out var idx) && idx >= 1 && idx <= options.Count)
    {
        try
        {
            await options[idx - 1].Action(api);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"⚠  {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inesperado: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Opción inválida.");
    }
}

static async Task<bool> TryLogin(ApiClient api)
{
    Console.Write("\nDocumento ('r' para registrarme, vacío para salir): ");
    var doc = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(doc))
        return false;

    if (doc.Trim().Equals("r", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            await Menu.Registrarme(api);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"⚠  {ex.Message}");
        }
        return true;
    }

    try
    {
        var s = await api.LoginAsync(doc.Trim());
        Console.WriteLine($"Bienvenido, {s.Nombre} ({s.Rol}).");
    }
    catch (ApiException ex)
    {
        Console.WriteLine($"⚠  {ex.Message}");
    }

    return true;
}
