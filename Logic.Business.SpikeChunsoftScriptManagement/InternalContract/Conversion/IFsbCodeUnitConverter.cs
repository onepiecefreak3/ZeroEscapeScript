using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;

public interface IFsbCodeUnitConverter
{
    Sir0Function[] CreateScriptFile(CodeUnitSyntax tree, HashSet<string> exportedLabels, out string name);
}