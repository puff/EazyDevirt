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

        var imported = typeSig.ToTypeDefOrRef().ImportWith(Ctx.Importer);
        Cache.Add(position, imported);
        return imported;
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
        Ctx.Console.Error($"Unable to resolve vm field {declaringType?.Name}.{data.Name}");
        return null;
    }
    
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
    
    private MethodDefinition? ResolveMethod(TypeDefinition? declaringType, VMMethodInfo data) =>
        declaringType?.Methods.FirstOrDefault(m => m.Name == data.Name
                                                   && (m.Signature?.ReturnType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                       (m.Signature?.ReturnType is TypeSpecificationSignature tss &&
                                                        (tss.BaseType is GenericParameterSignature or GenericInstanceTypeSignature ||
                                                         tss.BaseType.FullName == ResolveType(data.VMReturnType)?.FullName)) ||
                                                       m.Signature?.ReturnType?.FullName ==
                                                       ResolveType(data.VMReturnType)?.FullName)
                                                   && VerifyMethodParameters(m, data));

    private TypeSignature GetBaseTypeOfTypeSignature(TypeSignature ts)
    {
        while (ts is TypeSpecificationSignature tss)
            ts = tss.BaseType;
        return ts;
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

        if (inlineOperand.Data is VMEazCallData eazCallData)
        {
            var resolvedEazCall = ResolveEazCall(eazCallData);
            Cache.Add(position, resolvedEazCall);
            return resolvedEazCall;
        }

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

        var declaringTypeGenericContext = GenericContext.FromType(declaringTypeDefOrRef);
        var parameters = data.Parameters.Select(x => ResolveType(x)?.ToTypeSignature()!).ToArray();

        // Methods utilizing generics need to be resolved to get 100% accuracy.
        // This is because Eazfuscator uses the actual generic type instead of the generic parameter type.
        // Example:
        // The signature we need is:
        // !0 class TestClass`1<int32>::TestMethod<string, int32>(!0, string, !!1, !!0)
        // However, the signature Eazfuscator gives us is:
        // int32 class TestClass`1<int32>::TestMethod<string, int32>(int32, string, int32, string)
        // We can't convert them into their generic parameter type because we don't know the position of the generic type in the arguments, so we could inadvertently replace the wrong argument:
        // !0 class TestClass`1<int32>::TestMethod<string, int32>(!0, !!0, !!1, string)
        var declaringTypeIsGeneric = declaringTypeDefOrRef is TypeSpecification { Signature: GenericInstanceTypeSignature { TypeArguments.Count: > 0 } declaringTypeGenericSig};
        if (data.HasGenericParameters || declaringTypeIsGeneric)
        {
            var declaringTypeDef = declaringTypeDefOrRef.Resolve();
            if (declaringTypeDef is null)
            {
                if (Ctx.Options.RequireDepsForGenericMethods)
                {
                    Ctx.Console.Error($"Unable to resolve generic vm method {data.Name} declaring type (Assembly: {declaringTypeDefOrRef.Scope?.GetAssembly()?.FullName})!");
                    return null;
                }
                Ctx.Console.Warning($"Unable to resolve generic vm method {data.Name} declaring type (Assembly: {declaringTypeDefOrRef.Scope?.GetAssembly()?.FullName})! Method reference may be broken.");
                
                // This is inaccurate and will probably break your method signatures.
                var methodSignature = data.IsStatic 
                        ? MethodSignature.CreateStatic(returnType.ToTypeSignature(), data.GenericParameters.Length, parameters)
                        : MethodSignature.CreateInstance(returnType.ToTypeSignature(), data.GenericParameters.Length, parameters);

                var methodRef = declaringTypeDefOrRef.CreateMemberReference(data.Name, methodSignature);
                if (data.HasGenericParameters)
                {
                    var importedMethodGenericInstance = methodRef
                        .MakeGenericInstanceMethod(data.GenericParameters
                            .Select(x => ResolveType(x)?.ToTypeSignature()!).ToArray()).ImportWith(Ctx.Importer);
                    Cache.Add(position, importedMethodGenericInstance);
                    return importedMethodGenericInstance.ImportWith(Ctx.Importer);
                }

                var importedMethodRef = methodRef.ImportWith(Ctx.Importer);
                Cache.Add(position, importedMethodRef);
                return importedMethodRef;
            }

            // Look for matching methods
            foreach (var method in declaringTypeDef.Methods)
            {
                if (method.IsStatic == data.IsStatic
                    && method.Name == data.Name 
                    && method.GenericParameters.Count == data.GenericParameters.Length)
                {
                    // Skip past ThisParameter when comparing method parameters
                    var skip = 0;
                    if (method.Parameters.ThisParameter != null
                        && data.Parameters.Length > 0 
                        && SignatureComparer.Default.Equals(method.Parameters.ThisParameter.ParameterType, parameters[0]))
                        skip++;

                    // Check method parameters
                    if (method.Parameters.Count == parameters.Length 
                        || method.Parameters.Count == parameters.Length - skip
                        && method.Parameters.Zip(parameters.Skip(skip)).All(x => SignatureComparer.Default.Equals(x.First.ParameterType, x.Second)))
                    {
                        // Found a potential match, now build signatures from both the real method and vm method data and compare them to ensure we have the correct method

                        // Build signature from vm method data
                        var vmGenericParameters = data.GenericParameters.Select(x => ResolveType(x)?.ToTypeSignature()!)
                            .ToArray();
                        var vmMethodSig = (data.IsStatic
                                ? MethodSignature.CreateStatic(returnType.ToTypeSignature(), vmGenericParameters.Length,
                                    parameters)
                                : MethodSignature.CreateInstance(returnType.ToTypeSignature(),
                                    vmGenericParameters.Length, parameters))
                            .InstantiateGenericTypes(declaringTypeGenericContext);

                        // Instantiate generic types for real method from vm method data
                        var realParameters = method.Parameters.Select(x => x.ParameterType).ToArray();
                        for (var i = 0; i < parameters.Length; i++)
                            if (GetBaseTypeOfTypeSignature(parameters[i]) is GenericParameterSignature)
                                realParameters[i] = vmGenericParameters[i];

                        var realReturnType = method.Signature!.ReturnType;
                        if (GetBaseTypeOfTypeSignature(realReturnType) is GenericParameterSignature)
                            realReturnType = returnType.ToTypeSignature();

                        var realMethodSignature = (method.IsStatic
                                ? MethodSignature.CreateStatic(realReturnType, vmGenericParameters.Length,
                                    realParameters)
                                : MethodSignature.CreateInstance(realReturnType, vmGenericParameters.Length,
                                    realParameters))
                            .InstantiateGenericTypes(declaringTypeGenericContext);

                        // Compare instantiated real method signature with our signature built from vm method data
                        if (!SignatureComparer.Default.Equals(realMethodSignature, vmMethodSig))
                            continue;

                        if (data.HasGenericParameters)
                        {
                            
                            var genericInstanceMethod = declaringTypeDefOrRef.CreateMemberReference(method.Name, method.Signature)
                                .MakeGenericInstanceMethod(vmGenericParameters).ImportWith(Ctx.Importer);
                            Cache.Add(position, genericInstanceMethod);
                            return genericInstanceMethod;
                        }

                        var importedMethod = declaringTypeDefOrRef.CreateMemberReference(method.Name, method.Signature).ImportWith(Ctx.Importer);
                        Cache.Add(position, importedMethod);
                        return importedMethod;
                    }
                }
            }
        }
        
        var signature = data.IsStatic 
            ? MethodSignature.CreateStatic(returnType.ToTypeSignature(), data.GenericParameters.Length, parameters)
            : MethodSignature.CreateInstance(returnType.ToTypeSignature(), data.GenericParameters.Length, parameters);
        
        var methodDefOrRef = (IMethodDefOrRef)declaringTypeDefOrRef.CreateMemberReference(data.Name, signature).ImportWith(Ctx.Importer);
        Cache.Add(position, methodDefOrRef);
        return methodDefOrRef;
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
        if (Cache.TryGetValue(position, out var result))
            return (IMethodDescriptor?)result;
        
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