namespace JeisAlive.Models;

public enum BoundFileAction : byte
{
    Open = 0,
    Execute = 1
}

public sealed class BoundFile
{
    public string FilePath { get; set; } = "";
    public string FileName => Path.GetFileName(FilePath);
    public BoundFileAction Action { get; set; } = BoundFileAction.Open;
}
