using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;

class StatementBlock
{
    public IList<StatementBlock> Parents { get; set; } = [];

    public IList<StatementBlock> Children { get; set; } = [];

    public int InstructionIndex { get; set; } = -1;

    public bool IsExit { get; set; }

    public HashSet<string> Labels { get; set; } = [];

    public IList<StatementSyntax> Statements { get; set; } = [];
}