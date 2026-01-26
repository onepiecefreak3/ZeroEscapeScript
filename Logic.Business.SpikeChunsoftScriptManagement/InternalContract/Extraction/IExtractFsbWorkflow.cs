namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Extraction;

internal interface IExtractFsbWorkflow
{
    void Extract(Stream input, Stream output);
}