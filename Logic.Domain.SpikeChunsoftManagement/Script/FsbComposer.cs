using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using System.Buffers.Binary;

namespace Logic.Domain.SpikeChunsoftManagement.Script;

internal class FsbComposer : IFsbComposer
{
    public Sir0ScriptData Compose(Sir0Script script)
    {
        var result = new List<Sir0FunctionData>();
        var texts = new Dictionary<string, short>();

        foreach (Sir0Function function in script.Functions)
        {
            bool isLast = script.Functions[^1] == function;

            Dictionary<string, int> jumpOffsets = CreateJumpOffsets(function);
            result.Add(CreateFunction(function, isLast, texts, jumpOffsets));
        }

        return new Sir0ScriptData
        {
            Name = script.Name,
            Functions = [.. result],
            Texts1 = [.. texts.Keys],
            Texts2 = script.Texts2,
            Texts3 = script.Texts3,
            Values = script.Values
        };
    }

    private static Sir0FunctionData CreateFunction(Sir0Function function, bool isLast, Dictionary<string, short> texts, Dictionary<string, int> jumpOffsets)
    {
        var data = new List<byte>(0x100);

        var buffer = new byte[2];

        foreach (Sir0Operation operation in function.Operations)
        {
            if (operation.Command is 0x26)
                data.Add(0x26);

            if (operation.Command is 0x26 or 0x45)
                break;

            switch (operation.Command)
            {
                case 0x02:
                case 0x04:
                case 0x05:
                case 0x06:
                    throw new InvalidOperationException($"Invalid instruction 0x{operation.Command:X2}.");

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
                    data.Add(operation.Command);
                    break;

                case 0x2B:
                case 0x2C:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No byte value was given.");

                    byte byteValue = GetByte(operation.Arguments[0]);

                    data.Add(operation.Command);
                    data.Add(byteValue);
                    break;

                case 0x28:
                case 0x2F:
                case 0x31: //*
                case 0x33:
                case 0x34: //x
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No string value was given.");

                    short stringIndex = GetStringIndex(operation.Arguments[0], texts);
                    BinaryPrimitives.WriteInt16LittleEndian(buffer, stringIndex);

                    data.Add(operation.Command);
                    data.AddRange(buffer);
                    break;

                case 0x2E: //*
                case 0x32:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No short value was given.");

                    short shortValue = GetShort(operation.Arguments[0]);
                    BinaryPrimitives.WriteInt16LittleEndian(buffer, shortValue);

                    data.Add(operation.Command);
                    data.AddRange(buffer);
                    break;

                case 0x35:
                case 0x36:
                case 0x37:
                    if (operation.Arguments.Length <= 0 || operation.Arguments[0] is not string jumpLabel)
                        throw new InvalidOperationException("No jump label was given.");

                    if (!jumpOffsets.TryGetValue(jumpLabel, out int jumpOffset))
                        throw new InvalidOperationException($"Could not resolve jump label {jumpLabel}.");

                    BinaryPrimitives.WriteInt16LittleEndian(buffer, (short)(jumpOffset - data.Count - 3));

                    data.Add(operation.Command);
                    data.AddRange(buffer);
                    break;

                case 0xF0:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No number value was given.");

                    data.Add(0x0D);
                    data.Add(operation.Command);

                    int variableInt = GetVariableInt(operation.Arguments[0]);
                    do
                    {
                        int part = variableInt & 0x7F;
                        if ((variableInt >>= 7) > 0)
                            part |= 0x80;

                        data.Add((byte)part);
                    } while (variableInt > 0);
                    break;

                case 0xF1:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No string value was given.");

                    short stringIndex1 = GetStringIndex(operation.Arguments[0], texts);
                    BinaryPrimitives.WriteInt16LittleEndian(buffer, stringIndex1);

                    data.Add(0x0D);
                    data.Add(operation.Command);
                    data.AddRange(buffer);
                    break;

                case 0xF4:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No string value was given.");

                    short stringIndex2 = GetStringIndex(operation.Arguments[0], texts);
                    BinaryPrimitives.WriteInt16LittleEndian(buffer, stringIndex2);

                    data.Add(0x0D);
                    data.Add(operation.Command);
                    data.AddRange(buffer);

                    if (operation.Arguments.Length > 1)
                    {
                        short stringIndex3 = GetStringIndex(operation.Arguments[1], texts);
                        BinaryPrimitives.WriteInt16LittleEndian(buffer, stringIndex3);
                    }
                    else
                    {
                        Array.Clear(buffer);
                    }

                    data.AddRange(buffer);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown instruction 0x{operation.Command:X2}.");
            }
        }

        if (isLast)
            data.Add(0x45);

        return new Sir0FunctionData
        {
            Name = function.Name,
            Data = [.. data]
        };
    }

    private static Dictionary<string, int> CreateJumpOffsets(Sir0Function function)
    {
        var result = new Dictionary<string, int>();

        var offset = 0;
        foreach (Sir0Operation operation in function.Operations)
        {
            if (operation.Label is not null)
                result[operation.Label] = offset;

            if (operation.Command is 0x26 or 0x45)
                break;

            switch (operation.Command)
            {
                case 0x02:
                case 0x04:
                case 0x05:
                case 0x06:
                    throw new InvalidOperationException($"Invalid instruction 0x{operation.Command:X2}.");

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
                    offset++;
                    break;

                case 0x2B:
                case 0x2C:
                    offset += 2;
                    break;

                case 0x28:
                case 0x2E: //*
                case 0x2F:
                case 0x31: //*
                case 0x32:
                case 0x33:
                case 0x34: //x
                case 0x35:
                case 0x36:
                case 0x37:
                    offset += 3;
                    break;

                case 0xF0:
                    if (operation.Arguments.Length <= 0)
                        throw new InvalidOperationException("No number value was given.");

                    int value = GetVariableInt(operation.Arguments[0]);

                    offset += 3;
                    while ((value >>= 7) > 0)
                        offset++;
                    break;

                case 0xF1:
                    offset += 4;
                    break;

                case 0xF4:
                    offset += 6;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown instruction 0x{operation.Command:X2}.");
            }
        }

        return result;
    }

    private static byte GetByte(object argument)
    {
        int value = argument switch
        {
            float floatValue => (int)floatValue,
            int intValue => intValue,
            _ => throw new InvalidOperationException("Invalid byte value was given.")
        };

        if (value is < 0 or > 255)
            throw new InvalidOperationException("Byte value is out of range.");

        return (byte)value;
    }

    private static short GetShort(object argument)
    {
        int value = argument switch
        {
            float floatValue => (int)floatValue,
            int intValue => intValue,
            _ => throw new InvalidOperationException("Invalid short value was given.")
        };

        if (value is < short.MinValue or > short.MaxValue)
            throw new InvalidOperationException("Short value is out of range.");

        return (short)value;
    }

    private static short GetStringIndex(object argument, Dictionary<string, short> cache)
    {
        if (argument is not string stringValue)
            throw new InvalidOperationException("Invalid string value was given.");

        if (!cache.TryGetValue(stringValue, out short stringIndex))
        {
            stringIndex = (short)cache.Keys.Count;
            cache[stringValue] = stringIndex;
        }

        return stringIndex;
    }

    private static int GetVariableInt(object argument)
    {
        int value = argument switch
        {
            float floatValue => (int)(floatValue * 1024),
            int intValue => intValue * 1024,
            _ => throw new InvalidOperationException("Invalid number value was given.")
        };

        if (value >= 0)
            value <<= 1;
        else
            value = (-(value + 1) << 1) | 1;

        return value;
    }
}