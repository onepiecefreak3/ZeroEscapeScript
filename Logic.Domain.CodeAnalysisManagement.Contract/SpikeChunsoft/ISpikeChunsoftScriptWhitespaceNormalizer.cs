using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

public interface ISpikeChunsoftScriptWhitespaceNormalizer
{
    void NormalizeCodeUnit(CodeUnitSyntax codeUnit);
}