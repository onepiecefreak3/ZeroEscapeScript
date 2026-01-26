using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Script;

public interface IFsbWriter
{
    void Write(Sir0Script script, Stream output);
}