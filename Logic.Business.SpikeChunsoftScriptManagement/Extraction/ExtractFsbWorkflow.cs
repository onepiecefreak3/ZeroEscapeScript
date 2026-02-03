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
    //private readonly Dictionary<int, HashSet<string>> _invocationLookup = [];

    public void Extract(Stream input, Stream output)
    {
        // Read script data
        Sir0Script script = scriptParser.Parse(input);

        //foreach (var function in script.Functions)
        //{
        //    Sir0Operation? functionNameOperation = null;
        //    for (var i = 0; i < function.Operations.Length; i++)
        //    {
        //        var operation = function.Operations[i];
        //        if (operation.Command is not 36 and not 35)
        //            continue;

        //        if (operation.Command is 35)
        //        {
        //            if (i - 1 < 0)
        //                continue;

        //            functionNameOperation = function.Operations[i - 1];
        //            continue;
        //        }

        //        if (functionNameOperation is null)
        //            continue;

        //        string name = functionNameOperation.Arguments.Length != 2
        //            ? (string)functionNameOperation.Arguments[0]
        //            : (string)functionNameOperation.Arguments[0] + "::" + (string)functionNameOperation.Arguments[1];

        //        if (i + 1 >= function.Operations.Length || function.Operations[i + 1].Command is not 39)
        //        {
        //            if (!_invocationLookup.TryGetValue(function.Operations[i + 1].Command, out var lookup))
        //                _invocationLookup[function.Operations[i + 1].Command] = lookup = [];

        //            lookup.Add(name);
        //            continue;
        //        }

        //        if (!_invocationLookup.TryGetValue(39, out var lookup1))
        //            _invocationLookup[39] = lookup1 = [];

        //        lookup1.Add(name);

        //        // '?' or '@' or ':' or '~'or '^' or '$' or '&'
        //    }
        //}

        // Convert to readable script
        CodeUnitSyntax codeUnit = scriptConverter.CreateCodeUnit(script.Functions);
        scriptNormalizer.NormalizeCodeUnit(codeUnit);

        string readableScript = scriptComposer.ComposeCodeUnit(codeUnit);

        // Write readable script
        using StreamWriter streamWriter = new(output);

        streamWriter.Write(readableScript);
    }
}