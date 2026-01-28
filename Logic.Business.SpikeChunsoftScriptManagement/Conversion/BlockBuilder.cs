using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class BlockBuilder : IBlockBuilder
{
    public IReadOnlyList<StatementBlock> CreateStatementBlocks(Sir0Operation[] operations)
    {
        List<StatementBlock> blocks = CreateBlocks(operations, out List<BlockInfo> blockInfos);
        RelateBlocks(blockInfos);

        return blocks;
    }

    private static List<StatementBlock> CreateBlocks(Sir0Operation[] operations, out List<BlockInfo> blockInfos)
    {
        blockInfos = [];

        var blocks = new List<StatementBlock>();

        StatementBlock? currentBlock = null;
        BlockInfo? currentInfo = null;

        for (var index = 0; index < operations.Length; index++)
        {
            Sir0Operation operation = operations[index];

            if (operation.Label is not null)
            {
                if (currentBlock is { Operations.Count: > 0 })
                {
                    currentBlock = null;
                    currentInfo = null;
                }

                EnsureBlock(ref currentBlock, ref currentInfo, blocks, blockInfos, index);

                currentBlock.Labels.Add(operation.Label);
            }

            EnsureBlock(ref currentBlock, ref currentInfo, blocks, blockInfos, index);

            currentBlock.Operations.Add(operation);

            if (!IsBlockTerminator(operation.Command))
                continue;

            currentInfo!.TerminalCommand = operation.Command;
            currentInfo.JumpLabel = GetJumpLabel(operation);
            currentBlock.TerminalCommand = currentInfo.TerminalCommand;
            currentBlock.JumpLabel = currentInfo.JumpLabel;
            currentBlock = null;
            currentInfo = null;
        }

        return blocks;
    }

    private static void EnsureBlock(ref StatementBlock? currentBlock, ref BlockInfo? currentInfo, List<StatementBlock> blocks,
        List<BlockInfo> blockInfos, int instructionIndex)
    {
        if (currentBlock is not null)
            return;

        currentBlock = new StatementBlock
        {
            InstructionIndex = instructionIndex
        };
        currentInfo = new BlockInfo(currentBlock);
        blocks.Add(currentBlock);
        blockInfos.Add(currentInfo);
    }

    private void RelateBlocks(IReadOnlyList<BlockInfo> blockInfos)
    {
        var labelLookup = new Dictionary<string, StatementBlock>(StringComparer.Ordinal);
        foreach (BlockInfo info in blockInfos)
        {
            foreach (string label in info.Block.Labels)
            {
                if (!labelLookup.TryAdd(label, info.Block))
                    throw new InvalidOperationException($"Duplicate jump label {label}.");
            }
        }

        for (var i = 0; i < blockInfos.Count; i++)
        {
            BlockInfo info = blockInfos[i];
            StatementBlock block = info.Block;
            StatementBlock? nextBlock = i + 1 < blockInfos.Count ? blockInfos[i + 1].Block : null;

            if (info.TerminalCommand is 0x26 or 0x30)
            {
                block.IsExit = true;
                continue;
            }

            if (info.TerminalCommand is 0x35 or 0x36 or 0x37)
            {
                if (info.JumpLabel is null)
                    throw new InvalidOperationException($"Missing jump label for operation 0x{info.TerminalCommand:X2}.");

                if (!labelLookup.TryGetValue(info.JumpLabel, out StatementBlock? targetBlock))
                    throw new InvalidOperationException($"Could not resolve jump label {info.JumpLabel}.");

                AddBlockRelation(block, targetBlock);

                if (info.TerminalCommand is 0x36 or 0x37)
                {
                    if (nextBlock is null)
                        block.IsExit = true;
                    else
                        AddBlockRelation(block, nextBlock);
                }

                continue;
            }

            if (nextBlock is null)
            {
                block.IsExit = true;
                continue;
            }

            AddBlockRelation(block, nextBlock);
        }
    }

    private static void AddBlockRelation(StatementBlock parent, StatementBlock? child)
    {
        if (child is null)
            return;

        if (!parent.Children.Contains(child))
            parent.Children.Add(child);

        if (!child.Parents.Contains(parent))
            child.Parents.Add(parent);
    }

    private static bool IsBlockTerminator(byte command)
    {
        return command is 0x26 or 0x30 or 0x35 or 0x36 or 0x37;
    }

    private static string? GetJumpLabel(Sir0Operation operation)
    {
        if (operation.Command is not (0x35 or 0x36 or 0x37))
            return null;

        if (operation.Arguments.Length <= 0 || operation.Arguments[0] is not string jumpLabel)
            throw new InvalidOperationException($"Invalid jump label for operation 0x{operation.Command:X2}.");

        return jumpLabel;
    }
}