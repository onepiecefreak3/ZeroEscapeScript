using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

public interface ISpikeChunsoftScriptParser
{
    CodeUnitSyntax ParseCodeUnit(string text);
}