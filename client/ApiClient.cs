using System.Net.Http.Json;
using Shared.Contracts;

namespace Client;

/// <summary>Error de negocio devuelto por el server (cuerpo <see cref="ApiError"/>).</summary>
public sealed class ApiException(string message) : Exception(message);

/// <summary>
/// Cliente HTTP del server. Mantiene la sesión y la reenvía como headers
/// en cada request. Convierte respuestas de error en <see cref="ApiException"/>.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(string baseUrl) =>
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };

    public UserSession? Session { get; private set; }
    public bool IsLoggedIn => Session is not null;

    public async Task<UserSession> LoginAsync(string documento, string contrasenia)
    {
        Session = await PostAsync<UserSession>("/login", new LoginRequest(documento, contrasenia));
        return Session;
    }

    public void Logout() => Session = null;

    public Task<T> GetAsync<T>(string path) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, path));

    public Task<T> PostAsync<T>(string path, object body) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) });

    public Task<T> PatchAsync<T>(string path, object body) =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Patch, path) { Content = JsonContent.Create(body) });

    public Task PatchAsync(string path, object? body = null) =>
        SendVoidAsync(new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = body is not null ? JsonContent.Create(body) : null
        });

    public Task PutAsync(string path, object body) =>
        SendVoidAsync(new HttpRequestMessage(HttpMethod.Put, path) { Content = JsonContent.Create(body) });

    public Task DeleteAsync(string path) =>
        SendVoidAsync(new HttpRequestMessage(HttpMethod.Delete, path));

    private async Task<T> SendAsync<T>(HttpRequestMessage req)
    {
        using (req)
        {
            if (Session is not null)
            {
                req.Headers.Add(AuthHeaders.Documento, Session.Documento);
                req.Headers.Add(AuthHeaders.Rol, Session.Rol);
            }

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await SafeReadError(resp);
                throw new ApiException(err);
            }

            var data = await resp.Content.ReadFromJsonAsync<T>();
            return data!;
        }
    }

    private async Task SendVoidAsync(HttpRequestMessage req)
    {
        using (req)
        {
            if (Session is not null)
            {
                req.Headers.Add(AuthHeaders.Documento, Session.Documento);
                req.Headers.Add(AuthHeaders.Rol, Session.Rol);
            }

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await SafeReadError(resp);
                throw new ApiException(err);
            }
        }
    }

    private static async Task<string> SafeReadError(HttpResponseMessage resp)
    {
        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ApiError>();
            if (!string.IsNullOrWhiteSpace(err?.Error))
                return err!.Error;
        }
        catch { /* cuerpo no-JSON */ }
        return $"Error HTTP {(int)resp.StatusCode}";
    }
}

internal static class AuthHeaders
{
    public const string Documento = "X-Documento";
    public const string Rol = "X-Rol";
}
