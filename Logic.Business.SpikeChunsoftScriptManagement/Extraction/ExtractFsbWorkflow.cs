using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Extraction;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Extraction;

class ExtractFsbWorkflow(
    IFsbParser scriptParser,
    IFsbScriptFileConverter scriptConverter,
    ISpikeChunsoftScriptWhitespaceNormalizer scriptNormalizer,
    ISpikeChunsoftScriptComposer scriptComposer)
    : IExtractFsbWorkflow
{
    public void Extract(Stream input, Stream output)
    {
        // Read script data
        Sir0Script script = scriptParser.Parse(input);

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script);
        scriptNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }
}