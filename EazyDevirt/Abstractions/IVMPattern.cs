using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Architecture;

namespace EazyDevirt.Abstractions;

public interface IVMPattern : IPattern
{
    VMOpCode Translates { get; }
}