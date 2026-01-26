namespace Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

public class Sir0Script
{
    public required string Name { get; set; }
    public required Sir0Function[] Functions { get; set; }
    public required string[] Texts2 { get; set; }
    public required string[] Texts3 { get; set; }
    public required byte[] Values { get; set; }
}