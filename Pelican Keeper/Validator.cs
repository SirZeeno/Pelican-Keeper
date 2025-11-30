namespace Pelican_Keeper;

public static class Validator
{
    public static void ValidateSecrets(TemplateClasses.Secrets? secrets)
    {
        if (secrets == null)
        {
            throw new ArgumentNullException(nameof(secrets), "Secrets object is null.");
        }
        if (string.IsNullOrWhiteSpace(secrets.ClientToken))
        {
            throw new ArgumentException("ClientToken is null or empty. Make sure to provide a valid Discord client token.");
        }
        
        if (string.IsNullOrWhiteSpace(secrets.ServerToken))
        {
            throw new ArgumentException("ServerToken is null or empty. Make sure to provide a valid token.");
        }
        
        if (string.IsNullOrWhiteSpace(secrets.ServerUrl))
        {
            throw new ArgumentException("ServerUrl is null or empty. Make sure to provide a valid URL.");
        }
        
        if (string.IsNullOrWhiteSpace(secrets.BotToken))
        {
            throw new ArgumentException("BotToken is null or empty. Make sure to provide a valid Discord bot token.");
        }
        
        if (secrets.ChannelIds == null || secrets.ChannelIds.Length == 0)
        {
            throw new ArgumentException("ChannelIds is null or empty. Make sure at least one channel ID is provided in the list.");
        }
    }
    
    public static void ValidateConfig(TemplateClasses.Config? config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config), "Config is null.");
        }
        //TODO: Add the variables of the config to validate its format.
    }
}