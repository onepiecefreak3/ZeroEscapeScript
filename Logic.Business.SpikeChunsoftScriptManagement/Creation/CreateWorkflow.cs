using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using System.Diagnostics.CodeAnalysis;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Creation;

class CreateWorkflow(
    SpikeChunsoftScriptManagementConfiguration config,
    ICreateFsbWorkflow createFsbWorkflow,
    IFsbParser scriptParser)
    : ICreateWorkflow
{
    public void Create()
    {
        bool isDirectory = Directory.Exists(config.InputPath);
        if (isDirectory)
            CreateDirectory(config.InputPath);
        else
            CreateFile(Path.GetFullPath(config.InputPath));
    }

    private void CreateDirectory(string dirPath)
    {
        string[] files = Directory.GetFiles(dirPath, "*.txt", SearchOption.AllDirectories);

        foreach (string file in files)
            CreateFile(file);
    }

    private void CreateFile(string filePath)
    {
        Console.Write($"Compile {filePath}... ");

        bool wasSuccessful = TryCreateFile(filePath, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");
    }

    private bool TryCreateFile(string filePath, [NotNullWhen(false)] out Exception? error)
    {
        error = null;

        using Stream donorStream = File.OpenRead(GetDonorPath(filePath));
        Sir0Script donorScript = scriptParser.Parse(donorStream);

        using Stream inputStream = File.OpenRead(filePath);

        try
        {
            using Stream outputStream = File.Create(filePath + ".fsb");
            createFsbWorkflow.Create(inputStream, outputStream, donorScript);
        }
        catch (Exception e)
        {
            error = e;
            return false;
        }

        return true;
    }

    private static string GetDonorPath(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        return Path.Combine(directory!, Path.GetFileNameWithoutExtension(filePath));
    }

    private static Exception GetInnermostException(Exception e)
    {
        while (e.InnerException != null)
            e = e.InnerException;

        return e;
    }
}