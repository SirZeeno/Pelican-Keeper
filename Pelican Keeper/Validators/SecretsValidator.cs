namespace Pelican_Keeper.Validators;

public static class SecretsValidator
{
    public static void ValidateSecrets(TemplateClasses.Secrets secrets)
    {
        //ClientToken
        if (string.IsNullOrWhiteSpace(secrets.ClientToken))
            throw new ArgumentException(
                "ClientToken is null or empty. Make sure to provide a valid Discord client token.");
        if (secrets.ClientToken.Length != 48)
            throw new ArgumentException("ClientToken is invalid. Make sure to provide a valid client token.");
        if (secrets.ClientToken.StartsWith("pacc_") || secrets.ClientToken.StartsWith("plcn_"))
            ConsoleExt.WriteLine(
                "ClientToken is not in the proper pacc or plcn format. Make sure to provide a valid client token.",
                ConsoleExt.CurrentStep.FileChecks, ConsoleExt.OutputType.Warning);

        //ServerToken
        if (string.IsNullOrWhiteSpace(secrets.ServerToken))
            throw new ArgumentException("ServerToken is null or empty. Make sure to provide a valid token.");
        if (secrets.ServerToken.Length != 48)
            throw new ArgumentException("ServerToken is invalid. Make sure to provide a valid server token.");
        if (secrets.ServerToken.StartsWith("papp_") || secrets.ClientToken.StartsWith("peli_"))
            ConsoleExt.WriteLine(
                "ServerToken is not in the proper papp or peli format. Make sure to provide a valid server token.",
                ConsoleExt.CurrentStep.FileChecks, ConsoleExt.OutputType.Warning);

        //ServerUrl
        if (string.IsNullOrWhiteSpace(secrets.ServerUrl))
            throw new ArgumentException("ServerUrl is null or empty. Make sure to provide a valid URL.");
        if (!secrets.ServerUrl.Contains('.'))
            throw new ArgumentException("ServerUrl is not a valid URL. Make sure to provide a valid URL.");

        //BotToken
        if (string.IsNullOrWhiteSpace(secrets.BotToken))
            throw new ArgumentException("BotToken is null or empty. Make sure to provide a valid Discord bot token.");
        if (secrets.BotToken.Length < 57)
            throw new ArgumentException("BotToken lenght is invalid. Make sure to provide a valid bot token.");

        //ChannelIds
        if (secrets.ChannelIds == null || secrets.ChannelIds.Length == 0)
            throw new ArgumentException(
                "ChannelIds is null or empty. Make sure at least one channel ID is provided in the list.");
        foreach (var channelId in secrets.ChannelIds)
            if (channelId.ToString().Length < 17)
                throw new ArgumentException(
                    "One of your ChannelIds is too short, Make sure you provided valid channel ids.");

        //ExternalServerIp
        if (string.IsNullOrWhiteSpace(secrets.ExternalServerIp))
            throw new ArgumentException(
                "ExternalServerIp is null or empty. Make sure to provide a valid external Server IP.");
        string[] segements = secrets.ExternalServerIp.Split('.');
        if (segements.Length != 4)
            throw new ArgumentException(
                "Your IP doesn't contain 4 Segments. Make sure to provide a valid external Server IP.");
        bool allValid = true;
        foreach (string segement in segements)
            allValid = segement.Length <= 3;
        if (!allValid)
            throw new ArgumentException(
                "One of your External IP segments is larger than 3 numbers. Make sure to provide a valid external Server IP.");
    }
}