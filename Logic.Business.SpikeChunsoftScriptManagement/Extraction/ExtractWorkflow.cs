using System.Diagnostics.CodeAnalysis;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Extraction;

namespace Logic.Business.SpikeChunsoftScriptManagement.Extraction;

class ExtractWorkflow(
    SpikeChunsoftScriptManagementConfiguration config,
    IExtractFsbWorkflow extractFsbWorkflow)
    : IExtractWorkflow
{
    public void Extract()
    {
        bool isDirectory = Directory.Exists(config.InputPath);
        if (isDirectory)
            ExtractDirectory(config.InputPath);
        else
            ExtractFile(config.InputPath);
    }

    private void ExtractDirectory(string dirPath)
    {
        string[] files = CollectFiles(dirPath);

        foreach (string file in files)
            ExtractFile(file);
    }

    private static string[] CollectFiles(string dirPath)
    {
        IEnumerable<string> files = Directory.EnumerateFiles(dirPath, "*.fsb", SearchOption.AllDirectories);

        return [.. files];
    }

    private void ExtractFile(string filePath)
    {
        Console.Write($"Extract {filePath}... ");

        using Stream inputStream = File.OpenRead(filePath);

        string outputPath = filePath + ".txt";

        using Stream outputStream = File.Create(outputPath);

        bool wasSuccessful = TryExtractFile(inputStream, outputStream, out Exception? error);
        if (wasSuccessful)
        {
            Console.WriteLine("Ok");
            return;
        }

        Console.WriteLine($"Error: {GetInnermostException(error!).Message}");

        outputStream.Close();
        File.Delete(outputPath);
    }

    private bool TryExtractFile(Stream input, Stream output, [NotNullWhen(false)] out Exception? error)
    {
        error = null;

        try
        {
            extractFsbWorkflow.Extract(input, output);
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