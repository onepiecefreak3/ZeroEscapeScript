using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using System.Buffers.Binary;

namespace Logic.Domain.SpikeChunsoftManagement.Script;

internal class FsbParser(IFsbReader reader) : IFsbParser
{
    public Sir0Script Parse(Stream input)
    {
        Sir0ScriptData scriptData = reader.Read(input);
        Sir0Function[] functions = CreateFunctions(scriptData);

        return new Sir0Script
        {
            Name = scriptData.Name,
            Functions = functions,
            Texts2 = scriptData.Texts2,
            Texts3 = scriptData.Texts3
        };
    }

    private static Sir0Function[] CreateFunctions(Sir0ScriptData scriptData)
    {
        var result = new List<Sir0Function>();

        foreach (Sir0FunctionData functionData in scriptData.Functions)
        {
            Dictionary<int, string> jumpLabels = CreateJumpLabels(functionData.Data);
            result.Add(CreateFunction(scriptData, functionData, jumpLabels));
        }

        return [.. result];
    }

    private static Sir0Function CreateFunction(Sir0ScriptData scriptData, Sir0FunctionData functionData, Dictionary<int, string> jumpLabels)
    {
        var operations = new List<Sir0Operation>();

        for (var i = 0; i < functionData.Data.Length;)
        {
            jumpLabels.TryGetValue(i, out string? jumpLabel);

            if (functionData.Data[i] is 0x26)
                operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i], []));

            if (functionData.Data[i] is 0x26 or 0x45)
                break;

            switch (functionData.Data[i])
            {
                case 0x02:
                case 0x04:
                case 0x05:
                case 0x06:
                    throw new InvalidOperationException($"Invalid instruction 0x{functionData.Data[i]:X2}.");

                case 0x01:
                case 0x03: //*
                case 0x07:
                case 0x08: //*
                case 0x09: //*
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0E: //*
                case 0x0F:
                case 0x10: //*
                case 0x11: //*
                case 0x12:
                case 0x13: //*
                case 0x14: //*
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x27:
                case 0x29: //x
                case 0x2A: //x
                case 0x2D: //x
                case 0x30:
                    operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i++], []));
                    break;

                case 0x2B:
                case 0x2C:
                    operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i++], [(int)functionData.Data[i++]]));
                    break;

                case 0x28:
                case 0x2F:
                case 0x31: //*
                case 0x33:
                case 0x34:
                    i += 3;
                    int index3 = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 2));
                    operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i - 3], [scriptData.Texts1[index3]]));
                    break;

                case 0x2E: //*
                case 0x32:
                    i += 3;
                    int value = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 2));
                    operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i - 3], [value]));
                    break;

                case 0x35:
                case 0x36:
                case 0x37:
                    i += 3;
                    int offset = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 2));

                    if (!jumpLabels.TryGetValue(i + offset, out string? jumpLabel1))
                        throw new InvalidOperationException($"Could not resolve jump to offset {offset}.");

                    operations.Add(new Sir0Operation(jumpLabel, functionData.Data[i - 3], [jumpLabel1]));
                    break;

                case 0x0D:
                    switch (functionData.Data[i + 1])
                    {
                        case 0xF0:
                            i += 2;
                            operations.Add(new Sir0Operation(jumpLabel, 0xF0, [ReadFloat(functionData.Data, ref i)]));
                            break;

                        case 0xF1:
                            i += 4;
                            int index = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 2));
                            operations.Add(new Sir0Operation(jumpLabel, 0xF1, [scriptData.Texts1[index]]));
                            break;

                        case 0xF4:
                            i += 6;
                            int index1 = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 4));
                            int index2 = BinaryPrimitives.ReadInt16LittleEndian(functionData.Data.AsSpan(i - 2));
                            operations.Add(index2 != 0
                                ? new Sir0Operation(jumpLabel, 0xF4, [scriptData.Texts1[index1], scriptData.Texts1[index2]])
                                : new Sir0Operation(jumpLabel, 0xF4, [scriptData.Texts1[index1]]));
                            break;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown instruction 0x{functionData.Data[i]:X2}.");
            }
        }

        return new Sir0Function(functionData.Name, [.. operations]);
    }

    private static Dictionary<int, string> CreateJumpLabels(byte[] functionData)
    {
        var result = new Dictionary<int, string>();

        for (var i = 0; i < functionData.Length;)
        {
            if (functionData[i] is 0x26 or 0x45)
                break;

            switch (functionData[i])
            {
                case 0x02:
                case 0x04:
                case 0x05:
                case 0x06:
                    throw new InvalidOperationException($"Invalid instruction 0x{functionData[i]:X2}.");

                case 0x01:
                case 0x03: //*
                case 0x07:
                case 0x08: //*
                case 0x09: //*
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0E: //*
                case 0x0F:
                case 0x10: //*
                case 0x11: //*
                case 0x12:
                case 0x13: //*
                case 0x14: //*
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                case 0x1F:
                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x27:
                case 0x29: //x
                case 0x2A: //x
                case 0x2D: //x
                case 0x30:
                    i++;
                    break;

                case 0x2B:
                case 0x2C:
                    i += 2;
                    break;

                case 0x28:
                case 0x2E: //*
                case 0x2F:
                case 0x31: //*
                case 0x32:
                case 0x33:
                case 0x34:
                    i += 3;
                    break;

                case 0x35:
                case 0x36:
                case 0x37:
                    i += 3;
                    int offset = BinaryPrimitives.ReadInt16LittleEndian(functionData.AsSpan(i - 2));
                    if (!result.ContainsKey(i + offset))
                        result[i + offset] = $"@{result.Keys.Count:000}@";
                    break;

                case 0x0D:
                    switch (functionData[i + 1])
                    {
                        case 0xF0:
                            i += 2;

                            for (int local = i; local < local + 5;)
                            {
                                i++;
                                if (functionData[local++] < 0x80)
                                    break;
                            }
                            break;

                        case 0xF1:
                            i += 4;
                            break;

                        case 0xF4:
                            i += 6;
                            break;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown instruction 0x{functionData[i]:X2}.");
            }
        }

        return result;
    }

    private static float ReadFloat(byte[] data, ref int index)
    {
        int value = ReadInt(data, ref index);
        return value / 1024f;
    }

    private static int ReadInt(byte[] data, ref int index)
    {
        int value = ReadVariableInt(data, ref index);

        if ((value & 0x1) == 0)
            return value >> 1;

        return -(value >> 1) - 1;
    }

    private static int ReadVariableInt(byte[] data, ref int index)
    {
        int result = data[index] & 0x7F;

        if (data[index++] < 0x80)
            return result;

        int value = data[index] & 0x7F;
        result |= value << 7;

        if (data[index++] < 0x80)
            return result;

        value = data[index] & 0x7F;
        result |= value << 14;

        if (data[index++] < 0x80)
            return result;

        value = data[index] & 0x7F;
        result |= value << 21;

        if (data[index++] < 0x80)
            return result;

        value = data[index++];
        result |= value << 28;

        return result;
    }
}