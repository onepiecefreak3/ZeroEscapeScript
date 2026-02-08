namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Purification;

internal interface IPurifyFsbWorkflow
{
    void Purify(Stream input, Stream output);
}