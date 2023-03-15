using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
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

    private TypeSignature ApplySigModifiers(TypeSignature baseTypeSig, Stack<string> mods)
    {
        while (mods.Count > 0)
        {
            var mod = mods.Pop();
            baseTypeSig = mod switch
            {
                "[]" => baseTypeSig.MakeSzArrayType(),
                "*" => baseTypeSig.MakePointerType(),
                "&" => baseTypeSig.MakeByReferenceType(),
                _ => throw new Exception($"Unknown modifier: {mod}")
            };
        }

        return baseTypeSig;
    }
    
    public ITypeDefOrRef? ResolveType(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        if (inlineOperand.IsToken)
            return ApplySigModifiers(((ITypeDefOrRef)Ctx.Module.LookupMember(inlineOperand.Token))
                .MakeGenericInstanceType(data.GenericTypes.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray()),
                data.TypeName.Modifiers).ToTypeDefOrRef().ImportWith(Ctx.Importer);

                
        // Try to find type definition or reference
        var typeDefOrRef = ((ITypeDefOrRef?)Ctx.Module.GetAllTypes()
                                .FirstOrDefault(x => x.FullName == data.TypeName.Name) ??
                            (ITypeDefOrRef?)Ctx.Module.GetImportedTypeReferences()
                                .FirstOrDefault(x => x.FullName == data.TypeName.Name && x.Scope?.Name == data.TypeName.AssemblyName));
        if (typeDefOrRef != null)
            return ApplySigModifiers(typeDefOrRef.ToTypeSignature(), data.TypeName.Modifiers).ToTypeDefOrRef().ImportWith(Ctx.Importer);

        var assemblyRef = Ctx.Module.AssemblyReferences.FirstOrDefault(x => x.Name == data.TypeName.AssemblyName);
        if (assemblyRef == null)
        {
            Ctx.Console.Error($"Failed to find vm type {data.Name} assembly reference!");
            return null!;
        }

        var typeRef = assemblyRef.CreateTypeReference(data.TypeName.Namespace, data.TypeName.NameWithoutNamespace);
        var typeBaseSig = typeRef.ToTypeSignature();
        if (data.HasGenericTypes)
            typeBaseSig = typeRef
                .MakeGenericInstanceType(data.GenericTypes.Select(g => ResolveType(g.Position)!.ToTypeSignature())
                    .ToArray());

        return ApplySigModifiers(typeBaseSig, data.TypeName.Modifiers).ToTypeDefOrRef().ImportWith(Ctx.Importer);
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

    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodData data) =>
        declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name
                                                   && (!(m.Signature?.ReturnsValue).GetValueOrDefault() ||
                                                       m.Signature?.ReturnType.FullName ==
                                                       ResolveType(data.ReturnType.Position)?.FullName)
                                                   && m.Signature?.GenericParameterCount == data.GenericArguments.Length
                                                   && m.Parameters.Count == data.Parameters.Length
                                                   && m.Parameters.Zip(data.Parameters).All(x =>
                                                       x.First.ParameterType.FullName ==
                                                       ResolveType(x.Second.Position)?.FullName));


    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodInfo data) =>
        declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name
                                                   && (!(m.Signature?.ReturnsValue).GetValueOrDefault() ||
                                                       m.Signature?.ReturnType.FullName ==
                                                       ResolveType(data.VMReturnType)?.FullName)
                                                   && m.Parameters.Count == data.VMParameters.Count
                                                   && m.Parameters.Zip(data.VMParameters).All(x =>
                                                       x.First.ParameterType.FullName ==
                                                       ResolveType(x.Second.VMType)?.FullName));

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
                    .MakeGenericInstanceMethod(data.GenericArguments.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
                    .ImportWith(Ctx.Importer);
            return methodSpec?.ImportWith(Ctx.Importer);
        }
        
        var declaringTypeDefOrRef = ResolveType(data.DeclaringType.Position);
        if (declaringTypeDefOrRef == null)
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

        // there's probably a better way to check if a type is outside the assembly / module
        if (declaringTypeDefOrRef.Scope?.GetAssembly()?.Name != Ctx.Module.Assembly?.Name)
        {
            var importedMemberRef = Ctx.Module.GetImportedMemberReferences().FirstOrDefault(x =>
                x.IsMethod
                && x.DeclaringType?.FullName == declaringTypeDefOrRef.FullName
                && x.Name == data.Name
                && x.Signature is MethodSignature ms
                // TODO: Fix resolving imported member references of methods returning a generic parameter
                && (!ms.ReturnsValue || ms.ReturnType is GenericParameterSignature || ms.ReturnType.FullName == returnType.FullName)
                && ms.GenericParameterCount == data.GenericArguments.Length
                && ms.ParameterTypes.Count == data.Parameters.Length
                && ms.ParameterTypes.Zip(data.Parameters).All(z =>
                    z.First.FullName ==
                    ResolveType(z.Second.Position)?.FullName));
            
            if (importedMemberRef != null)
            {
                if (data.HasGenericArguments)
                    return importedMemberRef
                        .MakeGenericInstanceMethod(
                            data.GenericArguments.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
                        .ImportWith(Ctx.Importer);
                return importedMemberRef.ImportWith(Ctx.Importer);
            }
        }
        
        var declaringType = declaringTypeDefOrRef.Resolve();
        if (declaringType != null)
        {
            var methodDef = ResolveMethod(declaringType, data);
            if (data.HasGenericArguments)
                return methodDef?
                    .MakeGenericInstanceMethod(data.GenericArguments.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
                    .ImportWith(Ctx.Importer);
            return methodDef?.ImportWith(Ctx.Importer);
        }
        
        // stuff below should never execute

        var memberRef = declaringTypeDefOrRef
            .CreateMemberReference(data.Name, data.IsStatic
                ? MethodSignature.CreateStatic(
                    returnType.ToTypeSignature(), data.GenericArguments.Length,
                    data.Parameters.Select(g => ResolveType(g.Position)!.ToTypeSignature()))
                : MethodSignature.CreateInstance(
                    returnType.ToTypeSignature(), data.GenericArguments.Length,
                    data.Parameters.Select(g => ResolveType(g.Position)!.ToTypeSignature())));

        if (data.HasGenericArguments)
            return memberRef
                .MakeGenericInstanceMethod(data.GenericArguments.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
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