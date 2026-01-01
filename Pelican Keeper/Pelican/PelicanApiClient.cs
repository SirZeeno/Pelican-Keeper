using System.Text.Json;
using Pelican_Keeper.Core;
using Pelican_Keeper.Models;
using Pelican_Keeper.Utilities;
using RestSharp;

namespace Pelican_Keeper.Pelican;

/// <summary>
/// REST API client for Pelican Panel interactions.
/// </summary>
public static class PelicanApiClient
{
    /// <summary>
    /// Executes an authenticated GET request to the Pelican API.
    /// </summary>
    private static RestResponse ExecuteRequest(string url, string? token)
    {
        var client = new RestClient(url);
        var request = new RestRequest("");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {token}");
        return client.Execute(request);
    }

    /// <summary>
    /// Gets the list of available eggs from the application API.
    /// </summary>
    public static List<EggInfo> GetEggList()
    {
        var url = $"{RuntimeContext.Secrets.ServerUrl}/api/application/eggs";
        var response = ExecuteRequest(url, RuntimeContext.Secrets.ClientToken);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            try
            {
                return JsonResponseParser.ExtractEggInfo(response.Content);
            }
            catch (JsonException ex)
            {
                Logger.WriteLineWithStep($"Egg list parse error: {ex.Message}", Logger.Step.PelicanApi);
            }
        }

        Logger.WriteLineWithStep("Empty egg list response.", Logger.Step.PelicanApi);
        return [];
    }

    /// <summary>
    /// Gets real-time resource usage for a server.
    /// </summary>
    public static void GetServerStats(ServerInfo serverInfo)
    {
        if (string.IsNullOrWhiteSpace(serverInfo.Uuid))
        {
            Logger.WriteLineWithStep("UUID is null or empty.", Logger.Step.PelicanApi, Logger.OutputType.Error);
            return;
        }

        var url = $"{RuntimeContext.Secrets.ServerUrl}/api/client/servers/{serverInfo.Uuid}/resources";
        var response = ExecuteRequest(url, RuntimeContext.Secrets.ClientToken);

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            try
            {
                serverInfo.Resources = JsonResponseParser.ExtractServerResources(response.Content);
                return;
            }
            catch (JsonException ex)
            {
                Logger.WriteLineWithStep($"Stats parse error: {ex.Message}", Logger.Step.PelicanApi);
            }
        }

        Logger.WriteLineWithStep("Empty stats response.", Logger.Step.PelicanApi);
    }

    /// <summary>
    /// Gets the list of servers from the application API.
    /// </summary>
    public static List<ServerInfo> GetServerList()
    {
        var url = $"{RuntimeContext.Secrets.ServerUrl}/api/application/servers";
        var response = ExecuteRequest(url, RuntimeContext.Secrets.ServerToken);

        if (response.Content == null)
        {
            Logger.WriteLineWithStep("Empty server list response.", Logger.Step.PelicanApi, Logger.OutputType.Error);
            return [];
        }

        try
        {
            return JsonResponseParser.ExtractServerListInfo(response.Content);
        }
        catch (JsonException ex)
        {
            Logger.WriteLineWithStep($"Server list parse error: {ex.Message}", Logger.Step.PelicanApi, Logger.OutputType.Error, ex);
            return [];
        }
    }

    /// <summary>
    /// Gets server allocations and variables from the client API.
    /// </summary>
    public static string? GetServerAllocationsJson()
    {
        var url = $"{RuntimeContext.Secrets.ServerUrl}/api/client/?type=admin-all";
        var response = ExecuteRequest(url, RuntimeContext.Secrets.ClientToken);
        return response.Content;
    }

    /// <summary>
    /// Sends a power command (start, stop, restart, kill) to a server.
    /// </summary>
    public static void SendPowerCommand(string? uuid, string command)
    {
        if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(command))
            return;

        var client = new RestClient($"{RuntimeContext.Secrets.ServerUrl}/api/client/servers/");
        var request = new RestRequest($"{uuid}/power", Method.Post);
        request.AddHeader("Authorization", $"Bearer {RuntimeContext.Secrets.ClientToken}");
        request.AddHeader("Content-Type", "application/json");
        request.AddStringBody(JsonSerializer.Serialize(new { signal = command }), ContentType.Json);

        client.Execute(request);
        Logger.WriteLineWithStep($"Sent {command} command to server {uuid}", Logger.Step.PelicanApi);
    }
}
