namespace Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;

internal class BlockInfo(StatementBlock block)
{
    public StatementBlock Block { get; } = block;
    public byte? TerminalCommand { get; set; }
    public string? JumpLabel { get; set; }
}