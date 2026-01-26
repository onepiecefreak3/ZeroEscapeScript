using Logic.Business.SpikeChunsoftScriptManagement.InternalContract;

namespace Logic.Business.SpikeChunsoftScriptManagement;

internal class SpikeChunsoftScriptManagementConfigurationValidator : ISpikeChunsoftScriptManagementConfigurationValidator
{
    public void Validate(SpikeChunsoftScriptManagementConfiguration config)
    {
        if (config.ShowHelp)
            return;

        ValidateOperation(config);
        ValidateFilePath(config);
    }

    private void ValidateOperation(SpikeChunsoftScriptManagementConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Operation))
            throw new InvalidOperationException("No operation mode was given. Specify an operation mode by using the -o argument.");

        if (config.Operation != "e" && config.Operation != "c")
            throw new InvalidOperationException($"The operation mode '{config.Operation}' is not valid. Use -h to see a list of valid operation modes.");
    }

    private void ValidateFilePath(SpikeChunsoftScriptManagementConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.InputPath))
            throw new InvalidOperationException("No file to process was specified. Specify a file by using the -f argument.");

        if (!File.Exists(config.InputPath) && !Directory.Exists(config.InputPath))
            throw new InvalidOperationException($"File or directory '{Path.GetFullPath(config.InputPath)}' was not found.");
    }
}