using System.Globalization;

namespace Pelican_Keeper.Validators;

public static class ConfigValidator
{
    public static void ValidateConfig(TemplateClasses.Config config)
    {
        int serverUuidLength = 9;
        int discordUserIdLenth = 6;
        
        if (string.IsNullOrEmpty(config.InternalIpStructure))
            throw new ArgumentException(
                "InternalIpStructure is null or empty. Make sure to provide a valid IP structure.");
        string[] segements = config.InternalIpStructure.Split('.');
        if (segements.Length != 4)
            throw new ArgumentException(
                "Your IP structure doesn't contain 4 Segments. Make sure to provide a valid IP structure.");

        if (config.MarkdownUpdateInterval < 10)
            throw new ArgumentException(
                "EmptyServerTimeout is set too low. Make sure you don't turn the interval below 10 seconds.");

        if (config.ServerUpdateInterval < 10)
            throw new ArgumentException(
                "ServerUpdateInterval is set too low. Make sure you don't turn the interval below 10 seconds.");

        //CustomDateTimeFormat
        if (string.IsNullOrEmpty(config.CustomDateTimeFormat))
            throw new ArgumentException(
                "CustomDateTimeFormat is null or empty. Make sure to set a valid Date Time Format.");
        try
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            DateTime.Now.ToString(config.CustomDateTimeFormat); //Catches invalid formats in the conversion process
        }
        catch
        {
            throw new ArgumentException(
                "CustomDateTimeFormat is not set to a valid format. Make sure to set a valid custom Date Time Format.");
        }

        //EmptyServerTimeout
        if (string.IsNullOrEmpty(config.EmptyServerTimeout))
            throw new ArgumentException(
                "EmptyServerTimeout is null or empty. Make sure to set a valid d:hh:mm Time span Format.");
        try
        {
            TimeSpan.ParseExact(config.EmptyServerTimeout!, @"dd\:hh\:mm", CultureInfo.InvariantCulture); //Catches invalid formats in the conversion process
        }
        catch (Exception e)
        {
            throw new ArgumentException("EmptyServerTimeout is not set to a valid format. Make sure to set a valid dd:hh:mm Time span Format.");
        }

        //ServersToIgnore
        if (config.ServersToIgnore == null)
            throw new ArgumentException(
                "EmptyServerTimeout is null or empty. Make sure to provide a valid list of server UUIDs.");
        foreach (var serversToIgnore in config.ServersToIgnore)
        {
            if (serversToIgnore.Length < serverUuidLength)
                throw new ArgumentException(
                    "A EmptyServerTimeout UUID is too short. Make sure to set a valid d:hh:mm Time span Format.");
        }

        //ServersToMonitor
        if (config.ServersToMonitor == null)
            throw new ArgumentException(
                "ServersToMonitor is null or empty. Make sure to set a list of server UUIDs.");
        foreach (var serversToMonitor in config.ServersToMonitor)
        {
            if (serversToMonitor.Length < serverUuidLength)
                throw new ArgumentException(
                    "A ServersToMonitor UUID is too short. Make sure to provide a valid list of server UUIDs.");
        }
        
        //ServersToAutoShutdown
        if (config.ServersToAutoShutdown == null)
            throw new ArgumentException(
                "ServersToAutoShutdown is null or empty. Make sure to set a list of server UUIDs.");
        foreach (var serversToAutoShutdown in config.ServersToAutoShutdown)
        {
            if (serversToAutoShutdown.Length < serverUuidLength)
                throw new ArgumentException(
                    "A ServersToAutoShutdown UUID is too short. Make sure to provide a valid list of server UUIDs.");
        }
        
        //AllowServerStartup
        if (config.AllowServerStartup == null)
            throw new ArgumentException(
                "AllowServerStartup is null or empty. Make sure to set a list of server UUIDs.");
        foreach (var allowServerStartup in config.AllowServerStartup)
        {
            if (allowServerStartup.Length < serverUuidLength)
                throw new ArgumentException(
                    "A AllowServerStartup UUID is too short. Make sure to provide a valid list of server UUIDs.");
        }
        
        //AllowServerStopping
        if (config.AllowServerStopping == null)
            throw new ArgumentException(
                "AllowServerStopping is null or empty. Make sure to set a list of server UUIDs.");
        foreach (var allowServerStopping in config.AllowServerStopping)
        {
            if (allowServerStopping.Length < serverUuidLength)
                throw new ArgumentException(
                    "A AllowServerStopping UUID is too short. Make sure to provide a valid list of server UUIDs.");
        }
        
        //ServersToDisplay
        if (config.ServersToDisplay == null)
            throw new ArgumentException(
                "ServersToDisplay is null or empty. Make sure to set a list of server UUIDs.");
        foreach (var allowServerStopping in config.ServersToDisplay)
        {
            if (allowServerStopping.Length < serverUuidLength)
                throw new ArgumentException(
                    "A ServersToDisplay UUID is too short. Make sure to provide a valid list of server UUIDs.");
        }
        
        //UsersAllowedToStartServers
        if (config.UsersAllowedToStartServers == null)
            throw new ArgumentException(
                "UsersAllowedToStartServers is null or empty. Make sure to set a list of User IDs.");
        foreach (var userAllowedToStartServers in config.UsersAllowedToStartServers)
        {
            if (userAllowedToStartServers.Length < discordUserIdLenth)
                throw new ArgumentException(
                    "A UsersAllowedToStartServers User ID is too short. Make sure to provide a valid list of User IDs.");
        }
        
        //UsersAllowedToStopServers
        if (config.UsersAllowedToStopServers == null)
            throw new ArgumentException(
                "UsersAllowedToStopServers is null or empty. Make sure to set a list of User IDs.");
        foreach (var userAllowedToStopServers in config.UsersAllowedToStopServers)
        {
            if (userAllowedToStopServers.Length < discordUserIdLenth)
                throw new ArgumentException(
                    "A UsersAllowedToStopServers User ID is too short. Make sure to provide a valid list of User IDs.");
        }
    }
}