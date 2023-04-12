﻿using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Architecture;

namespace EazyDevirt.Core.Abstractions.Interfaces;

internal interface IOpCodePattern : IPattern
{
    CilOpCode? CilOpCode
    {
        get { return null; }
    }

    SpecialOpCodes? SpecialOpCode
    {
        get { return null; }
    }

    /// <summary>
    ///     Whether the pattern can translate to CIL opcodes or is a special vm action.
    /// </summary>
    bool IsSpecial
    {
        get { return false; }
    }

    /// <summary>
    ///     Whether the pattern can be matched more than once.
    /// </summary>
    bool AllowMultiple
    {
        get { return false; }
    }

    /// <summary>
    ///     Additional verification to ensure the match is valid.
    /// </summary>
    /// <param name="vmOpCode">VMOpCode the pattern is for</param>
    /// <param name="index">Index of the pattern</param>
    /// <returns>Whether verification is successful</returns>
    bool Verify(VMOpCode vmOpCode, int index = 0)
    {
        return Verify(vmOpCode.SerializedDelegateMethod, index);
    }
}