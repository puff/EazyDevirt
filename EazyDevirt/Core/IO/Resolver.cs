using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.DotNet.Signatures.Types.Parsing;
using EazyDevirt.Architecture;
using EazyDevirt.Architecture.InlineOperands;
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
        if (inlineOperand.IsToken)
            return ((TypeDefinition)Ctx.Module.LookupMember(inlineOperand.Token)).ToTypeSignature();
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMTypeData data)
            throw new Exception("VM inline operand expected to have type data!");

        var typeSignature = TypeNameParser.Parse(Ctx.Module, data.Name);
        if (typeSignature == null!)
        {
            Ctx.Console.Error($"Failed to resolve vm type {data.Name}");
            return null;
        }

        // var assembly = typeSignature.Scope?.GetAssembly()!;
        // var assemblyResolver = Ctx.Module.MetadataResolver.AssemblyResolver;
        // if (!assemblyResolver.HasCached(assembly))
        // {
        //     if (assembly.FullName == Ctx.Module.Assembly!.FullName)
        //     {
        //         // it works, okay?
        //         assemblyResolver.AddToCache(assembly, Ctx.Module.Assembly);
        //         assembly.ImportWith(Ctx.Importer);
        //     }
        //     else
        //     {
        //         var assemblyDefinition = assemblyResolver.Resolve(assembly);
        //
        //         if (assemblyDefinition == null)
        //         {
        //             Ctx.Console.Error("Failed resolving assembly " + assembly.FullName);
        //             return null;
        //         }
        //     }
        // }
        
        if (!data.HasGenericTypes)
            return typeSignature;

        var generics = data.GenericTypes.Select(g => ResolveType(g.Position)!).ToArray();
        return typeSignature.MakeGenericInstanceType(generics);
    }
    
    public FieldDefinition? ResolveField(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return (FieldDefinition)Ctx.Module.LookupMember(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMFieldData data)
            throw new Exception("VM inline operand expected to have field data!");
        
        var declaringType = ResolveType(data.FieldType.Position)?.Resolve();
        if (declaringType == null)
            throw new Exception("Unable to resolve vm field declaring type as TypeDef!");
        
        return declaringType.Fields.FirstOrDefault(f => f.Name == data.Name);
    }

    public IMethodDescriptor? ResolveMethod(int position)
    {
        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var inlineOperand = new VMInlineOperand(VMStreamReader);
        if (inlineOperand.IsToken)
            return (MethodDefinition)Ctx.Module.LookupMember(inlineOperand.Token);
        
        if (!inlineOperand.HasData || inlineOperand.Data is not VMMethodData data)
            throw new Exception("VM inline operand expected to have method data!");
        
        var declaringType = ResolveType(data.DeclaringType.Position);
        if (declaringType == null)
            throw new Exception("Unable to resolve vm method declaring type!");

        var typeDef = declaringType.Resolve();
        var method = typeDef?.Methods.FirstOrDefault(m => m.Name == data.Name 
                                                          && m.Parameters.Count == data.Parameters.Length
                                                          && m.Parameters.Where((p, i) => 
                                                              p.ParameterType.FullName == ResolveType(data.Parameters[i].Position)?.FullName).Count() == data.Parameters.Length);
        if (method == null)
        {
            Ctx.Console.Error($"Failed to resolve vm method {data.Name}");
            return null;
        }
       
        // TODO: verify declaringType's generic types are working with the resolved method
        var methodDefOrRef = method.ImportWith(Ctx.Importer);

        if (!data.HasGenericArguments)
            return methodDefOrRef;

        var generics = data.GenericArguments.Select(g => ResolveType(g.Position)!).ToArray();
        return methodDefOrRef.MakeGenericInstanceMethod(generics);
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
            VMInlineOperandType.Type => ResolveType(position)?.ToTypeDefOrRef(),
            VMInlineOperandType.Field => ResolveField(position),
            VMInlineOperandType.Method => ResolveMethod(position),
            _ => throw new ArgumentOutOfRangeException(nameof(inlineOperand.Data.Type), inlineOperand.Data.Type, "VM inline operand data is neither Type, Field, nor Method!")
        };
    }
    
    public MethodDefinition? ResolveEazCall(int value)
    {
        var noGenericVars = (value & 0x80000000) != 0; 
        // var forceResolveGenericVars = (value & 0x40000000) != 0;
        var position = value & 0x3FFFFFFF;

        Ctx.VMResolverStream.Seek(position, SeekOrigin.Begin);
        
        var methodInfo = new VMMethodInfo(VMStreamReader);

        var declaringType = ResolveType(methodInfo.VMDeclaringType)?.Resolve();
        if (declaringType == null)
            throw new Exception("Unable to resolve vm eaz call declaring type");

        // TODO: generic types support
        return declaringType.Methods.FirstOrDefault(m => m.Name == methodInfo.Name);
        // if (!data.HasGenericArguments)
        //     return GetMethodSignatureFromDeclaringType(declaringType, methodInfo.Name, null);
        //
        // var generics = data.GenericArguments.Select(g => ResolveType(g.Position)!).ToArray();
        // return GetMethodSignatureFromDeclaringType(declaringType, methodInfo.Name, generics);
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