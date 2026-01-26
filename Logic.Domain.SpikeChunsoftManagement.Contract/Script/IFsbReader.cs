using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Script;

public interface IFsbReader
{
    Sir0ScriptData Read(Stream input);
}