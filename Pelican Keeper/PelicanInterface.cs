﻿using System.Text.Json;
using RestSharp;

namespace Pelican_Keeper;

using static TemplateClasses;
using static HelperClass;

public static class PelicanInterface
{
    /// <summary>
    /// Gets the server stats from the Pelican API
    /// </summary>
    /// <param name="uuid">UUID of the game server</param>
    /// <returns>The server stats response</returns>
    internal static StatsResponse? GetServerStats(string? uuid)
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/client/servers/" + uuid + "/resources");
        var response = CreateRequest(client, Program.Secrets.ClientToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var stats = JsonSerializer.Deserialize<StatsResponse>(response.Content, options);

                if (stats?.Attributes != null)
                    return stats;

                Console.WriteLine("Stats response had null attributes.");
            }
            else
            {
                Console.WriteLine("Empty response content.");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine("JSON deserialization error: " + ex.Message);
            Console.WriteLine("Response content: " + response.Content);
        }

        return null;
    }

    /// <summary>
    /// Gets the list of servers from the Pelican API
    /// </summary>
    /// <returns>Server list response</returns>
    internal static ServerListResponse? GetServersList()
    {
        var client = new RestClient(Program.Secrets.ServerUrl + "/api/application/servers");
        var response = CreateRequest(client, Program.Secrets.ServerToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            if (response.Content != null)
            {
                var server = JsonSerializer.Deserialize<ServerListResponse>(response.Content, options);
                return server;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine("JSON Error: " + ex.Message);
            Console.WriteLine("JSON: " + response.Content);
        }

        return null;
    }

    /// <summary>
    /// Gets alist of server stats from the Pelican API
    /// </summary>
    /// <param name="uuids">list of game server UUIDs</param>
    /// <returns>list of server stats responses</returns>
    internal static List<StatsResponse?> GetServerStatsList(List<string?> uuids)
    {
        List<StatsResponse?> stats = uuids.Select(GetServerStats).ToList();
        return stats.Where(s => s != null).ToList(); 
    }
}