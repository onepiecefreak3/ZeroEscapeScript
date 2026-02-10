namespace Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

public class Sir0ScriptData
{
    public required string Name { get; set; }
    public required Sir0FunctionData[] Functions { get; set; }
    public required string[] Strings { get; set; }
    public required string[] ExportedLabels { get; set; }
    public required string[] GlobalVariables { get; set; }
}