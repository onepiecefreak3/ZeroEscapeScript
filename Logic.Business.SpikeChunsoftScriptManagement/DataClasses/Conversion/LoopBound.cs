using Logic.Business.SpikeChunsoftScriptManagement.Enums.Conversion;

namespace Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;

internal readonly record struct LoopBound(int EndIndex, LoopConditionKind ConditionKind);