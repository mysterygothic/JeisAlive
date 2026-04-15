namespace JeisAlive.Models;

public enum OutputFormat
{
    NativeExe,
    Batch
}

public sealed class PackerConfig
{
    public string PayloadPath { get; set; } = "";
    public OutputFormat Format { get; set; } = OutputFormat.NativeExe;
    public bool AntiDebug { get; set; } = true;
    public bool AntiVM { get; set; } = true;
    public bool MeltFile { get; set; } = true;
    public List<BoundFile> BoundFiles { get; set; } = new();
    public string OutputPath { get; set; } = "";
}
