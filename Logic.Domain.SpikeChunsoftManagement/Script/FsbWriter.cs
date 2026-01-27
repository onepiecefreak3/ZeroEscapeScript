using Komponent.IO;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using System.Text;

namespace Logic.Domain.SpikeChunsoftManagement.Script;

internal class FsbWriter(IFsbComposer scriptComposer) : IFsbWriter
{
    public void Write(Sir0Script script, Stream output)
    {
        Sir0ScriptData data = scriptComposer.Compose(script);

        Write(data, output);
    }

    private static void Write(Sir0ScriptData data, Stream output)
    {
        using var writer = new BinaryWriterX(output, Encoding.GetEncoding(932), true);

        writer.BaseStream.Position = 0x10;

        int[] functionOffsets = WriteFunctions(writer, data.Functions);

        int text1Offset = WriteStrings(writer, data.Texts1);
        int text2Offset = WriteStrings(writer, data.Texts2);
        int text3Offset = WriteStrings(writer, data.Texts3);

        int functionOffset = WriteFunctionEntries(writer, data.Functions, functionOffsets);

        int indexOffset = WriteIndex(writer, data, text1Offset, text2Offset, text3Offset, functionOffset);
        int valueOffset = WriteOffsetCorrections(writer, data, text1Offset, text2Offset, text3Offset, functionOffset, indexOffset);

        writer.BaseStream.Position = 0;
        WriteHeader(writer, indexOffset, valueOffset);
    }

    private static int[] WriteFunctions(BinaryWriterX writer, Sir0FunctionData[] functions)
    {
        var result = new int[functions.Length];

        for (var i = 0; i < functions.Length; i++)
        {
            result[i] = (int)writer.BaseStream.Position;
            writer.Write(functions[i].Data);
        }

        writer.WriteAlignment(4, 0xaa);

        return result;
    }

    private static int WriteStrings(BinaryWriterX writer, string[] texts)
    {
        var offsets = new int[texts.Length];

        for (var i = 0; i < texts.Length; i++)
        {
            offsets[i] = (int)writer.BaseStream.Position;
            writer.WriteString(texts[i]);
        }

        writer.WriteAlignment(4, 0xaa);

        var stringOffset = (int)writer.BaseStream.Position;

        foreach (int offset in offsets)
            writer.Write(offset);

        writer.Write(0);

        return stringOffset;
    }

    private static int WriteFunctionEntries(BinaryWriterX writer, Sir0FunctionData[] functions, int[] functionOffsets)
    {
        var nameOffsets = new int[functions.Length];

        for (var i = 0; i < functions.Length; i++)
        {
            nameOffsets[i] = (int)writer.BaseStream.Position;
            writer.WriteString(functions[i].Name);
        }

        writer.WriteAlignment(4, 0xaa);

        var functionOffset = (int)writer.BaseStream.Position;

        for (var i = 0; i < functions.Length; i++)
        {
            writer.Write(functionOffsets[i]);
            writer.Write(nameOffsets[i]);
        }

        writer.Write(0);
        writer.Write(0);

        return functionOffset;
    }

    private static int WriteIndex(BinaryWriterX writer, Sir0ScriptData data, int text1Offset, int text2Offset, int text3Offset, int functionOffset)
    {
        var nameOffset = (int)writer.BaseStream.Position;
        writer.WriteString(data.Name);

        writer.WriteAlignment(4, 0xaa);

        var indexOffset = (int)writer.BaseStream.Position;

        writer.Write(nameOffset);
        writer.Write(functionOffset);
        writer.Write(data.Texts1.Length);
        writer.Write(text1Offset);
        writer.Write(text2Offset);
        writer.Write(text3Offset);

        writer.WriteAlignment(0x10, 0xaa);

        return indexOffset;
    }

    private static int WriteOffsetCorrections(BinaryWriterX writer, Sir0ScriptData data, int text1Offset, int text2Offset, int text3Offset, int functionOffset, int indexOffset)
    {
        var valueOffset = (int)writer.BaseStream.Position;

        writer.Write((byte)4);
        writer.Write((byte)4);

        WriteVariableInt(writer, text1Offset - 8);
        for (var i = 0; i < data.Texts1.Length - 1; i++)
            writer.Write((byte)4);

        WriteVariableInt(writer, text2Offset - (text1Offset + data.Texts1.Length * 4) + 4);
        for (var i = 0; i < data.Texts2.Length - 1; i++)
            writer.Write((byte)4);

        WriteVariableInt(writer, text3Offset - (text2Offset + data.Texts2.Length * 4) + 4);
        for (var i = 0; i < data.Texts3.Length - 1; i++)
            writer.Write((byte)4);

        WriteVariableInt(writer, functionOffset - (text3Offset + data.Texts3.Length * 4) + 4);
        for (var i = 0; i < data.Functions.Length * 2 - 1; i++)
            writer.Write((byte)4);

        WriteVariableInt(writer, indexOffset - (functionOffset + data.Functions.Length * 8) + 4);

        writer.Write((byte)4);
        writer.Write((byte)8);
        writer.Write((byte)4);
        writer.Write((byte)4);
        writer.Write((byte)0);
        writer.WriteAlignment(0x10, 0xaa);

        return valueOffset;
    }

    private static void WriteHeader(BinaryWriterX writer, int indexOffset, int valueOffset)
    {
        writer.WriteString("SIR0", writeNullTerminator: false);
        writer.Write(indexOffset);
        writer.Write(valueOffset);
    }

    private static void WriteVariableInt(BinaryWriterX writer, int value)
    {
        var bytes = new List<byte>(5);
        do
        {
            bytes.Add((byte)(value & 0x7F));
            value >>= 7;
        } while (value > 0);

        for (int i = bytes.Count - 1; i >= 0; i--)
        {
            byte part = bytes[i];

            if (i > 0)
                part |= 0x80;

            writer.Write(part);
        }
    }
}