using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Abstractions;

namespace EazyDevirt.PatternMatching.Patterns;

internal  record GetVMStreamPattern : IPattern
{
    /// <summary>
    /// Pattern for the VM Resource Stream Getter / Initializer
    /// </summary>
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Ldsfld,      // 0	0000	ldsfld	class [mscorlib]System.IO.Stream VMResourceStreamGetter::'stream'
        CilOpCodes.Brtrue_S,    // 1	0005	brtrue.s	15 (0044) ldsfld class [mscorlib]System.IO.Stream VMResourceStreamGetter::'stream'
        CilOpCodes.Ldtoken,     // 2	0007	ldtoken	VMResourceStreamGetter
        CilOpCodes.Call,        // 3	000C	call	class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
        CilOpCodes.Callvirt,    // 4	0011	callvirt	instance class [mscorlib]System.Reflection.Assembly [mscorlib]System.Type::get_Assembly()
        CilOpCodes.Ldstr,       // 5	0016	ldstr	"5247306dc5cd44d1daf7110b93ff6938"
        CilOpCodes.Callvirt,    // 6	001B	callvirt	instance class [mscorlib]System.IO.Stream [mscorlib]System.Reflection.Assembly::GetManifestResourceStream(string)
        CilOpCodes.Ldc_I4,      // 7	0020	ldc.i4	0x80
        CilOpCodes.Newarr,      // 8	0025	newarr	[System.Runtime]System.Byte
        CilOpCodes.Dup,         // 9	002A	dup
        CilOpCodes.Ldtoken,     // 10	002B	ldtoken	valuetype VMResourceStreamGetter/a1 VMResourceStreamGetter::a1
        CilOpCodes.Call,        // 11	0030	call	void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
        CilOpCodes.Call,        // 12	0035	call	string VMResourceStreamGetter::GetVMResourceModulusString()
        CilOpCodes.Call,        // 13	003A	call	class [mscorlib]System.IO.Stream VMStreamInitializer::InitializeVMStream(class [mscorlib]System.IO.Stream, uint8[], string)
        CilOpCodes.Stsfld,      // 14	003F	stsfld	class [mscorlib]System.IO.Stream VMResourceStreamGetter::'stream'
        CilOpCodes.Ldsfld,      // 15	0044	ldsfld	class [mscorlib]System.IO.Stream VMResourceStreamGetter::'stream'
        CilOpCodes.Ret          // 16	0049	ret
    };
    
    public bool Verify(MethodDefinition method)
    {
        var instructions = method.CilMethodBody!.Instructions;
        return ((SerializedMemberReference)instructions[6].Operand!).FullName 
               == "System.IO.Stream System.Reflection.Assembly::GetManifestResourceStream(System.String)";
    }
}