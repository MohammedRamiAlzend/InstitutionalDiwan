using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Server.Infrastructure;

public sealed class SeaweedFsHostedService : IHostedService
{
    private readonly SeaweedFsOptions _options;
    private readonly ILogger<SeaweedFsHostedService> _logger;
    private Process? _process;

    public SeaweedFsHostedService(
        IOptions<SeaweedFsOptions> options,
        ILogger<SeaweedFsHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ExecutablePath) ||
            !File.Exists(_options.ExecutablePath))
        {
            _logger.LogWarning(
                "SeaweedFS startup is enabled but {ExecutablePath} does not exist.",
                _options.ExecutablePath);
            return;
        }

        if (await IsPortOpenAsync(_options.S3Port, cancellationToken))
        {
            _logger.LogInformation(
                "SeaweedFS S3 endpoint is already available on port {Port}.",
                _options.S3Port);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.DataDirectory))
        {
            _logger.LogWarning(
                "SeaweedFS startup is enabled but Drive:SeaweedFs:DataDirectory is empty.");
            return;
        }

        Directory.CreateDirectory(_options.DataDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("server");
        startInfo.ArgumentList.Add($"--dir={_options.DataDirectory}");
        startInfo.ArgumentList.Add("--ip=127.0.0.1");
        startInfo.ArgumentList.Add("--ip.bind=127.0.0.1");
        startInfo.ArgumentList.Add($"--master.port={_options.MasterPort}");
        startInfo.ArgumentList.Add($"--volume.port={_options.VolumePort}");
        startInfo.ArgumentList.Add("--filer=true");
        startInfo.ArgumentList.Add($"--filer.port={_options.FilerPort}");
        startInfo.ArgumentList.Add("--s3");
        startInfo.ArgumentList.Add("--s3.ip.bind=127.0.0.1");
        startInfo.ArgumentList.Add($"--s3.port={_options.S3Port}");

        _process = Process.Start(startInfo);
        if (_process is null)
        {
            _logger.LogWarning("Failed to start the configured SeaweedFS process.");
            return;
        }

        for (var attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_process.HasExited)
            {
                _logger.LogError(
                    "SeaweedFS exited during startup with code {ExitCode}.",
                    _process.ExitCode);
                return;
            }

            if (await IsPortOpenAsync(_options.S3Port, cancellationToken))
            {
                _logger.LogInformation(
                    "SeaweedFS started at http://127.0.0.1:{Port}",
                    _options.S3Port);
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        _logger.LogWarning(
            "SeaweedFS did not open S3 port {Port} within the startup timeout.",
            _options.S3Port);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false })
        {
            _logger.LogInformation("Stopping SeaweedFS process started by the API.");
            _process.Kill(entireProcessTree: true);
        }

        _process?.Dispose();
        _process = null;
        return Task.CompletedTask;
    }

    private static async Task<bool> IsPortOpenAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
