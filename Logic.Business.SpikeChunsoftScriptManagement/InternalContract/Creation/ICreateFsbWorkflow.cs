using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;

internal interface ICreateFsbWorkflow
{
    void Create(Stream input, Stream output, Sir0Script donorScript);
}