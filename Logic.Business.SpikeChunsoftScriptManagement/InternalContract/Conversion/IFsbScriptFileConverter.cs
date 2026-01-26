using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;

interface IFsbScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(Sir0Function[] functions);
}