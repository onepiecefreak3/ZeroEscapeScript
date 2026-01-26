using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Script;

public interface IFsbComposer
{
    Sir0ScriptData Compose(Sir0Script script);
}