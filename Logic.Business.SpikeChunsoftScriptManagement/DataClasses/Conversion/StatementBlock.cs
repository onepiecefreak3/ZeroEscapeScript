using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;

internal class StatementBlock
{
    public IList<StatementBlock> Parents { get; set; } = [];

    public IList<StatementBlock> Children { get; set; } = [];

    public int InstructionIndex { get; set; } = -1;

    public bool IsExit { get; set; }

    public HashSet<string> Labels { get; set; } = [];

    public byte? TerminalCommand { get; set; }

    public string? JumpLabel { get; set; }

    public IList<Sir0Operation> Operations { get; set; } = [];
}