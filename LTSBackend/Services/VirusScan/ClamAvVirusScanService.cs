using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LTSBackend.Services.VirusScan;

// ================================================================
// SECURITY
// ================================================================
public class ClamAvVirusScanService(IConfiguration _configuration, ILogger<ClamAvVirusScanService> _logger) : IVirusScanService
{
    private const int ChunkSize = 8192;

    public async Task<VirusScanResult> ScanAsync(Stream fileStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        bool enabled = _configuration.GetValue("VirusScan:Enabled", true);
        if (!enabled)
        {
            // Explicit opt-out (e.g. local dev without clamd installed).
            // Logged loudly on every call so this is never silently the
            // case in a production environment by accident.
            _logger.LogWarning("Virus scanning is DISABLED (VirusScan:Enabled=false) - upload of {FileName} was NOT scanned", originalFileName);
            return VirusScanResult.Clean();
        }

        string host = _configuration.GetValue("VirusScan:ClamAvHost", "localhost") ?? "localhost";
        int port = _configuration.GetValue("VirusScan:ClamAvPort", 3310);
        int timeoutSeconds = _configuration.GetValue("VirusScan:TimeoutSeconds", 30);

        try
        {
            using var client = new TcpClient();
            using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectCts.Token);

            await client.ConnectAsync(host, port, linkedCts.Token);
            client.ReceiveTimeout = timeoutSeconds * 1000;
            client.SendTimeout = timeoutSeconds * 1000;

            using var networkStream = client.GetStream();

            // "zINSTREAM\0" - begin a streamed scan session.
            byte[] command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await networkStream.WriteAsync(command, linkedCts.Token);

            if (fileStream.CanSeek)
            {
                fileStream.Seek(0, SeekOrigin.Begin);
            }

            byte[] buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, ChunkSize), linkedCts.Token)) > 0)
            {
                // Each chunk is prefixed with its length as a 4-byte
                // big-endian (network byte order) integer - this is the
                // clamd INSTREAM wire format, not negotiable.
                byte[] lengthPrefix = BitConverter.GetBytes(bytesRead);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthPrefix);
                }

                await networkStream.WriteAsync(lengthPrefix, linkedCts.Token);
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), linkedCts.Token);
            }

            // Zero-length chunk signals end of stream.
            byte[] terminator = new byte[4];
            await networkStream.WriteAsync(terminator, linkedCts.Token);

            using var reader = new StreamReader(networkStream, Encoding.ASCII, leaveOpen: true);
            string? response = await reader.ReadLineAsync(linkedCts.Token);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("Virus scan of {FileName} got an empty response from clamd at {Host}:{Port}", originalFileName, host, port);
                return VirusScanResult.ScanFailed("No response from virus scanner.");
            }

            _logger.LogInformation("Virus scan result for {FileName}: {Response}", originalFileName, response);

            if (response.Contains("FOUND", StringComparison.Ordinal))
            {
                // Format: "stream: <ThreatName> FOUND"
                string threatName = response.Replace("stream:", "").Replace("FOUND", "").Trim();
                _logger.LogWarning("MALWARE DETECTED in upload {FileName}: {ThreatName}", originalFileName, threatName);
                return VirusScanResult.Infected(threatName);
            }

            if (response.Contains("ERROR", StringComparison.Ordinal))
            {
                _logger.LogError("Virus scanner returned an error for {FileName}: {Response}", originalFileName, response);
                return VirusScanResult.ScanFailed(response);
            }

            // "stream: OK"
            return VirusScanResult.Clean();
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Virus scan of {FileName} timed out after {Timeout}s (host {Host}:{Port})", originalFileName, timeoutSeconds, host, port);
            return VirusScanResult.ScanFailed("Virus scan timed out.");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Could not reach ClamAV daemon at {Host}:{Port} while scanning {FileName} - is clamd running? See VIRUS_SCAN_SETUP.md", host, port, originalFileName);
            return VirusScanResult.ScanFailed("Virus scanner is unreachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning {FileName} for viruses", originalFileName);
            return VirusScanResult.ScanFailed("Unexpected error during virus scan.");
        }
    }
}
