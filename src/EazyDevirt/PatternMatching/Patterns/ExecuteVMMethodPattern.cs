using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core.Abstractions.Interfaces;

namespace EazyDevirt.PatternMatching.Patterns;

internal record ExecuteVMMethodPattern : IPattern
{
    public IList<CilOpCode> Pattern => new List<CilOpCode>
    { 
        CilOpCodes.Ldarg_0,     // 109	0128	ldarg.0
        CilOpCodes.Ldloc_S,     // 110	0129	ldloc.s	V_5 (5)
        CilOpCodes.Callvirt,    // 111	012B	callvirt	instance int32 VMParameter::GetTypeCode()
        CilOpCodes.Ldc_I4_0,    // 112	0130	ldc.i4.0
        CilOpCodes.Call,        // 113	0131	call	instance class [mscorlib]System.Type VM::ResolveType(int32, bool)
        CilOpCodes.Stloc_S,     // 114	0136	stloc.s	V_8 (8)
        CilOpCodes.Ldarg_1,     // 115	0138	ldarg.1
        CilOpCodes.Ldloc_0,     // 116	0139	ldloc.0
        CilOpCodes.Ldnull,      // 117	013A	ldnull
                                // ...
    };
    
    public bool MatchEntireBody => false;

    public bool Verify(MethodDefinition method, int index = 0)
    {
        var resolveTypeMethod = method.CilMethodBody!.Instructions[index + 4].Operand as SerializedMethodDefinition;
        return resolveTypeMethod!.Signature!.ReturnsValue ||
               resolveTypeMethod.Signature.ReturnType.FullName == "System.Type" &&
               resolveTypeMethod.Parameters.Count == 2 &&
               resolveTypeMethod.Parameters[0].ParameterType.FullName == "System.Int32" &&
               resolveTypeMethod.Parameters[1].ParameterType.FullName == "System.Boolean";
    }
}