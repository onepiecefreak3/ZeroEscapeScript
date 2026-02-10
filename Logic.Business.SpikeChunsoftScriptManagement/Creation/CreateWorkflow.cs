using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;
using System.Diagnostics.CodeAnalysis;

namespace Logic.Business.SpikeChunsoftScriptManagement.Creation;

class CreateWorkflow(
    SpikeChunsoftScriptManagementConfiguration config,
    ICreateFsbWorkflow createFsbWorkflow)
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

        using Stream inputStream = File.OpenRead(filePath);

        try
        {
            using Stream outputStream = File.Create(filePath + ".fsb");
            createFsbWorkflow.Create(inputStream, outputStream);
        }
        catch (Exception e)
        {
            error = e;
            return false;
        }

        return true;
    }

    private static Exception GetInnermostException(Exception e)
    {
        while (e.InnerException != null)
            e = e.InnerException;

        return e;
    }
}