namespace InstitutionalDiwan.Server.Infrastructure;

public sealed class SeaweedFsOptions
{
    public const string SectionName = "Drive:SeaweedFs";

    public bool Enabled { get; set; }
    public string ExecutablePath { get; set; } = string.Empty;
    public string DataDirectory { get; set; } = string.Empty;
    public int S3Port { get; set; } = 8333;
    public int FilerPort { get; set; } = 8888;
    public int MasterPort { get; set; } = 9333;
    public int VolumePort { get; set; } = 8080;
}
