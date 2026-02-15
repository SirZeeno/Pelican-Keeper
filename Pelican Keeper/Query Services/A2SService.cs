using System.Net;
using System.Net.Sockets;
using System.Text;
using Pelican_Keeper.Interfaces;

namespace Pelican_Keeper.Query_Services;

public class A2SService(string ip, int port) : ISendCommand, IDisposable
{
    private UdpClient? _udpClient;
    private IPEndPoint? _endPoint;

    public Task Connect()
    {
        _udpClient = new UdpClient();
        _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _udpClient.Client.ReceiveTimeout = 3000;

        ConsoleExt.WriteLine("Connected to A2S server at " + _endPoint, ConsoleExt.CurrentStep.A2SQuery);
        return Task.CompletedTask;
    }

    public async Task<string> SendCommandAsync(string? command = null, string? regexPattern = null)
    {
        if (_udpClient == null || _endPoint == null)
            throw new InvalidOperationException("Call Connect() before sending commands.");
        
        var request = BuildA2SInfoPacket();
        await _udpClient.SendAsync(request, request.Length, _endPoint);
        ConsoleExt.WriteLine("Sent A2S_INFO request", ConsoleExt.CurrentStep.A2SQuery);
        
        var first = await ReceiveWithTimeoutAsync(_udpClient, timeoutMs: 15000);
        if (first == null)
        {
            ConsoleExt.WriteLine("Timed out waiting for server response.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error);
            return HelperClass.ServerPlayerCountDisplayCleanup(string.Empty);
        }

        ConsoleExt.WriteLine("Received response from A2S server (first packet).", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        DumpBytes(first);

        // Response header at offset 4
        if (first.Length >= 5)
        {
            byte header = first[4];

            // 0x41 = 'A' = S2C_CHALLENGE
            if (header == 0x41 && first.Length >= 9)
            {
                // bytes 5 to 8 are the challenge
                int challenge = BitConverter.ToInt32(first, 5);
                ConsoleExt.WriteLine($"Received challenge: 0x{challenge:X8}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
                
                var challenged = BuildA2SInfoPacket(challenge);
                await _udpClient.SendAsync(challenged, challenged.Length, _endPoint);
                ConsoleExt.WriteLine("Sent A2S_INFO request with challenge", ConsoleExt.CurrentStep.A2SQuery);
                
                var second = await ReceiveWithTimeoutAsync(_udpClient, timeoutMs: 15000);
                if (second == null)
                {
                    ConsoleExt.WriteLine("Timed out waiting for challenged info response.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error);
                    return HelperClass.ServerPlayerCountDisplayCleanup(string.Empty);
                }

                ConsoleExt.WriteLine("Received challenged info response.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
                DumpBytes(second);

                return ParseOrFail(second);
            }
            // 0x49 = 'I' = S2A_INFO (immediate info response, no challenge)
            if (header == 0x49)
            {
                return ParseOrFail(first);
            }

            // Some servers may reply multi-packet (0xFE) or other types, but I will treat them as unsupported for now
            ConsoleExt.WriteLine($"Unexpected response header: 0x{header:X2}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Warning);
        }

        ConsoleExt.WriteLine("Invalid or unexpected response.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error);
        return HelperClass.ServerPlayerCountDisplayCleanup(string.Empty);
    }

    private static async Task<byte[]?> ReceiveWithTimeoutAsync(UdpClient udp, int timeoutMs)
    {
        try
        {
            var receiveTask = udp.ReceiveAsync();
            var t = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));
            if (t != receiveTask) return null;

            var result = receiveTask.Result;
            return result.Buffer;
        }
        catch (SocketException ex)
        {
            ConsoleExt.WriteLine("No response from server.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error, ex);
            return null;
        }
    }

    private static string ParseOrFail(byte[] buffer)
    {
        string parseResult = ParseA2SInfoResponse(buffer);

        if (string.IsNullOrEmpty(parseResult))
        {
            ConsoleExt.WriteLine("Failed to parse response.", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error);
            return string.Empty;
        }

        ConsoleExt.WriteLine($"A2S request response: {parseResult}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);

        return parseResult;
    }

    /// <summary>
    /// Builds the A2S info packet with an optional challenge response.
    /// </summary>
    /// <param name="challenge">The Solved Challenge</param>
    /// <returns>Built A2S info Packet</returns>
    private static byte[] BuildA2SInfoPacket(int? challenge = null)
    {
        var head = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, (byte)'T' };
        var query = "Source Engine Query\0"u8.ToArray();

        if (challenge.HasValue)
        {
            var chall = BitConverter.GetBytes(challenge.Value); // little-endian
            return head.Concat(query).Concat(chall).ToArray();
        }

        return head.Concat(query).ToArray();
    }

    /// <summary>
    /// Parses the A2S response.
    /// </summary>
    /// <param name="buffer">The Response</param>
    /// <returns>"players/maxPlayers" or empty on failure.</returns>
    private static string ParseA2SInfoResponse(byte[] buffer)
    {
        // Need at least header + 1 type byte
        if (buffer.Length < 5) return string.Empty;

        int index = 4; // Skips the initial 4 bytes (0xFF 0xFF 0xFF 0xFF)
        byte header = buffer[index++];
        if (header != 0x49) // 'I'
        {
            ConsoleExt.WriteLine($"Invalid response (expected 0x49, got 0x{header:X2}).", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Error);
            return string.Empty;
        }

        if (index >= buffer.Length) return string.Empty;

        byte protocol = buffer[index++];

        string name = ReadNullTerminatedString(buffer, ref index);
        string map = ReadNullTerminatedString(buffer, ref index);
        string folder = ReadNullTerminatedString(buffer, ref index);
        string game = ReadNullTerminatedString(buffer, ref index);

        if (index + 2 > buffer.Length) return string.Empty;
        short appId = BitConverter.ToInt16(buffer, index); index += 2;

        if (index + 3 > buffer.Length) return string.Empty;
        byte players = buffer[index++];
        byte maxPlayers = buffer[index++];
        byte bots = buffer[index];

        ConsoleExt.WriteLine("A2S Info Response:", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Protocol: {protocol}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"App ID: {appId}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Folder: {folder}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);

        ConsoleExt.WriteLine("Server Name: " + name, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine("Map: " + map, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine("Game: " + game, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine($"Players: {players}/{maxPlayers}", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        ConsoleExt.WriteLine("Bots: " + bots, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);

        return $"{players}/{maxPlayers}";
    }

    public void Dispose()
    {
        _udpClient?.Close();
        _udpClient = null;
        _endPoint = null;
    }

    private static string ReadNullTerminatedString(byte[] buffer, ref int index)
    {
        int start = index;
        while (index < buffer.Length && buffer[index] != 0)
            index++;
        string result = Encoding.UTF8.GetString(buffer, start, index - start);
        if (index < buffer.Length) index++; // Skip null byte safely
        return result;
    }

    private static void DumpBytes(byte[] data)
    {
        ConsoleExt.WriteLine("[Hex Dump]", ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
        string output = string.Empty;
        for (int i = 0; i < data.Length; i += 16)
        {
            output = $"{i:X4}: ";
            for (int j = 0; j < 16 && i + j < data.Length; j++)
                output += $"{data[i + j]:X2} ";
            output += " | ";
            for (int j = 0; j < 16 && i + j < data.Length; j++)
            {
                char c = (char)data[i + j];
                output += char.IsControl(c) ? '.' : c;
            }

            output += Environment.NewLine;
        }
        ConsoleExt.WriteLine(output, ConsoleExt.CurrentStep.A2SQuery, ConsoleExt.OutputType.Debug);
    }
}