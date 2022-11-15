using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;

namespace EazyDevirt.Abstractions;

internal interface IVMPattern : IPattern
{
    VMOpCode Translates { get; }
}