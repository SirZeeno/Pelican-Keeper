namespace Pelican_Keeper;

public static class Validator
{
    public static void ValidateSecrets(TemplateClasses.Secrets? secrets)
    {
        if (secrets == null)
            throw new ArgumentNullException(nameof(secrets), "Secrets object is null.");
        
        if (string.IsNullOrWhiteSpace(secrets.ClientToken))
            throw new ArgumentException("ClientToken is null or empty. Make sure to provide a valid Discord client token.");
        
        if (string.IsNullOrWhiteSpace(secrets.ServerToken))
            throw new ArgumentException("ServerToken is null or empty. Make sure to provide a valid token.");
        
        if (string.IsNullOrWhiteSpace(secrets.ServerUrl))
            throw new ArgumentException("ServerUrl is null or empty. Make sure to provide a valid URL.");
        
        if (string.IsNullOrWhiteSpace(secrets.BotToken))
            throw new ArgumentException("BotToken is null or empty. Make sure to provide a valid Discord bot token.");
        
        if (secrets.ChannelIds == null || secrets.ChannelIds.Length == 0)
            throw new ArgumentException("ChannelIds is null or empty. Make sure at least one channel ID is provided in the list.");
    }
    
    public static void ValidateConfig(TemplateClasses.Config? config) //TODO: Complete this to test every setting.
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config), "Config is null.");

        if (string.IsNullOrEmpty(config.InternalIpStructure))
            throw new ArgumentException("InternalIpStructure is null or empty. Make sure to provide a valid IP.");

        if (config.MessageFormat == TemplateClasses.MessageFormat.None)
            throw new ArgumentException("MessageFormat is not set. Make sure to provide a valid message format.");

        if (config.MessageSorting == TemplateClasses.MessageSorting.None)
            throw new ArgumentException("MessageSorting is not set. Make sure to provide a valid message sorting format.");

        if (config.MessageSortingDirection == TemplateClasses.MessageSortingDirection.None)
            throw new ArgumentException("MessageSortingDirection is not set. Make sure to provide a valid message sorting direction.");

        if (string.IsNullOrEmpty(config.EmptyServerTimeout))
            throw new ArgumentException("EmptyServerTimeout is not set. Make sure to provide a valid Timeout string.");

        if (config.MarkdownUpdateInterval < 10)
            throw new ArgumentException("EmptyServerTimeout is set too low. Make sure you don't turn the interval below 10 seconds.");
        
        if (config.ServerUpdateInterval < 10)
            throw new ArgumentException("ServerUpdateInterval is set too low. Make sure you don't turn the interval below 10 seconds.");
        
        //TODO: Add the variables of the config to validate its format.
    }
}