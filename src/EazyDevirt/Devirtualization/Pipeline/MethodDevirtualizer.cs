using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Core.Crypto;
using EazyDevirt.Core.IO;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using EazyDevirt.PatternMatching.Patterns.OpCodes;
using EazyDevirt.Devirtualization.Options;

namespace EazyDevirt.Devirtualization.Pipeline;

internal class MethodDevirtualizer : StageBase
{
    private CryptoStreamV3 VMStream { get; set; }
    private VMBinaryReader VMStreamReader { get; set; }
    private VMBinaryReader _currentReader;
    private readonly Stack<VMBinaryReader> _readerStack = new();

    private Resolver Resolver { get; set; }

    public override bool Run()
    {
        if (!Init()) return false;

        VMStream = new CryptoStreamV3(Ctx.VMStream, Ctx.MethodCryptoKey, true);
        VMStreamReader = new VMBinaryReader(VMStream);
        _currentReader = VMStreamReader;

        Resolver = new Resolver(Ctx);
        foreach (var vmMethod in Ctx.VMMethods)
        {
            VMStream.Seek(vmMethod.MethodKey, SeekOrigin.Begin);

            ReadVMMethod(vmMethod);

            if (Ctx.Options.VeryVerbose)
                Ctx.Console.Info(vmMethod);
        }

        VMStreamReader.Dispose();
        return true;
    }

    private void ReadVMMethod(VMMethod vmMethod)
    {
        vmMethod.MethodInfo = new VMMethodInfo(VMStreamReader);

        ReadExceptionHandlers(vmMethod);

        vmMethod.MethodInfo.DeclaringType = Resolver.ResolveType(vmMethod.MethodInfo.VMDeclaringType)!;
        vmMethod.MethodInfo.ReturnType = Resolver.ResolveType(vmMethod.MethodInfo.VMReturnType)!;

        ResolveLocalsAndParameters(vmMethod);

        ReadInstructions(vmMethod);

        ResolveBranchTargets(vmMethod);
        ResolveExceptionHandlers(vmMethod);
        
        // recompile method
        vmMethod.Parent.CilMethodBody!.LocalVariables.Clear();
        vmMethod.Locals.ForEach(x => vmMethod.Parent.CilMethodBody.LocalVariables.Add(x));

        vmMethod.Parent.CilMethodBody!.ExceptionHandlers.Clear();
        vmMethod.ExceptionHandlers.ForEach(x => vmMethod.Parent.CilMethodBody.ExceptionHandlers.Add(x));

        vmMethod.Parent.CilMethodBody.Instructions.Clear();
        vmMethod.Instructions.ForEach(x => vmMethod.Parent.CilMethodBody.Instructions.Add(x));
        
        vmMethod.Parent.CilMethodBody!.VerifyLabelsOnBuild = false;
        vmMethod.Parent.CilMethodBody!.ComputeMaxStackOnBuild = false;
        if (vmMethod.SuccessfullyDevirtualized && !Ctx.Options.NoVerify)
        {
            vmMethod.Parent.CilMethodBody!.ComputeMaxStack(false);
            vmMethod.Parent.CilMethodBody!.VerifyLabels(false);
        }
    }

    private void ReadExceptionHandlers(VMMethod vmMethod)
    {
        vmMethod.VMExceptionHandlers = new List<VMExceptionHandler>(VMStreamReader.ReadInt16());
        for (var i = 0; i < vmMethod.VMExceptionHandlers.Capacity; i++)
            vmMethod.VMExceptionHandlers.Add(new VMExceptionHandler(VMStreamReader));

        vmMethod.VMExceptionHandlers.Sort((first, second) =>
            first.TryStart == second.TryStart
                ? second.TryLength.CompareTo(first.TryLength)
                : first.TryStart.CompareTo(second.TryStart));
    }

    private void ResolveLocalsAndParameters(VMMethod vmMethod)
    {
        vmMethod.Locals = new List<CilLocalVariable>();
        foreach (var local in vmMethod.MethodInfo.VMLocals)
        {
            var type = Resolver.ResolveType(local.VMType)!;
            vmMethod.Locals.Add(new CilLocalVariable(type.ToTypeSignature()));

            // if (Ctx.Options.VeryVeryVerbose)
            //     Ctx.Console.Info($"[{vmMethod.MethodInfo.Name}] Local: {type.Name}");
        }

        // the parameters should already be the correct types and in the correct order so we don't need to resolve those
    }

    private void ReadInstructions(VMMethod vmMethod)
    {
        vmMethod.Instructions = new List<CilInstruction>();
        vmMethod.CodeSize = _currentReader.ReadInt32();
        vmMethod.InitialCodeStreamPosition = VMStream.Position;
        vmMethod.SuccessfullyDevirtualized = true;
        vmMethod.VmToCilOffsetMap = new Dictionary<uint, int>();
        var cilOffset = 0;
        var finalPosition = VMStream.Position + vmMethod.CodeSize;
        while (VMStream.Position < finalPosition)
        {
            if (vmMethod.HasHomomorphicEncryption && vmMethod.HMEndPositionStack.TryPeek(out var endPosition))
                vmMethod.CurrentVirtualOffset = (uint)(endPosition - (_currentReader.BaseStream.Length - _currentReader.BaseStream.Position) + vmMethod.HMEndPositionStack.Count * 8 - 4);
            else
                vmMethod.CurrentVirtualOffset = (uint)(vmMethod.CodeSize - (finalPosition - VMStream.Position));
            
            var virtualOpCode = _currentReader.ReadInt32Special();
            var vmOpCode = Ctx.PatternMatcher.GetOpCodeValue(virtualOpCode);
            if (!vmOpCode.HasVirtualCode)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Instruction {vmMethod.Instructions.Count}, VM opcode [{virtualOpCode}] not found!");

                var vmStart = (uint)vmMethod.CurrentVirtualOffset;
                var nop = new CilInstruction(CilOpCodes.Nop)
                {
                    Offset = cilOffset
                };
                vmMethod.VmToCilOffsetMap[vmStart] = cilOffset;
                vmMethod.Instructions.Add(nop);
                cilOffset += nop.Size;
                continue;
            }

            if (!vmOpCode.IsIdentified)
            {
                if (Ctx.Options.VeryVerbose)
                    Ctx.Console.Warning($"[{vmMethod.Parent.MetadataToken}] Instruction {vmMethod.Instructions.Count} vm opcode not identified [{vmOpCode}]");

                vmMethod.SuccessfullyDevirtualized = false;
            }

            if (vmOpCode is { IsSpecial: true, CilOpCode: null })
            {
                vmOpCode.CilOpCode = ResolveSpecialCilOpCode(vmOpCode, vmMethod);
                if (vmOpCode.CilOpCode != null && Ctx.Options.VeryVerbose)
                    Ctx.Console.InfoStr($"Resolved special opcode {vmOpCode.SpecialOpCode.ToString()!} to CIL opcode {vmOpCode.CilOpCode.ToString()}", vmMethod.Parent.MetadataToken);
            }

            var operand = vmOpCode.IsSpecial ? ReadSpecialOperand(vmOpCode, vmMethod) : ReadOperand(vmOpCode, vmMethod);
            if (vmOpCode.CilOpCode != null)
            {
                // Log these for now since they're special cases.
                if (vmOpCode.CilOpCode.Value.Mnemonic.StartsWith("stind"))
                    Ctx.Console.Warning($"Placing stind instruction at #{vmMethod.Instructions.Count}");
                else if (vmOpCode.SpecialOpCode == SpecialOpCodes.NoBody)
                    Ctx.Console.Warning($"Placing NoBody instruction at #{vmMethod.Instructions.Count}");

                if (vmOpCode.CilOpCode.Value.OperandType != CilOperandType.InlineNone && operand == null)
                    Ctx.Console.Warning($"[{vmMethod.Parent.MetadataToken}] Failed to resolve operand for opcode {vmOpCode.CilOpCode} at instruction #{vmMethod.Instructions.Count}");

                var instruction =
                    new CilInstruction(vmOpCode.CilOpCode.Value, operand);
                var vmStart = (uint)vmMethod.CurrentVirtualOffset;
                instruction.Offset = cilOffset;
                vmMethod.VmToCilOffsetMap[vmStart] = cilOffset;
                vmMethod.Instructions.Add(instruction);
                cilOffset += instruction.Size;
            }
        }
    }

    private void ResolveBranchTargets(VMMethod vmMethod)
    {
        // Reuse precomputed VM -> CIL offset map.
        var vmToCil = vmMethod.VmToCilOffsetMap;

        for (var i = 0; i < vmMethod.Instructions.Count; i++)
        {
            var ins = vmMethod.Instructions[i];
            switch (ins.OpCode.OperandType)
            {
                case CilOperandType.InlineBrTarget:
                case CilOperandType.ShortInlineBrTarget:
                {
                    uint vmTarget;
                    if (ins.Operand is uint u) vmTarget = u;
                    else if (ins.Operand is int si) vmTarget = unchecked((uint)si);
                    else { ins.Operand = new CilOffsetLabel(0); break; }

                    if (vmMethod.SuccessfullyDevirtualized && vmToCil.TryGetValue(vmTarget, out var targetCil))
                        ins.Operand = new CilOffsetLabel(targetCil);
                    else
                        ins.Operand = new CilOffsetLabel(0);
                    break;
                }
                case CilOperandType.InlineSwitch:
                {
                    if (ins.Operand is not int[] offsets) break;
                    var labels = new ICilLabel[offsets.Length];
                    for (var x = 0; x < offsets.Length; x++)
                    {
                        var vmTarget = unchecked((uint)offsets[x]);
                        if (vmMethod.SuccessfullyDevirtualized && vmToCil.TryGetValue(vmTarget, out var targetCil))
                            labels[x] = new CilOffsetLabel(targetCil);
                        else
                            labels[x] = new CilOffsetLabel(0);
                    }
                    ins.Operand = labels;
                    break;
                }
            }
        }
    }

    private void ResolveExceptionHandlers(VMMethod vmMethod)
    {
        vmMethod.ExceptionHandlers = new List<CilExceptionHandler>();
        if (!vmMethod.SuccessfullyDevirtualized) return;
        
        // Reuse precomputed VM -> CIL offset map.
        var vmToCil = vmMethod.VmToCilOffsetMap;
        foreach (var vmExceptionHandler in vmMethod.VMExceptionHandlers)
        {
            var exceptionHandler = new CilExceptionHandler
            {
                HandlerType = vmExceptionHandler.HandlerType,
                ExceptionType = vmExceptionHandler.HandlerType == CilExceptionHandlerType.Exception ? Resolver.ResolveType(vmExceptionHandler.CatchType) : null
            };

            var handlerStart = vmMethod.Instructions.GetByOffset(vmToCil[vmExceptionHandler.HandlerStart]);
            exceptionHandler.HandlerStart = handlerStart?.CreateLabel();

            // HandlerEnd is not explicitly defined, and we don't have a length, so we need to find it ourselves
            var handlerEndIndex = vmMethod.Instructions.GetIndexByOffset(vmToCil[vmExceptionHandler.HandlerStart]);
            var foundHandlerEnd = false;
            while (!foundHandlerEnd && vmMethod.Instructions.Count - 1 > handlerEndIndex)
            {
                var possibleHandlerEnd = vmMethod.Instructions[handlerEndIndex];

                // if there is a branch, skip past it to ensure the correct HandlerEnd is found
                if (possibleHandlerEnd.IsBranch() && possibleHandlerEnd.OpCode.Code is not (CilCode.Leave or CilCode.Leave_S))
                {
                    handlerEndIndex = vmMethod.Instructions.GetIndexByOffset(((ICilLabel)possibleHandlerEnd.Operand!).Offset);
                    continue;
                }

                switch (possibleHandlerEnd.OpCode.Code)
                {
                    case CilCode.Endfilter:
                        if (vmExceptionHandler.HandlerType == CilExceptionHandlerType.Filter)
                            foundHandlerEnd = true;
                        break;
                    case CilCode.Endfinally:
                        if (vmExceptionHandler.HandlerType == CilExceptionHandlerType.Finally)
                            foundHandlerEnd = true;
                        break;
                    case CilCode.Leave:
                    case CilCode.Leave_S:
                        if (possibleHandlerEnd.Operand is ICilLabel target &&
                            target.Offset >= exceptionHandler.HandlerStart?.Offset)
                            foundHandlerEnd = true;
                        break;
                    case CilCode.Ret:
                        // this shouldn't happen, but this makes the handler end set on the ret instruction instead of one after it
                        if (handlerEndIndex == vmMethod.Instructions.Count - 1)
                            handlerEndIndex--;
                        foundHandlerEnd = true;
                        break;
                    case CilCode.Rethrow:
                    case CilCode.Throw:
                        foundHandlerEnd = true;
                        break;
                }

                handlerEndIndex++;
            }

            exceptionHandler.HandlerEnd = vmMethod.Instructions[handlerEndIndex].CreateLabel();

            exceptionHandler.TryStart = vmMethod.Instructions.GetByOffset(vmToCil[vmExceptionHandler.TryStart])?.CreateLabel();

            // TryEnd is equal to TryStart + TryLength + 1
            var tryEndIndex = vmMethod
                .Instructions.GetIndexByOffset(
                    vmToCil[vmExceptionHandler.TryStart + vmExceptionHandler.TryLength]);
            exceptionHandler.TryEnd = vmMethod
                .Instructions[tryEndIndex + (vmMethod.Instructions.Count - 2 >= tryEndIndex ? 1 : 0)].CreateLabel();

            if (vmExceptionHandler.HandlerType == CilExceptionHandlerType.Filter)
                exceptionHandler.FilterStart = vmMethod.Instructions.GetByOffset(vmToCil[vmExceptionHandler.FilterStart])?.CreateLabel();

            vmMethod.ExceptionHandlers.Add(exceptionHandler);
        }
    }

    private object? ReadOperand(VMOpCode vmOpCode, VMMethod vmMethod) =>
        vmOpCode.CilOperandType switch // maybe switch this to vmOpCode.CilOpCode.OperandType and add more handlers
        {
            CilOperandType.InlineI => _currentReader.ReadInt32Special(),
            CilOperandType.ShortInlineI => _currentReader.ReadSByte(),
            CilOperandType.InlineI8 => _currentReader.ReadInt64(),
            CilOperandType.InlineR => _currentReader.ReadDouble(),
            CilOperandType.ShortInlineR => _currentReader.ReadSingle(),
            CilOperandType.InlineVar => _currentReader.ReadUInt16(),    // IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, _currentReader.ReadUInt16()) : GetLocal(vmMethod, _currentReader.ReadUInt16()),
            CilOperandType.ShortInlineVar => _currentReader.ReadByte(), // IsInlineArgument(vmOpCode.CilOpCode) ? GetArgument(vmMethod, _currentReader.ReadByte()) : GetLocal(vmMethod, _currentReader.ReadByte()),
            CilOperandType.InlineTok => ReadInlineTok(vmOpCode),
            CilOperandType.InlineSwitch => ReadInlineSwitch(),
            CilOperandType.InlineBrTarget => _currentReader.ReadUInt32(),
            CilOperandType.InlineArgument => _currentReader.ReadUInt16(),    // GetArgument(vmMethod, _currentReader.ReadUInt16()),  // this doesn't seem to be used, might not be correct
            CilOperandType.ShortInlineArgument => _currentReader.ReadByte(), // GetArgument(vmMethod, _currentReader.ReadByte()),    // this doesn't seem to be used, might not be correct
            CilOperandType.InlineNone => null,
            _ => null
        };

    private object? ReadSpecialOperand(VMOpCode vmOpCode, VMMethod vmMethod) =>
        vmOpCode.SpecialOpCode switch
        {
            SpecialOpCodes.EazCall => Resolver.ResolveEazCall(_currentReader.ReadInt32Special()),
            SpecialOpCodes.StartHomomorphic => ReadHomomorphicEncryption(vmMethod),
            SpecialOpCodes.EndHomomorphic => EndHomomorphic(vmMethod),
            _ => null
        };

    /// <summary>
    /// Resolves special opcodes with no CIL opcode.
    /// </summary>
    /// <param name="vmOpCode"></param>
    /// <param name="vmMethod"></param>
    /// <returns>
    /// A CIL opcode that matches the special opcode.
    /// TODO: maybe a list of op codes?
    /// </returns>
    private static CilOpCode? ResolveSpecialCilOpCode(VMOpCode vmOpCode, VMMethod vmMethod)
    {
        switch (vmOpCode.SpecialOpCode)
        {
            // case SpecialOpCodes.Stind:
            // case SpecialOpCodes.StartHomomorphic:
            case SpecialOpCodes.NoBody:
                // TODO: Analyze vm method instructions / stack to determine CIL opcode (2 opcode handlers have this pattern)
                return CilOpCodes.Nop;
        }

        return CilOpCodes.Nop;
    }

    /// <summary>
    /// Processes homomorphic encryption data into CIL instructions
    /// </summary>
    /// <param name="vmMethod"></param>
    /// <returns>
    /// branch offset
    /// </returns>
    private int? ReadHomomorphicEncryption(VMMethod vmMethod)
    {
        if (!vmMethod.HasHomomorphicEncryption)
        {
            vmMethod.HasHomomorphicEncryption = true;
            vmMethod.HMEndPositionStack = new Stack<uint>();
        }
        
        try
        {
            // Get salt from the last ldc.i8 in the devirtualized instructions
            var saltIns = vmMethod.Instructions.LastOrDefault();
            if (saltIns?.OpCode.Code is not CilCode.Ldc_I8 || saltIns.Operand is not long salt)
            {
                Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Previous instruction (salt) is not ldc.i8 in devirtualized method body.");
                vmMethod.SuccessfullyDevirtualized = false;
                return null;
            }

            // Resolve password from CLI or prompt (typed).
            if (!TryGetHomomorphicPassword(vmMethod.Parent.MetadataToken.ToUInt32(), out var pwdEntry))
            {
                Ctx.Console.Info($"[{vmMethod.Parent.MetadataToken}] Enter homomorphic password (typed):");
                Console.Write("Type (sbyte, byte, short, ushort, int, uint, long, ulong, string) [empty=auto]: ");
                var typeInput = Console.ReadLine()?.Trim() ?? string.Empty;
                Console.Write("Value (decimal, 0xHEX, or text): ");
                var valueInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(valueInput))
                {
                    Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] No password value provided.");
                    vmMethod.SuccessfullyDevirtualized = false;
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(typeInput))
                {
                    if (!TryMapType(typeInput, out var kind) || !TryParseTypedNumericToBigEndian(kind, valueInput, out var bytes))
                    {
                        Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Invalid type or value. Type must be one of sbyte, byte, short, ushort, int, uint, long, ulong, string. Value must be decimal, 0xHEX, or text for string.");
                        vmMethod.SuccessfullyDevirtualized = false;
                        return null;
                    }
                    pwdEntry = new HmPasswordEntry(kind, valueInput, bytes);
                }
                else
                {
                    if (!TryParseNumericAutoWidthToBigEndian(valueInput, out var bytes, out var inferredKind))
                    {
                        Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Invalid password value. Provide an integer (decimal or 0xHEX).");
                        vmMethod.SuccessfullyDevirtualized = false;
                        return null;
                    }
                    pwdEntry = new HmPasswordEntry(inferredKind, valueInput, bytes);
                }

                // Cache for subsequent use within the same run.
                Ctx.Options.HmPasswords[vmMethod.Parent.MetadataToken.ToUInt32()] = pwdEntry;
            }
            else if (Ctx.Options.VeryVerbose)
                Ctx.Console.InfoStr($"Found homomorphic password from CLI arg: {pwdEntry.Value} [{pwdEntry.Kind.ToString()}].", vmMethod.Parent.MetadataToken);

            var decryptor = new HMDecryptor(pwdEntry.Bytes, salt);
            
            var decrypted = decryptor.DecryptInstructionBlock(VMStream);
            vmMethod.HMEndPositionStack.Push((uint)(vmMethod.CurrentVirtualOffset + decrypted.Length));

            // Swap reader to decrypted bytes and push current for nested blocks.
            _readerStack.Push(_currentReader);
            _currentReader = new VMBinaryReader(new MemoryStream(decrypted));

            if (Ctx.Options.Verbose)
                Ctx.Console.Success($"[{vmMethod.Parent.MetadataToken}] Switched to decrypted instruction reader (size={decrypted.Length} bytes).");
        }
        catch (Exception ex)
        {
            Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Homomorphic decryption failed. Is the password and its type correct? Error: {ex.Message}");
            vmMethod.SuccessfullyDevirtualized = false;
        }

        return null;
    }

    private int? EndHomomorphic(VMMethod vmMethod)
    {
        try
        {
            if (_readerStack.Count == 0)
            {
                Ctx.Console.Warning($"[{vmMethod.Parent.MetadataToken}] EndHomomorphic encountered with empty reader stack");
                return null;
            }
            
            if (vmMethod.Instructions.LastOrDefault() is not { } indexIns || !indexIns.IsLdcI4())
            {
                Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] EndHomomorphic: Previous instruction (index) is not ldc.i4");
                vmMethod.SuccessfullyDevirtualized = false;
                return null;
            }
            
            // Dispose reader to free memory.
            _currentReader.Dispose();

            vmMethod.HMEndPositionStack.Pop();
            
            _currentReader = _readerStack.Pop();
            if (Ctx.Options.Verbose)
                Ctx.Console.Success($"[{vmMethod.Parent.MetadataToken}] Restored previous instruction reader");
        }
        catch (Exception ex)
        {
            Ctx.Console.Error($"[{vmMethod.Parent.MetadataToken}] Failed to restore reader after EndHomomorphic: {ex.Message}");
            vmMethod.SuccessfullyDevirtualized = false;
        }

        return null;
    }

    private bool TryGetHomomorphicPassword(uint mdToken, out HmPasswordEntry pwdEntry)
    {
        pwdEntry = null!;
        var map = Ctx.Options.HmPasswords;
        if (map is null || map.Count == 0)
            return false;

        return map.TryGetValue(mdToken, out pwdEntry);
    }

    private static bool TryParseNumericToBigEndian(string s, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();

        // Hex prefixed value => parse as unsigned and choose minimal width
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = s[2..];
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
                return false;

            if (u <= byte.MaxValue)
                bytes = new[] { (byte)u };
            else if (u <= ushort.MaxValue)
                bytes = new[] { (byte)(u >> 8), (byte)u };
            else if (u <= uint.MaxValue)
                bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u };
            else
                bytes = new[]
                {
                    (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32),
                    (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u
                };
            return true;
        }
        
        // Decimal: try signed then unsigned types to preserve width semantics
        if (sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb))
        { bytes = new[] { unchecked((byte)sb) }; return true; }
        if (byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        { bytes = new[] { b }; return true; }
        if (short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh))
        { var u = unchecked((ushort)sh); bytes = new[] { (byte)(u >> 8), (byte)u }; return true; }
        if (ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ush))
        { bytes = new[] { (byte)(ush >> 8), (byte)ush }; return true; }
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32))
        { var u = unchecked((uint)i32); bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true; }
        if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui32))
        { bytes = new[] { (byte)(ui32 >> 24), (byte)(ui32 >> 16), (byte)(ui32 >> 8), (byte)ui32 }; return true; }
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64))
        { var u = unchecked((ulong)i64); bytes = new[] { (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32), (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true; }
        if (ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui64))
        { bytes = new[] { (byte)(ui64 >> 56), (byte)(ui64 >> 48), (byte)(ui64 >> 40), (byte)(ui64 >> 32), (byte)(ui64 >> 24), (byte)(ui64 >> 16), (byte)(ui64 >> 8), (byte)ui64 }; return true; }

        return false;
    }

    private static bool TryMapType(string s, out NumericKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().ToLowerInvariant();
        switch (s)
        {
            case "sbyte":
            case "i8": kind = NumericKind.SByte; return true;
            case "byte":
            case "u8": kind = NumericKind.Byte; return true;
            case "short":
            case "int16":
            case "i16": kind = NumericKind.Int16; return true;
            case "ushort":
            case "uint16":
            case "u16": kind = NumericKind.UInt16; return true;
            case "int":
            case "int32":
            case "i32": kind = NumericKind.Int32; return true;
            case "uint":
            case "uint32":
            case "u32": kind = NumericKind.UInt32; return true;
            case "long":
            case "int64":
            case "i64": kind = NumericKind.Int64; return true;
            case "ulong":
            case "uint64":
            case "u64": kind = NumericKind.UInt64; return true;
            case "string":
            case "str": kind = NumericKind.String; return true;
            default: return false;
        }
    }

    private static bool TryParseTypedNumericToBigEndian(NumericKind kind, string s, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (kind == NumericKind.String)
        {
            bytes = Encoding.Unicode.GetBytes(s);
            return true;
        }
        var isHex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (isHex)
        {
            var hex = s[2..];
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
                return false;
            switch (kind)
            {
                case NumericKind.SByte:
                { var v = unchecked((sbyte)u); bytes = new[] { unchecked((byte)v) }; return true; }
                case NumericKind.Byte:
                { var v = unchecked((byte)u); bytes = new[] { v }; return true; }
                case NumericKind.Int16:
                { var v = unchecked((short)u); var uv = unchecked((ushort)v); bytes = new[] { (byte)(uv >> 8), (byte)uv }; return true; }
                case NumericKind.UInt16:
                { var v = unchecked((ushort)u); bytes = new[] { (byte)(v >> 8), (byte)v }; return true; }
                case NumericKind.Int32:
                { var v = unchecked((int)u); var uv = unchecked((uint)v); bytes = new[] { (byte)(uv >> 24), (byte)(uv >> 16), (byte)(uv >> 8), (byte)uv }; return true; }
                case NumericKind.UInt32:
                { var v = unchecked((uint)u); bytes = new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }; return true; }
                case NumericKind.Int64:
                { var v = unchecked((long)u); var uv = unchecked((ulong)v); bytes = new[] { (byte)(uv >> 56), (byte)(uv >> 48), (byte)(uv >> 40), (byte)(uv >> 32), (byte)(uv >> 24), (byte)(uv >> 16), (byte)(uv >> 8), (byte)uv }; return true; }
                case NumericKind.UInt64:
                { var v = unchecked((ulong)u); bytes = new[] { (byte)(v >> 56), (byte)(v >> 48), (byte)(v >> 40), (byte)(v >> 32), (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }; return true; }
                default: return false;
            }
        }
        else
        {
            switch (kind)
            {
                case NumericKind.SByte:
                    if (!sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb)) return false; bytes = new[] { unchecked((byte)sb) }; return true;
                case NumericKind.Byte:
                    if (!byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b)) return false; bytes = new[] { b }; return true;
                case NumericKind.Int16:
                    if (!short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh)) return false; { var u = unchecked((ushort)sh); bytes = new[] { (byte)(u >> 8), (byte)u }; return true; }
                case NumericKind.UInt16:
                    if (!ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ush)) return false; bytes = new[] { (byte)(ush >> 8), (byte)ush }; return true;
                case NumericKind.Int32:
                    if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32)) return false; { var u = unchecked((uint)i32); bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true; }
                case NumericKind.UInt32:
                    if (!uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui32)) return false; bytes = new[] { (byte)(ui32 >> 24), (byte)(ui32 >> 16), (byte)(ui32 >> 8), (byte)ui32 }; return true;
                case NumericKind.Int64:
                    if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64)) return false; { var u = unchecked((ulong)i64); bytes = new[] { (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32), (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true; }
                case NumericKind.UInt64:
                    if (!ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui64)) return false; bytes = new[] { (byte)(ui64 >> 56), (byte)(ui64 >> 48), (byte)(ui64 >> 40), (byte)(ui64 >> 32), (byte)(ui64 >> 24), (byte)(ui64 >> 16), (byte)(ui64 >> 8), (byte)ui64 }; return true;
                default:
                    return false;
            }
        }
    }

    private static bool TryParseNumericAutoWidthToBigEndian(string s, out byte[] bytes, out NumericKind kind)
    {
        bytes = Array.Empty<byte>();
        kind = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hex = s[2..];
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
                return false;
            if (u <= byte.MaxValue)
            { bytes = new[] { (byte)u }; kind = NumericKind.Byte; return true; }
            if (u <= ushort.MaxValue)
            { bytes = new[] { (byte)(u >> 8), (byte)u }; kind = NumericKind.UInt16; return true; }
            if (u <= uint.MaxValue)
            { bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; kind = NumericKind.UInt32; return true; }
            bytes = new[] { (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32), (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; kind = NumericKind.UInt64; return true;
        }
        if (sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb2))
        { bytes = new[] { unchecked((byte)sb2) }; kind = NumericKind.SByte; return true; }
        if (byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b2))
        { bytes = new[] { b2 }; kind = NumericKind.Byte; return true; }
        if (short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh2))
        { var u2 = unchecked((ushort)sh2); bytes = new[] { (byte)(u2 >> 8), (byte)u2 }; kind = NumericKind.Int16; return true; }
        if (ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ush2))
        { bytes = new[] { (byte)(ush2 >> 8), (byte)ush2 }; kind = NumericKind.UInt16; return true; }
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i322))
        { var u3 = unchecked((uint)i322); bytes = new[] { (byte)(u3 >> 24), (byte)(u3 >> 16), (byte)(u3 >> 8), (byte)u3 }; kind = NumericKind.Int32; return true; }
        if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui322))
        { bytes = new[] { (byte)(ui322 >> 24), (byte)(ui322 >> 16), (byte)(ui322 >> 8), (byte)ui322 }; kind = NumericKind.UInt32; return true; }
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i642))
        { var u4 = unchecked((ulong)i642); bytes = new[] { (byte)(u4 >> 56), (byte)(u4 >> 48), (byte)(u4 >> 40), (byte)(u4 >> 32), (byte)(u4 >> 24), (byte)(u4 >> 16), (byte)(u4 >> 8), (byte)u4 }; kind = NumericKind.Int64; return true; }
        if (ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui642))
        { bytes = new[] { (byte)(ui642 >> 56), (byte)(ui642 >> 48), (byte)(ui642 >> 40), (byte)(ui642 >> 32), (byte)(ui642 >> 24), (byte)(ui642 >> 16), (byte)(ui642 >> 8), (byte)ui642 }; kind = NumericKind.UInt64; return true; }
        return false;
    }

    private object? ReadInlineTok(VMOpCode vmOpCode) =>
      vmOpCode.CilOpCode?.OperandType switch
      {
            CilOperandType.InlineString => Resolver.ResolveString(_currentReader.ReadInt32Special()),
            _ => Resolver.ResolveToken(_currentReader.ReadInt32Special())
      };

  private int[] ReadInlineSwitch()
  {
        var destCount = _currentReader.ReadInt32Special();
        var branchDests = new int[destCount];
        for (var i = 0; i < destCount; i++)
            branchDests[i] = _currentReader.ReadInt32Special();
        return branchDests;
  }

#pragma warning disable CS8618
    public MethodDevirtualizer(DevirtualizationContext ctx) : base(ctx)
    {
    }
#pragma warning restore CS8618
}