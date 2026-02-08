using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Creation;

class CreateFsbWorkflow(
    ISpikeChunsoftScriptParser scriptParser,
    IFsbCodeUnitConverter treeConverter,
    IFsbWriter scriptWriter)
    : ICreateFsbWorkflow
{
    public void Create(Stream input, Stream output, Sir0Script donorScript)
    {
        // Read readable script
        using StreamReader streamReader = new(input);

        string readableScript = streamReader.ReadToEnd();

        // Convert to script data
        var exportedLabels = new HashSet<string>();

        CodeUnitSyntax codeUnit = scriptParser.ParseCodeUnit(readableScript);
        Sir0Function[] functions = treeConverter.CreateScriptFile(codeUnit, exportedLabels, out string name);

        donorScript.Name = name;
        donorScript.Functions = functions;
        donorScript.ExportedLabels = [.. exportedLabels];

        // Write script data
        scriptWriter.Write(donorScript, output);
    }
}