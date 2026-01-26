using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

public interface ISpikeChunsoftScriptComposer
{
    string ComposeCodeUnit(CodeUnitSyntax codeUnit);
}