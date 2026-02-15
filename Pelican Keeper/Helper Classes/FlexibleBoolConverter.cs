using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pelican_Keeper.Helper_Classes;

/// <summary>
/// This Class is a conversion class to allow the use of other formats of booleans in the config to account for issues with Pelican Panel and allow more flexibility
/// </summary>
public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intValue))
                    return intValue != 0;
                break;

            case JsonTokenType.String:
                var str = reader.GetString()?.Trim().ToLowerInvariant();

                if (string.IsNullOrEmpty(str))
                    return false;

                if (str == "true" || str == "1" || str == "yes" || str == "on")
                    return true;

                if (str == "false" || str == "0" || str == "no" || str == "off" || str == "")
                    return false;

                break;
        }

        throw new JsonException($"Invalid boolean value: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}