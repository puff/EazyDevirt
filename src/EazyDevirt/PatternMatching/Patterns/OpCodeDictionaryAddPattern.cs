using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record OpCodeDictionaryAddPattern : IPattern
{
    /// <summary>
    /// Pattern for additions to the VM OpCode dictionary
    /// </summary>
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    {
        CilOpCodes.Dup,         // 40    007E   dup
        CilOpCodes.Ldarg_0,     // 41    007F   ldarg.0
        CilOpCodes.Ldfld,       // 42    0080   ldfld   valuetype Struct13 VMOpCodeStructs::struct13_89
        CilOpCodes.Stloc_0,     // 43    0085   stloc.0
        CilOpCodes.Ldloca_S,    // 44    0086   ldloca.s   V_0 (0)
        CilOpCodes.Call,        // 45    0088   call   instance int32 Struct13::GetVMOpCodeType()
        CilOpCodes.Ldarg_0,     // 46    008D   ldarg.0
        CilOpCodes.Ldfld,       // 47    008E   ldfld   valuetype Struct13 VMOpCodeStructs::struct13_89
        CilOpCodes.Ldsfld,      // 48    0093   ldsfld  class VM/VMOpCodeDelegate VM/VMOpCodeDelegateStorage::delegate1_25
        CilOpCodes.Dup,         // 49    0098   dup
        CilOpCodes.Brtrue_S,    // 50    0099   brtrue.s   57 (00AE) newobj instance void VM/VMOperand::.ctor(valuetype Struct13, class VM/VMOpCodeDelegate)
        CilOpCodes.Pop,         // 51    009B   pop
        CilOpCodes.Ldnull,      // 52    009C   ldnull
        CilOpCodes.Ldftn,       // 53    009D   ldftn   void VM::smethod_27(class VM, class Class81)
        CilOpCodes.Newobj,      // 54    00A3   newobj  instance void VM/VMOpCodeDelegate::.ctor(object, native int)
        CilOpCodes.Dup,         // 55    00A8   dup
        CilOpCodes.Stsfld,      // 56    00A9   stsfld  class VM/VMOpCodeDelegate VM/VMOpCodeDelegateStorage::delegate1_25
        CilOpCodes.Newobj,      // 57    00AE   newobj  instance void VM/VMOperand::.ctor(valuetype Struct13, class VM/VMOpCodeDelegate)
        CilOpCodes.Callvirt,    // 58    00B3   callvirt   instance void class [System.Collections]System.Collections.Generic.Dictionary`2<int32, valuetype VM/VMOperand>::Add(!0, !1)
                                // ...
    };
    
    public bool MatchEntireBody => false;
}