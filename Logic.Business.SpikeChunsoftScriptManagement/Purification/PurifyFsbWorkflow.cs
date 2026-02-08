using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Purification;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Purification;

class PurifyFsbWorkflow(
    IFsbParser scriptParser,
    IFsbWriter scriptWriter)
    : IPurifyFsbWorkflow
{
    public void Purify(Stream input, Stream output)
    {
        // Read script data
        Sir0Script script = scriptParser.Parse(input);

        // Clear out unconditional jumps
        for (var i = 0; i < script.Functions.Length; i++)
        {
            Sir0Function function = script.Functions[i];

            List<Sir0Operation> operations = function.Operations.ToList();
            for (var j = 0; j < operations.Count; j++)
            {
                if (operations[j].Command is not 0x35)
                    continue;

                if (j + 1 >= operations.Count)
                    continue;

                string? currentLabel = operations[j].Label;

                if (operations[j + 1].Label == (string)operations[j].Arguments[0])
                {
                    operations.RemoveAt(j--);

                    if (currentLabel is not null)
                    {
                        for (var h = 0; h < operations.Count; h++)
                        {
                            if (operations[h].Command is not (0x35 or 0x36 or 0x37))
                                continue;

                            if ((string)operations[h].Arguments[0] == currentLabel)
                                operations[h] = operations[h] with { Arguments = [operations[j + 1].Label!] };
                        }
                    }
                }
            }

            script.Functions[i] = function with { Operations = [.. operations] };
        }

        // Write script data
        scriptWriter.Write(script, output);
    }
}