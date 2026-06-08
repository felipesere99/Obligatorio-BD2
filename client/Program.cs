using System.Net.Http.Json;
using Shared;

var baseUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:5050";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

try
{
    var result = await http.GetFromJsonAsync<PingResult>("/health");
    Console.WriteLine($"db: {result!.Db}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error connecting to server: {ex.Message}");
    Environment.Exit(1);
}
