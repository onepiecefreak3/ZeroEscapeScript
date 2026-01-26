using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Domain.SpikeChunsoftManagement.Contract.Script;

public interface IFsbParser
{
    Sir0Script Parse(Stream input);
}