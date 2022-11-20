using AsmResolver.DotNet;

namespace EazyDevirt.Architecture;

internal record VMMethod(MethodDefinition Parent, string EncodedPosition)
{
    public MethodDefinition Parent { get; } = Parent;
    public string EncodedPosition { get; } = EncodedPosition;
    public VMMethodBody Body { get; } = new();
}

internal record VMMethodBody()
{
    public VMMethod Parent { get; }
    public List<VMInstruction> Instructions { get; } = new();
    public List<ITypeDescriptor> Locals { get; } = new();
    // public List<VMExceptionHandler> { get; } = new();
}

internal record VMInstruction()
{
    public VMOpCode OpCode { get; }
    public object Operand { get; }
}