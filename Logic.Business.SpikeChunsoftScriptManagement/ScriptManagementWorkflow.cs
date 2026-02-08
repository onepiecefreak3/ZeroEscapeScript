using Logic.Business.SpikeChunsoftScriptManagement.Contract;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Extraction;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Purification;

namespace Logic.Business.SpikeChunsoftScriptManagement;

internal class ScriptManagementWorkflow(
    SpikeChunsoftScriptManagementConfiguration config,
    ISpikeChunsoftScriptManagementConfigurationValidator configValidator,
    IExtractWorkflow extractWorkflow,
    ICreateWorkflow createWorkflow,
    IPurifyWorkflow purifyWorkflow)
    : IScriptManagementWorkflow
{
    public int Execute()
    {
        if (config.ShowHelp || Environment.GetCommandLineArgs().Length <= 0)
        {
            PrintHelp();
            return 0;
        }

        if (!TryValidateConfig())
        {
            PrintHelp();
            return 0;
        }

        switch (config.Operation)
        {
            case "e":
                extractWorkflow.Extract();
                break;

            case "c":
                createWorkflow.Create();
                break;

            case "p":
                purifyWorkflow.Purify();
                break;
        }

        return 0;
    }

    private bool TryValidateConfig()
    {
        try
        {
            configValidator.Validate(config);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine();

            return false;
        }

        return true;
    }

    private void PrintHelp()
    {
        Console.WriteLine("Following commands exist:");
        Console.WriteLine("  -h, --help\t\tShows this help message.");
        Console.WriteLine("  -o, --operation\tThe operation to take on the file");
        Console.WriteLine("    Valid operations are: e for extraction, c for creation");
        Console.WriteLine("  -f, --file\t\tThe file to process");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"\tExtract any script to human readable text: {Environment.ProcessPath} -o e -f Path/To/File.fsb");
        Console.WriteLine($"\tCreate a script from human readable text: {Environment.ProcessPath} -o c -f Path/To/File.fsb");
    }
}