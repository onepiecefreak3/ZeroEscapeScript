using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;

internal interface IBlockBuilder
{
    IReadOnlyList<StatementBlock> Build(Sir0Operation[] operations);
}