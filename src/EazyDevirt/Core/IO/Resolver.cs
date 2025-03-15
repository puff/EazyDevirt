using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Parsing;
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
        Cache = new Dictionary<int, object?>();
    }
    
    private DevirtualizationContext Ctx { get; }
    
    private VMBinaryReader VMStreamReader { get; }
    
    private Dictionary<int, object?> Cache { get; }

    public ITypeDefOrRef? ResolveType(VMInlineOperand inlineOperand)
    {
        if (inlineOperand.IsToken)
            return Ctx.Module.LookupMember<ITypeDefOrRef>(inlineOperand.Token);
        return ResolveType(inlineOperand.Position);
    }
    
    public ITypeDefOrRef? ResolveType(int position)
    {
        if (Cache.TryGetValue(position, out var result))
            return (ITypeDefOrRef?)result;
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var lookup = Ctx.Module.LookupMember<ITypeDefOrRef>(inlineOperand.Token);
            Cache.Add(position, lookup);
            return lookup;
        }

        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        if (data.IsGenericParameterIndex)
        {
            if (data.GenericMethodParameterIndex >= 0)
            {
                var genericTypeDefOrRef = new GenericParameterSignature(Ctx.Module, GenericParameterType.Method, data.GenericMethodParameterIndex).ToTypeDefOrRef();
                Cache.Add(position, genericTypeDefOrRef);
                return genericTypeDefOrRef;
            }
            
            if (data.GenericTypeParameterIndex >= 0)
            {
                var genericTypeDefOrRef = new GenericParameterSignature(Ctx.Module, GenericParameterType.Type, data.GenericTypeParameterIndex).ToTypeDefOrRef();
                Cache.Add(position, genericTypeDefOrRef);
                return genericTypeDefOrRef;
            }
        }

        var typeSig = TypeNameParser.Parse(Ctx.Module, data.Name);
        if (typeSig is null)
            throw new Exception($"Failed to parse vm type {data.Name}");

        if (data.HasGenericTypeParameters)
            typeSig = typeSig.MakeGenericInstanceType(data.GenericParameters.Select(x => ResolveType(x)!.ToTypeSignature()).ToArray());

        var typeDefOrRef = typeSig.ToTypeDefOrRef();
        if (SignatureComparer.Default.Equals(typeDefOrRef.Scope?.GetAssembly(), Ctx.Module.Assembly))
        {
            var resolvedType = typeDefOrRef.Resolve();
            if (resolvedType is not null)
                typeDefOrRef = resolvedType;
            else
                Ctx.Console.Warning($"Failed to resolve same assembly vm type {data.Name}, using its reference instead.");
        }
        else
            typeDefOrRef = Ctx.Importer.ImportType(typeDefOrRef);
        
        Cache.Add(position, typeDefOrRef);
        return typeDefOrRef;
    }
    
    public IFieldDescriptor? ResolveField(int position)
    {
        if (Cache.TryGetValue(position, out var result))
            return (IFieldDescriptor?)result;

        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var lookup = Ctx.Module.LookupMember<IFieldDescriptor>(inlineOperand.Token);
            Cache.Add(position, lookup);
            return lookup;
        }

        if (!inlineOperand.HasData || inlineOperand.Data is not VMFieldData data)
            throw new Exception("VM inline operand expected to have field data!");

        var declaringType = ResolveType(data.DeclaringType.Position);
        if (declaringType is null)
        {
            Ctx.Console.Error($"Unable to resolve vm field {data.Name} declaring type!");
            Cache.Add(position, null);
            return null;
        }

        if (declaringType.Resolve() is { } declaringTypeDef)
        {
            var fieldDef = declaringTypeDef.Fields.FirstOrDefault(f => f.Name == data.Name && f.IsStatic == data.IsStatic)?.ImportWith(Ctx.Importer);
            Cache.Add(position, fieldDef);
            return fieldDef;
        }

        // we can't create our own reference since we don't know the field's type.
        // maybe it could be inferred from where it's being used, but that would require a lot of rework.
        Ctx.Console.Error($"Unable to resolve vm field {declaringType.Name}.{data.Name}");
        return null;
    }

    private TypeSignature GetBaseTypeOfTypeSignature(TypeSignature ts)
    {
        while (ts is TypeSpecificationSignature tss)
            ts = tss.BaseType;
        return ts;
    }
    
    private IMethodDescriptor? ResolveMethod(string name, ITypeDefOrRef declaringTypeDefOrRef, MethodSignature vmMethodSig, TypeSignature[] vmParameters, GenericContext vmGenericContext)
    {
        // Methods utilizing generics need to be resolved to get 100% accuracy.
        // This is because Eazfuscator uses the actual generic type instead of the generic parameter type.
        // Example:
        // The signature we need is:
        // !0 class TestClass`1<int32>::TestMethod<string, int32>(!0, string, !!1, !!0)
        // However, the signature Eazfuscator gives us is:
        // int32 class TestClass`1<int32>::TestMethod<string, int32>(int32, string, int32, string)
        // We can't convert them into their generic parameter type because we don't know the position of the generic type in the arguments, so we could inadvertently replace the wrong argument:
        // !0 class TestClass`1<int32>::TestMethod<string, int32>(!0, !!0, !!1, string)
        // In the future, we could probably implement something with Echo to analyze devirtualized code to find the correct type positions, but for now this is the easiest way.
        if (!vmGenericContext.IsEmpty)
        {
            var declaringTypeDef = declaringTypeDefOrRef.Resolve();
            if (declaringTypeDef is null)
            {
                if (Ctx.Options.RequireDepsForGenericMethods)
                {
                    Ctx.Console.Error($"Unable to resolve generic method {name} declaring type (Assembly: {declaringTypeDefOrRef.Scope?.GetAssembly()?.FullName})!");
                    return null;
                }
                
                Ctx.Console.Warning($"Unable to resolve generic method {name} declaring type (Assembly: {declaringTypeDefOrRef.Scope?.GetAssembly()?.FullName})! Method reference may be broken.");
                
                // This is fragile and will probably break your method signatures.
            
                var methodRef = declaringTypeDefOrRef.CreateMemberReference(name, vmMethodSig);
                if (vmGenericContext.Method?.TypeArguments.Count > 0)
                    return methodRef.MakeGenericInstanceMethod(vmGenericContext.Method.TypeArguments.ToArray());
                return methodRef;
            }

            // Look for matching methods
            foreach (var method in declaringTypeDef.Methods)
            {
                if (method.IsStatic != !vmMethodSig.HasThis
                    || method.Name != name
                    || method.GenericParameters.Count != (vmGenericContext.Method?.TypeArguments.Count ?? 0)) 
                    continue;
                
                if (method.Parameters.Count != vmParameters.Length)
                    continue;
                    
                // Found a potential match, now build a signature from the real method and compare it with the signature built from our vm data to ensure we have the correct method
    
                // Instantiate generic types for real method from vm method data
                var realParameters = method.Parameters.Select(x => x.ParameterType).ToArray();
                for (var i = 0; i < vmParameters.Length; i++)
                    if (GetBaseTypeOfTypeSignature(vmParameters[i]) is GenericParameterSignature genericParameterSignature)
                        realParameters[i] = vmGenericContext.GetTypeArgument(genericParameterSignature);
    
                var realReturnType = method.Signature!.ReturnType;
                if (GetBaseTypeOfTypeSignature(realReturnType) is GenericParameterSignature)
                    realReturnType = vmMethodSig.ReturnType;
    
                var realMethodSignature = (method.IsStatic
                        ? MethodSignature.CreateStatic(realReturnType, vmGenericContext.Method?.TypeArguments.Count ?? 0,
                            realParameters)
                        : MethodSignature.CreateInstance(realReturnType, vmGenericContext.Method?.TypeArguments.Count ?? 0,
                            realParameters))
                    .InstantiateGenericTypes(vmGenericContext);
    
                // Compare instantiated real method signature with our signature built from vm method data
                if (!SignatureComparer.Default.Equals(realMethodSignature, vmMethodSig))
                    continue;
    
                if (vmGenericContext.Method?.TypeArguments.Count > 0)
                    return declaringTypeDefOrRef.CreateMemberReference(method.Name, method.Signature)
                        .MakeGenericInstanceMethod(vmGenericContext.Method.TypeArguments.ToArray());
    
                return declaringTypeDefOrRef.CreateMemberReference(method.Name, method.Signature);
            }
            
            Ctx.Console.Error($"Failed to resolve generic method {name}!");
            return null;
        }
        
        return declaringTypeDefOrRef.CreateMemberReference(name, vmMethodSig);
    }
    
    public IMethodDescriptor? ResolveMethod(int position)
    {
        if (Cache.TryGetValue(position, out var result))
            return (IMethodDescriptor?)result;
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var lookup = Ctx.Module.LookupMember<IMethodDescriptor>(inlineOperand.Token);
            Cache.Add(position, lookup);
            return lookup;
        }
        
        if (!inlineOperand.HasData)
            throw new Exception("VM inline operand expected to have data!");

        if (inlineOperand.Data is not VMMethodData data)
            throw new Exception("VM inline operand expected to have method data!");

        var declaringTypeDefOrRef = ResolveType(data.DeclaringType.Position);
        if (declaringTypeDefOrRef == null)
        {
            Ctx.Console.Error($"Failed to resolve declaring type on vm method {data.Name}!");
            return null;
        }

        var returnType = ResolveType(data.ReturnType.Position);
        if (returnType == null)
        {
            Ctx.Console.Error($"Failed to resolve return type on vm method {data.Name}!");
            return null;
        }
        
        var vmGenericInstanceMethodSig = data.HasGenericParameters ? new GenericInstanceMethodSignature(data.GenericParameters.Select(x => ResolveType(x)?.ToTypeSignature()!)) : null;
        var vmGenericContext = GenericContext.FromType(declaringTypeDefOrRef).WithMethod(vmGenericInstanceMethodSig!);
        var vmParameters = data.Parameters.Select(x => ResolveType(x)?.ToTypeSignature()!).ToArray();

        var vmMethodSig = (data.IsStatic
                ? MethodSignature.CreateStatic(returnType.ToTypeSignature(),
                    vmGenericContext.Method?.TypeArguments.Count ?? 0, vmParameters)
                : MethodSignature.CreateInstance(returnType.ToTypeSignature(),
                    vmGenericContext.Method?.TypeArguments.Count ?? 0, vmParameters))
            .InstantiateGenericTypes(vmGenericContext);
        
        var method = ResolveMethod(data.Name, declaringTypeDefOrRef, vmMethodSig, vmParameters, vmGenericContext)?.ImportWith(Ctx.Importer);
        if (method is null) 
            return null;
        
        Cache.Add(position, method);
        return (IMethodDescriptor)method;
    }

    public IMemberDescriptor? ResolveToken(int position)
    {
        if (Cache.TryGetValue(position, out var result))
            return (IMemberDescriptor?)result;
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var member = Ctx.Module.LookupMember(inlineOperand.Token);

            Cache.Add(position, member);
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

    public IMethodDescriptor? ResolveEazCall(int value)
    {
        var noGenericArgs = (value & 0x80000000) != 0; 
        var useConstrainedType  = (value & 0x40000000) != 0;
        var position = value & 0x3FFFFFFF;
        if (Cache.TryGetValue(position, out var result))
            return (IMethodDescriptor?)result;

        if (noGenericArgs)
            return ResolveEazCall(position, new GenericContext(), useConstrainedType);
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var lookup = Ctx.Module.LookupMember<IMethodDescriptor>(inlineOperand.Token);
            Cache.Add(position, lookup);
            return lookup;
        }
        
        if (!inlineOperand.HasData)
            throw new Exception("VM inline operand expected to have data!");

        if (inlineOperand.Data is not VMEazCallData data)
            throw new Exception("VM inline operand expected to have eazcall data!");

        return ResolveEazCall(data);
    }

    private IMethodDescriptor? ResolveEazCall(VMEazCallData eazCallData)
    {
        var useConstrainedType  = (eazCallData.EazCallValue & 0x40000000) != 0;
        var position = eazCallData.EazCallValue & -0x40000001;

        var method = ResolveMethod(eazCallData.VMMethodPosition);
        var genericContext = GenericContext.FromMethod(method!);
        
        return ResolveEazCall(position, genericContext, useConstrainedType);
    }

    private IMethodDescriptor? ResolveEazCall(int position, GenericContext vmGenericContext, bool useConstrainedType)
    {
        if (Cache.TryGetValue(position, out var result))
            return (IMethodDescriptor?)result;
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);

        var methodInfo = new VMMethodInfo(VMStreamReader);
        
        // i don't think anything special needs to be done for constrained
        // eaz just includes the types needed in the data anyway
        // but log it just in case
        if (useConstrainedType)
            Ctx.Console.Warning("Constrained type EazCall detected");

        var declaringTypeDefOrRef = ResolveType(methodInfo.VMDeclaringType);
        if (declaringTypeDefOrRef is null)
            throw new Exception("Failed to resolve eaz call declaring type!");
        
        var returnType = ResolveType(methodInfo.VMReturnType);
        if (returnType == null)
        {
            Ctx.Console.Error($"Failed to resolve eaz call {methodInfo.Name} return type!");
            return null;
        }
        
        var vmParameters = methodInfo.VMParameters.Select(x => ResolveType(x.VMType)?.ToTypeSignature()!).ToArray();

        var vmMethodSig = (methodInfo.IsStatic
                ? MethodSignature.CreateStatic(returnType.ToTypeSignature(),
                    vmGenericContext.Method?.TypeArguments.Count ?? 0, vmParameters)
                : MethodSignature.CreateInstance(returnType.ToTypeSignature(),
                    vmGenericContext.Method?.TypeArguments.Count ?? 0, vmParameters.Skip(1))) // instance eaz calls will contain declaring type as first parameter
            .InstantiateGenericTypes(vmGenericContext);
        
        var method = ResolveMethod(methodInfo.Name, declaringTypeDefOrRef, vmMethodSig, vmParameters, vmGenericContext);
        if (method is null)
        {        
            Ctx.Console.Error($"Failed to resolve eaz call {methodInfo.Name}!");
            return null;
        }

        Cache.Add(position, method);
        return method;
    }

    public string ResolveString(int position)
    {
        if (Cache.TryGetValue(position, out var result))
            return (string)result!;
        
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
        {
            var lookup = Ctx.Module.LookupString(inlineOperand.Token);
            Cache.Add(position, lookup);
            return lookup;
        }

        if (!inlineOperand.HasData || inlineOperand.Data is not VMUserStringData data) 
            throw new Exception("VM inline operand expected to have string data!");
        
        Cache.Add(position, data.Value);
        return data.Value;
    }
}