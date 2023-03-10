using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using EazyDevirt.Core.Architecture;
using EazyDevirt.Core.Architecture.InlineOperands;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.Core.IO;

internal class Resolver
{
    public Resolver(DevirtualizationContext ctx)
    {
        Ctx = ctx;
        VMStreamReader = new VMBinaryReader(new CryptoStreamV3(Ctx.VMResolverStream, Ctx.MethodCryptoKey, true));
    }
    
    private DevirtualizationContext Ctx { get; }
    
    private VMBinaryReader VMStreamReader { get; }

    public TypeSignature? ResolveType(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        if (inlineOperand.IsToken)
            return ((ITypeDefOrRef)Ctx.Module.LookupMember(inlineOperand.Token))
                .MakeGenericInstanceType(data.GenericTypes.Select(g => ResolveType(g.Position)!).ToArray())
                .ImportWith(Ctx.Importer);

                
        // Try to find type definition or reference
        var typeDefOrRef = ((ITypeDefOrRef?)Ctx.Module.GetAllTypes()
                                .FirstOrDefault(x => x.FullName == data.TypeName) ??
                            (ITypeDefOrRef?)Ctx.Module.GetImportedTypeReferences()
                                .FirstOrDefault(x => x.FullName == data.TypeName && x.Scope?.Name == data.AssemblyName))?.ToTypeSignature();
        if (typeDefOrRef != null)
            return typeDefOrRef.ImportWith(Ctx.Importer);
        
        var assemblyRef = Ctx.Module.AssemblyReferences.FirstOrDefault(x => x.Name == data.AssemblyName);
        if (assemblyRef == null)
        {
            Ctx.Console.Error($"Failed to find vm type {data.Name} assembly reference!");
            return null!;
        }

        var typeRef = assemblyRef.CreateTypeReference(data.Namespace, data.TypeNameWithoutNamespace);
        if (data.HasGenericTypes)
            return typeRef
                .MakeGenericInstanceType(data.GenericTypes.Select(g => ResolveType(g.Position)!).ToArray())
                .ImportWith(Ctx.Importer);
        return typeRef.ToTypeSignature().ImportWith(Ctx.Importer);
    }
    
    public IFieldDescriptor? ResolveField(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return (FieldDefinition)Ctx.Module.LookupMember(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMFieldData data)
            throw new Exception("VM inline operand expected to have field data!");
        
        var declaringTypeSig = ResolveType(data.DeclaringType.Position);
        var declaringType = declaringTypeSig?.Resolve();
        if (declaringType != null)
            return declaringType.Fields.FirstOrDefault(f => f.Name == data.Name)?.ImportWith(Ctx.Importer);
        
        Ctx.Console.Error($"Unable to resolve vm field {data.Name} declaring type {declaringTypeSig?.Name} to a TypeDef!");
        return null;
    }

    // private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodData data) =>
    //     declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name 
    //                                                && m.Parameters.Count == data.Parameters.Length 
    //                                                && m.Parameters.Where((p, i) => 
    //                                                    p.ParameterType.FullName == ResolveType(data.Parameters[i].Position)?.FullName).Count() == data.Parameters.Length);

    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodInfo data) =>
        declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name 
                                                   && m.Parameters.Count == data.VMParameters.Count 
                                                   && m.Parameters.Where((p, i) => 
                                                       p.ParameterType.FullName == ResolveType(data.VMParameters[i].VMType)?.FullName).Count() == data.VMParameters.Count);

    public IMethodDescriptor? ResolveMethod(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (!inlineOperand.HasData || inlineOperand.Data is not VMMethodData data)
            throw new Exception("VM inline operand expected to have method data!");
        
        if (inlineOperand.IsToken)
        {
            var methodSpec = ((IMethodDescriptor)Ctx.Module.LookupMember(inlineOperand.Token)).Resolve();
            if (data.HasGenericArguments)
                return methodSpec?
                    .MakeGenericInstanceMethod(data.GenericArguments.Select(g => ResolveType(g.Position)!).ToArray())
                    .ImportWith(Ctx.Importer);
            return methodSpec?.ImportWith(Ctx.Importer);
        }
        
        var declaringType = ResolveType(data.DeclaringType.Position);
        if (declaringType == null)
        {
            Ctx.Console.Error($"Unable to resolve declaring type on vm method {data.Name}");
            return null;
        }
        
        var returnType = ResolveType(data.ReturnType.Position);
        if (returnType == null)
        {
            Ctx.Console.Error($"Failed to resolve vm method {data.Name} return type!");
            return null;
        }

        var memberRef = declaringType
            .ToTypeDefOrRef()
            .CreateMemberReference(data.Name, data.IsStatic
                ? MethodSignature.CreateStatic(
                    returnType, data.GenericArguments.Length, data.Parameters.Select(g => ResolveType(g.Position)!))
                : MethodSignature.CreateInstance(
                    returnType, data.GenericArguments.Length, data.Parameters.Select(g => ResolveType(g.Position)!)));

        if (data.HasGenericArguments)
            return memberRef
                .MakeGenericInstanceMethod(data.GenericArguments.Select(g => ResolveType(g.Position)!).ToArray())
                .ImportWith(Ctx.Importer);
        return memberRef.ImportWith(Ctx.Importer);
    }

    public IMemberDescriptor? ResolveToken(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var member = Ctx.Module.LookupMember(inlineOperand.Token);
            return member switch
            {
                TypeDefinition typeDef => typeDef.ToTypeSignature(),
                FieldDefinition fieldDef => fieldDef,
                MethodDefinition methodDef => methodDef,
                _ => throw new ArgumentOutOfRangeException(nameof(member), member.GetType().FullName, "Member not a type, field, or method definition.")
            };
        }

        if (!inlineOperand.HasData)
            throw new Exception("VM inline operand expected to have data!");

        return inlineOperand.Data.Type switch
        {
            VMInlineOperandType.Type => ResolveType(position),
            VMInlineOperandType.Field => ResolveField(position),
            VMInlineOperandType.Method => ResolveMethod(position),
            _ => throw new ArgumentOutOfRangeException(nameof(inlineOperand.Data.Type), inlineOperand.Data.Type, "VM inline operand data is neither Type, Field, nor Method!")
        };
    }
    
    public IMethodDescriptor? ResolveEazCall(int value)
    {
        var noGenericArgs = (value & 0x80000000) != 0; 
        // var maybeForceResolveGenericVarsIdk  = (value & 0x40000000) != 0;
        var position = value & 0x3FFFFFFF;
        
        // TODO: verify support for generic arguments to type and method

        IMethodDescriptor? methodDescriptor = null;
        if (!noGenericArgs)
        {
            methodDescriptor = ResolveMethod(position);
            if (methodDescriptor == null)
            {
                Ctx.Console.Error($"Failed to resolve vm eaz call with generic arguments {value}!");
                return null;
            }

            position &= -1073741825; // 0xBFFFFFFF
        }
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var methodInfo = new VMMethodInfo(VMStreamReader);

        var declaringType = ResolveType(methodInfo.VMDeclaringType)?.Resolve();
        if (declaringType == null)
        {
            Ctx.Console.Error($"Failed to resolve vm eaz call {methodInfo.Name} declaring type {declaringType?.Name} as TypeDef!");
            return null;
        }

        // might error on return if generic args exist
        var method = ResolveMethod(declaringType, methodInfo);
        if (method != null)
            return noGenericArgs
                ? method.ImportWith(Ctx.Importer)
                : (IMethodDescriptor?)methodDescriptor!.ImportWith(Ctx.Importer);
        
        Ctx.Console.Error($"Failed to resolve vm eaz call {methodInfo.Name}");
        return null;
    }

    public string ResolveString(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return Ctx.Module.LookupString(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMStringData data) 
            throw new Exception("VM inline operand expected to have string data!");
        
        return data.Value;
    }
}