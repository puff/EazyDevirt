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
        if (inlineOperand.IsToken)
            return Ctx.Module.LookupMember<ITypeDefOrRef>(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        if (data.IsGenericParameterType)
        {
            if (data.GenericArgumentIndex != -1)
            {
                var typeGenericParameterSignature = new GenericParameterSignature(Ctx.Module, GenericParameterType.Method, data.GenericArgumentIndex);
                return typeGenericParameterSignature.ToTypeDefOrRef();
            }
            
            if (data.DeclaringTypeGenericArgumentIndex != -1)
            {
                var typeGenericParameterSignature = new GenericParameterSignature(Ctx.Module, GenericParameterType.Type, data.GenericArgumentIndex);
                return typeGenericParameterSignature.ToTypeDefOrRef();
            }
        }
        
        // Try to find type definition or reference
        var typeDefOrRef = (ITypeDefOrRef?)Ctx.Module.GetAllTypes()
                               .FirstOrDefault(x => x.FullName == data.TypeName.Name) ??
                           (ITypeDefOrRef?)Ctx.Module.GetImportedTypeReferences()
                               .FirstOrDefault(x => x.FullName == data.TypeName.Name && x.Scope?.Name == data.TypeName.AssemblyName.Name);
        if (typeDefOrRef != null)
        {
            var typeSig = typeDefOrRef.ToTypeSignature();
            if (data.HasGenericTypes)
                typeSig = typeDefOrRef
                    .MakeGenericInstanceType(data.GenericTypes.Select(g => ResolveType(g.Position)!.ToTypeSignature())
                        .ToArray());
            
            return ApplySigModifiers(typeSig, data.TypeName.Modifiers).ToTypeDefOrRef().ImportWith(Ctx.Importer);
        }

        var assemblyRef =
            Ctx.Module.AssemblyReferences.FirstOrDefault(x => x.Name == data.TypeName.AssemblyName.Name) ??
            new AssemblyReference(data.TypeName.AssemblyName.Name, data.TypeName.AssemblyName.Version!,
                data.TypeName.AssemblyName.GetPublicKey() != null,
                data.TypeName.AssemblyName.GetPublicKey() ?? data.TypeName.AssemblyName.GetPublicKeyToken());
        if (assemblyRef == null!)
        {
            Ctx.Console.Warning($"Failed to find vm type {data.Name} assembly reference!");
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
            return Ctx.Module.LookupMember<IFieldDescriptor>(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMFieldData data)
            throw new Exception("VM inline operand expected to have field data!");
        
        var declaringTypeSig = ResolveType(data.DeclaringType.Position);
        var declaringType = declaringTypeSig?.Resolve();
        if (declaringType != null)
            return declaringType.Fields.FirstOrDefault(f => f.Name == data.Name)?.ImportWith(Ctx.Importer);
        
        Ctx.Console.Error($"Unable to resolve vm field {data.Name} declaring type {declaringTypeSig?.Name} to a TypeDef!");
        return null;
    }
    
    private bool VerifyMethodParameters(MethodSignatureBase method, VMMethodData data) =>
        method.ParameterTypes.Count == data.Parameters.Length && method.ParameterTypes
            .Zip(data.Parameters).All(x =>
                x.First is GenericParameterSignature or GenericInstanceTypeSignature || x.First.FullName ==
                ResolveType(x.Second.Position)?.FullName);

    private bool VerifyMethodParameters(MethodDefinition method, VMMethodInfo data)
    {
        var skip = 0;
        if (method.Parameters.ThisParameter != null && data.VMParameters.Count > 0 &&
            method.Parameters.ThisParameter.ParameterType.FullName ==
            ResolveType(data.VMParameters[0].VMType)?.FullName)
            skip++;

        return (method.Parameters.Count == data.VMParameters.Count || method.Parameters.Count == data.VMParameters.Count - skip) && method.Parameters
            .Zip(data.VMParameters.Skip(skip)).All(x =>
                x.First.ParameterType is GenericParameterSignature or GenericInstanceTypeSignature ||
                (x.First.ParameterType is TypeSpecificationSignature tss &&
                 (tss.BaseType is GenericParameterSignature or GenericInstanceTypeSignature ||
                  tss.BaseType.FullName == ResolveType(x.Second.VMType)?.FullName)) ||
                x.First.ParameterType?.FullName == ResolveType(x.Second.VMType)?.FullName);
    }
    
    private bool VerifyMethodParameters(MethodDefinition method, VMMethodData data)
    {
        var skip = 0;
        if (method.Parameters.ThisParameter != null && data.Parameters.Length > 0 &&
            method.Parameters.ThisParameter.ParameterType.FullName ==
            ResolveType(data.Parameters[0].Position)?.FullName)
            skip++;


        return (method.Parameters.Count == data.Parameters.Length || method.Parameters.Count == data.Parameters.Length - skip) && method.Parameters
            .Zip(data.Parameters.Skip(skip)).All(x =>
                // TODO: Should probably resolve these generic parameter signatures and verify them too
                x.First.ParameterType is GenericParameterSignature or GenericInstanceTypeSignature ||
                (x.First.ParameterType is TypeSpecificationSignature tss &&
                 (tss.BaseType is GenericParameterSignature or GenericInstanceTypeSignature ||
                  tss.BaseType.FullName == ResolveType(x.Second.Position)?.FullName)) ||
                x.First.ParameterType?.FullName == ResolveType(x.Second.Position)?.FullName);
        ;
    }
    
    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodInfo data) =>
        declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name
                                                   && (m.Signature?.ReturnType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                       (m.Signature?.ReturnType is TypeSpecificationSignature tss &&
                                                        (tss.BaseType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                         tss.BaseType.FullName == ResolveType(data.VMReturnType)?.FullName)) ||
                                                       m.Signature?.ReturnType?.FullName ==
                                                       ResolveType(data.VMReturnType)?.FullName)
                                                   && VerifyMethodParameters(m, data));

    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodData data) =>
            declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name
                                                   // TODO: Should probably resolve these generic parameter signatures and verify them too
                                                   && (m.Signature?.ReturnType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                       (m.Signature?.ReturnType is TypeSpecificationSignature tss &&
                                                        (tss.BaseType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                         tss.BaseType.FullName == ResolveType(data.ReturnType.Position)?.FullName)) ||
                                                       m.Signature?.ReturnType?.FullName ==
                                                       ResolveType(data.ReturnType.Position)?.FullName)
                                                   && VerifyMethodParameters(m, data));
    
    public IMethodDescriptor? ResolveMethod(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return Ctx.Module.LookupMember<IMethodDescriptor>(inlineOperand.Token);
        
        if (!inlineOperand.HasData)
            throw new Exception("VM inline operand expected to have data!");

        if (inlineOperand.Data is VMEazCallData eazCallData)
            return ResolveEazCall(eazCallData);

        if (inlineOperand.Data is not VMMethodData data)
            throw new Exception("VM inline operand expected to have method data!");

        var declaringTypeDefOrRef = ResolveType(data.DeclaringType.Position);
        if (declaringTypeDefOrRef == null)
        {
            Ctx.Console.Error($"Unable to resolve declaring type on vm method {data.Name}");
            return null;
        }

        var declaringTypeDefOrRefUnadorned = declaringTypeDefOrRef.Resolve();
        
        var returnType = ResolveType(data.ReturnType.Position);
        if (returnType == null)
        {
            Ctx.Console.Error($"Failed to resolve vm method {data.Name} return type!");
            return null;
        }

        if (declaringTypeDefOrRef.Scope?.GetAssembly()?.Name != Ctx.Module.Assembly?.Name)
        {
            var importedMemberRef = Ctx.Module.GetImportedMemberReferences().FirstOrDefault(x =>
                x.IsMethod
                && x.DeclaringType?.FullName == (declaringTypeDefOrRefUnadorned is null ? declaringTypeDefOrRef.FullName : declaringTypeDefOrRefUnadorned.FullName)
                && x.Name == data.Name
                && x.Signature is MethodSignature ms
                // TODO: Fix resolving imported member references of methods returning a generic parameter
                && (ms.ReturnType is GenericParameterSignature or GenericInstanceTypeSignature ||
                    ms.ReturnType.FullName == returnType.FullName)
                && ms.GenericParameterCount == data.GenericArguments.Length
                && VerifyMethodParameters(ms, data));

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
                    .MakeGenericInstanceMethod(data.GenericArguments
                        .Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
                    .ImportWith(Ctx.Importer);
            return methodDef?.ImportWith(Ctx.Importer);
        }

        // stuff below should never execute
        // this doesn't support generics in return types

        var declaringTypeSig = declaringTypeDefOrRef.ToTypeSignature();
        var parameters = data.Parameters.Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray();
        var newParams = new List<TypeSignature>();
        foreach (var parameter in parameters)
        {
            if (declaringTypeSig is GenericInstanceTypeSignature declaringTypeGenericSig)
            {
                var f = declaringTypeGenericSig.TypeArguments.FirstOrDefault(x => x.FullName == parameter.FullName);
                if (f != null)
                    newParams.Add(new GenericParameterSignature(GenericParameterType.Type,
                        declaringTypeGenericSig.TypeArguments.IndexOf(f)));
            }
            else
                newParams.Add(parameter);
        }

        var memberRef = declaringTypeDefOrRef
            .CreateMemberReference(data.Name, data.IsStatic
                ? MethodSignature.CreateStatic(
                    returnType.ToTypeSignature(), data.GenericArguments.Length,
                    newParams)
                : MethodSignature.CreateInstance(
                    returnType.ToTypeSignature(), data.GenericArguments.Length,
                    newParams));

        if (data.HasGenericArguments)
            return memberRef
                .MakeGenericInstanceMethod(data.GenericArguments
                    .Select(g => ResolveType(g.Position)!.ToTypeSignature()).ToArray())
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
                ITypeDescriptor typeDesc => typeDesc.ToTypeSignature(),
                IFieldDescriptor fieldDesc => fieldDesc,
                IMethodDescriptor methodDesc => methodDesc,
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
            VMInlineOperandType.EazCall => ResolveEazCall(position),
            // VMInlineOperandType.UserString => ResolveString(position),
            _ => throw new ArgumentOutOfRangeException(nameof(inlineOperand.Data.Type), inlineOperand.Data.Type, "VM inline operand data is neither Type, Field, Method, nor EazCall!")
        };
    }

    public IMethodDescriptor? ResolveEazCall(VMEazCallData eazCallData) => 
        ResolveMethod(eazCallData.VMMethodPosition);

    public IMethodDescriptor? ResolveEazCall(int value)
    {
        var noGenericArgs = (value & 0x80000000) != 0; 
        // var maybeSomethingWithDeclaringType  = (value & 0x40000000) != 0;
        var position = value & 0x3FFFFFFF;

        return noGenericArgs ? ResolveEazCall_Helper(position, null, null) : ResolveMethod(position);
    }

    private IMethodDescriptor? ResolveEazCall_Helper(int position, ITypeDefOrRef[]? genericTypes, ITypeDefOrRef[]? declaringGenericTypes)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var methodInfo = new VMMethodInfo(VMStreamReader);

        var declaringType = ResolveType(methodInfo.VMDeclaringType);
        if (declaringType is null)
            throw new Exception("Failed to resolve eaz call declaring type!");
        
        var returnType = ResolveType(methodInfo.VMReturnType);
        if (returnType == null)
        {
            Ctx.Console.Error($"Failed to resolve eaz call {methodInfo.Name} return type!");
            return null;
        }

        var method = ResolveMethod(declaringType.Resolve(), methodInfo);
        if (method is null)
        {
            Ctx.Console.Error($"Failed to resolve eaz call {methodInfo.Name}!");
            return null;
        }

        return method;
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