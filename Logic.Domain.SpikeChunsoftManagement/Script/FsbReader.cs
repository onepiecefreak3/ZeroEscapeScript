using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using System.Text;
using Komponent.IO;
using Logic.Domain.SpikeChunsoftManagement.DataClasses.Script;

namespace Logic.Domain.SpikeChunsoftManagement.Script;

internal class FsbReader : IFsbReader
{
    public Sir0ScriptData Read(Stream input)
    {
        using var br = new BinaryReaderX(input, Encoding.GetEncoding(932), true);

        // Read header data
        var header = ReadHeader(br);

        br.BaseStream.Position = header.indexOffset;
        var index = ReadIndex(br);

        // Read index data
        br.BaseStream.Position = index.nameOffset;
        var name = br.ReadNullTerminatedString();

        br.BaseStream.Position = index.functionOffset;
        var functionEntries = ReadFunctions(br);

        br.BaseStream.Position = index.stringOffset;
        var textOffsets1 = ReadTextOffsets(br);

        br.BaseStream.Position = index.unkOffset1;
        var textOffsets2 = ReadTextOffsets(br);

        br.BaseStream.Position = index.unkOffset2;
        var textOffsets3 = ReadTextOffsets(br);

        // Resolve functions
        var functions = new List<Sir0FunctionData>();

        for (var i = 0; i < functionEntries.Length; i++)
        {
            br.BaseStream.Position = functionEntries[i].nameOffset;
            var functionName = br.ReadNullTerminatedString();

            br.BaseStream.Position = functionEntries[i].codeOffset;
            var codeLength = i + 1 < functionEntries.Length
                ? functionEntries[i + 1].codeOffset - functionEntries[i].codeOffset
                : textOffsets1[0] - functionEntries[i].codeOffset;

            functions.Add(new Sir0FunctionData
            {
                Name = functionName,
                Data = br.ReadBytes(codeLength)
            });
        }

        // Resolve strings
        var texts1 = new List<string>();
        var texts2 = new List<string>();
        var texts3 = new List<string>();

        foreach (int textOffset in textOffsets1)
        {
            br.BaseStream.Position = textOffset;
            texts1.Add(br.ReadNullTerminatedString());
        }
        foreach (int textOffset in textOffsets2)
        {
            br.BaseStream.Position = textOffset;
            texts2.Add(br.ReadNullTerminatedString());
        }
        foreach (int textOffset in textOffsets3)
        {
            br.BaseStream.Position = textOffset;
            texts3.Add(br.ReadNullTerminatedString());
        }

        // Create script data
        return new Sir0ScriptData
        {
            Name = name,
            Functions = [.. functions],
            Strings = [.. texts1],
            ExportedLabels = [.. texts2],
            Texts3 = [.. texts3]
        };
    }

    private Sir0Header ReadHeader(BinaryReaderX reader)
    {
        return new Sir0Header
        {
            magic = reader.ReadString(4),
            indexOffset = reader.ReadInt32(),
            unkOffset = reader.ReadInt32()
        };
    }

    private Sir0IndexTable ReadIndex(BinaryReaderX reader)
    {
        return new Sir0IndexTable
        {
            nameOffset = reader.ReadInt32(),
            functionOffset = reader.ReadInt32(),
            stringCount = reader.ReadInt32(),
            stringOffset = reader.ReadInt32(),
            unkOffset1 = reader.ReadInt32(),
            unkOffset2 = reader.ReadInt32(),
        };
    }

    private Sir0FunctionEntry[] ReadFunctions(BinaryReaderX reader)
    {
        var result = new List<Sir0FunctionEntry>();

        var entry = ReadFunction(reader);
        while (entry.codeOffset is not 0 || entry.nameOffset is not 0)
        {
            result.Add(entry);
            entry = ReadFunction(reader);
        }

        return [.. result];
    }

    private Sir0FunctionEntry ReadFunction(BinaryReaderX reader)
    {
        return new Sir0FunctionEntry
        {
            codeOffset = reader.ReadInt32(),
            nameOffset = reader.ReadInt32()
        };
    }

    private int[] ReadTextOffsets(BinaryReaderX reader)
    {
        var result = new List<int>();

        var entry = reader.ReadInt32();
        while (entry is not 0)
        {
            result.Add(entry);
            entry = reader.ReadInt32();
        }

        return [.. result];
    }
}