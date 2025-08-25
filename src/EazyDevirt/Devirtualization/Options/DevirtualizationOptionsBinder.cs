using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Globalization;
using System.Text;

namespace EazyDevirt.Devirtualization.Options;

internal class DevirtualizationOptionsBinder : BinderBase<DevirtualizationOptions>
{
    private readonly Argument<FileInfo> _assemblyArgument;
    private readonly Argument<DirectoryInfo> _outputPathArgument;
    private readonly Option<int> _verbosityOption;
    private readonly Option<bool> _noVerifyOption;
    private readonly Option<bool> _preserveAllOption;
    private readonly Option<bool> _keepTypesOption;
    private readonly Option<bool> _saveAnywayOption;
    private readonly Option<bool> _onlySaveDevirtedOption;
    private readonly Option<bool> _requireDepsForGenericMethods;
    private readonly Option<string[]> _hmPasswordsOption;

    public DevirtualizationOptionsBinder(Argument<FileInfo> assemblyArgument, Argument<DirectoryInfo> outputPathArgument, 
        Option<int> verbosityOption, Option<bool> preserveAllOption, Option<bool> noVerifyOption, Option<bool> keepTypesOption, Option<bool> saveAnywayOption,
        Option<bool> onlySaveDevirtedOption, Option<bool> requireDepsForGenericMethods, Option<string[]> hmPasswordsOption)
    {
        _assemblyArgument = assemblyArgument;
        _outputPathArgument = outputPathArgument;
        _verbosityOption = verbosityOption;
        _preserveAllOption = preserveAllOption;
        _noVerifyOption = noVerifyOption;
        _keepTypesOption = keepTypesOption;
        _saveAnywayOption = saveAnywayOption;
        _onlySaveDevirtedOption = onlySaveDevirtedOption;
        _requireDepsForGenericMethods = requireDepsForGenericMethods;
        _hmPasswordsOption = hmPasswordsOption;
    }

    protected override DevirtualizationOptions GetBoundValue(BindingContext bindingContext) =>
        new()
        {
            Assembly = bindingContext.ParseResult.GetValueForArgument(_assemblyArgument),
            OutputPath = bindingContext.ParseResult.GetValueForArgument(_outputPathArgument),
            Verbosity = bindingContext.ParseResult.GetValueForOption(_verbosityOption),
            PreserveAll = bindingContext.ParseResult.GetValueForOption(_preserveAllOption),
            NoVerify = bindingContext.ParseResult.GetValueForOption(_noVerifyOption),
            KeepTypes = bindingContext.ParseResult.GetValueForOption(_keepTypesOption),
            SaveAnyway = bindingContext.ParseResult.GetValueForOption(_saveAnywayOption),
            OnlySaveDevirted = bindingContext.ParseResult.GetValueForOption(_onlySaveDevirtedOption),
            RequireDepsForGenericMethods = bindingContext.ParseResult.GetValueForOption(_requireDepsForGenericMethods),
            HmPasswords = ParseHmPasswords(bindingContext)
        };

    private Dictionary<uint, HmPasswordEntry> ParseHmPasswords(BindingContext bindingContext)
    {
        var dict = new Dictionary<uint, HmPasswordEntry>();
        var entries = bindingContext.ParseResult.GetValueForOption(_hmPasswordsOption) ?? Array.Empty<string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;
            // Accept formats:
            // 1) mdtoken:type:value
            // 2) mdtoken:value    (fallback, auto-width)
            var parts = entry.Split(':', 3, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;

            var tokenStr = parts[0];
            if (!TryParseMdToken(tokenStr, out var token))
                continue;

            if (parts.Length == 3)
            {
                var typeStr = parts[1];
                var valueStr = parts[2];
                if (!TryMapType(typeStr, out var kind))
                    continue;
                if (!TryParseTypedNumericToBigEndian(kind, valueStr, out var bytes))
                    continue;
                dict[token] = new HmPasswordEntry(kind, valueStr, bytes);
            }
            else // parts.Length == 2 -> fallback (auto-width)
            {
                var valueStr = parts[1];
                if (!TryParseNumericAutoWidthToBigEndian(valueStr, out var bytes, out var inferredKind))
                    continue;
                dict[token] = new HmPasswordEntry(inferredKind, valueStr, bytes);
            }
        }

        return dict;
    }

    private static bool TryParseMdToken(string s, out uint token)
    {
        token = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];

        return uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out token);
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

        bool isHex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        if (isHex)
        {
            var hex = s[2..];
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var u))
                return false;
            switch (kind)
            {
                case NumericKind.SByte:
                {
                    var v = unchecked((sbyte)u);
                    bytes = new[] { unchecked((byte)v) };
                    return true;
                }
                case NumericKind.Byte:
                {
                    var v = unchecked((byte)u);
                    bytes = new[] { v };
                    return true;
                }
                case NumericKind.Int16:
                {
                    var v = unchecked((short)u);
                    var uv = unchecked((ushort)v);
                    bytes = new[] { (byte)(uv >> 8), (byte)uv };
                    return true;
                }
                case NumericKind.UInt16:
                {
                    var v = unchecked((ushort)u);
                    bytes = new[] { (byte)(v >> 8), (byte)v };
                    return true;
                }
                case NumericKind.Int32:
                {
                    var v = unchecked((int)u);
                    var uv = unchecked((uint)v);
                    bytes = new[] { (byte)(uv >> 24), (byte)(uv >> 16), (byte)(uv >> 8), (byte)uv };
                    return true;
                }
                case NumericKind.UInt32:
                {
                    var v = unchecked((uint)u);
                    bytes = new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
                    return true;
                }
                case NumericKind.Int64:
                {
                    var v = unchecked((long)u);
                    var uv = unchecked((ulong)v);
                    bytes = new[]
                    {
                        (byte)(uv >> 56), (byte)(uv >> 48), (byte)(uv >> 40), (byte)(uv >> 32),
                        (byte)(uv >> 24), (byte)(uv >> 16), (byte)(uv >> 8), (byte)uv
                    };
                    return true;
                }
                case NumericKind.UInt64:
                {
                    var v = unchecked((ulong)u);
                    bytes = new[]
                    {
                        (byte)(v >> 56), (byte)(v >> 48), (byte)(v >> 40), (byte)(v >> 32),
                        (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v
                    };
                    return true;
                }
                default:
                    return false;
            }
        }
        else
        {
            switch (kind)
            {
                case NumericKind.SByte:
                    if (!sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb)) return false;
                    bytes = new[] { unchecked((byte)sb) }; return true;
                case NumericKind.Byte:
                    if (!byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b)) return false;
                    bytes = new[] { b }; return true;
                case NumericKind.Int16:
                    if (!short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh)) return false;
                    {
                        var u = unchecked((ushort)sh);
                        bytes = new[] { (byte)(u >> 8), (byte)u }; return true;
                    }
                case NumericKind.UInt16:
                    if (!ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ush)) return false;
                    bytes = new[] { (byte)(ush >> 8), (byte)ush }; return true;
                case NumericKind.Int32:
                    if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32)) return false;
                    {
                        var u = unchecked((uint)i32);
                        bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true;
                    }
                case NumericKind.UInt32:
                    if (!uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui32)) return false;
                    bytes = new[] { (byte)(ui32 >> 24), (byte)(ui32 >> 16), (byte)(ui32 >> 8), (byte)ui32 }; return true;
                case NumericKind.Int64:
                    if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64)) return false;
                    {
                        var u = unchecked((ulong)i64);
                        bytes = new[] { (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32), (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; return true;
                    }
                case NumericKind.UInt64:
                    if (!ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui64)) return false;
                    bytes = new[] { (byte)(ui64 >> 56), (byte)(ui64 >> 48), (byte)(ui64 >> 40), (byte)(ui64 >> 32), (byte)(ui64 >> 24), (byte)(ui64 >> 16), (byte)(ui64 >> 8), (byte)ui64 }; return true;
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
            bytes = new[]
            {
                (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32),
                (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u
            }; kind = NumericKind.UInt64; return true;
        }

        // Decimal: try signed then unsigned to preserve width semantics
        if (sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sb))
        { bytes = new[] { unchecked((byte)sb) }; kind = NumericKind.SByte; return true; }
        if (byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        { bytes = new[] { b }; kind = NumericKind.Byte; return true; }
        if (short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh))
        { var u = unchecked((ushort)sh); bytes = new[] { (byte)(u >> 8), (byte)u }; kind = NumericKind.Int16; return true; }
        if (ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ush))
        { bytes = new[] { (byte)(ush >> 8), (byte)ush }; kind = NumericKind.UInt16; return true; }
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32))
        { var u = unchecked((uint)i32); bytes = new[] { (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; kind = NumericKind.Int32; return true; }
        if (uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui32))
        { bytes = new[] { (byte)(ui32 >> 24), (byte)(ui32 >> 16), (byte)(ui32 >> 8), (byte)ui32 }; kind = NumericKind.UInt32; return true; }
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64))
        { var u = unchecked((ulong)i64); bytes = new[] { (byte)(u >> 56), (byte)(u >> 48), (byte)(u >> 40), (byte)(u >> 32), (byte)(u >> 24), (byte)(u >> 16), (byte)(u >> 8), (byte)u }; kind = NumericKind.Int64; return true; }
        if (ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ui64))
        { bytes = new[] { (byte)(ui64 >> 56), (byte)(ui64 >> 48), (byte)(ui64 >> 40), (byte)(ui64 >> 32), (byte)(ui64 >> 24), (byte)(ui64 >> 16), (byte)(ui64 >> 8), (byte)ui64 }; kind = NumericKind.UInt64; return true; }

        return false;
    }
}