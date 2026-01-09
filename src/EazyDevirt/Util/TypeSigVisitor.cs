using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using EazyDevirt.Devirtualization;

namespace EazyDevirt.Util;

public class TypeSigVisitor : ITypeSignatureVisitor<TypeSignature>
{
    private AssemblyDescriptor _assembly;
    
    public TypeSigVisitor(AssemblyDescriptor assembly)
    {
        _assembly = assembly;
    }
    
    public TypeSignature VisitArrayType(ArrayTypeSignature signature)
    {
        signature.BaseType.AcceptVisitor(this);
        return signature;
    }

    public TypeSignature VisitBoxedType(BoxedTypeSignature signature)
    {
        signature.BaseType.AcceptVisitor(this);
        return signature;
    }

    public TypeSignature VisitByReferenceType(ByReferenceTypeSignature signature)
    {
        signature.BaseType.AcceptVisitor(this);
        return signature;
    }

    public TypeSignature VisitCorLibType(CorLibTypeSignature signature)
    {
        return signature;
    }

    public TypeSignature VisitCustomModifierType(CustomModifierTypeSignature signature)
    {
        if (SignatureComparer.Default.Equals(signature.ModifierType.Scope?.GetAssembly(), _assembly))
        {
            if (signature.ModifierType.Resolve() is { } typeDef)
                signature.ModifierType = typeDef;
            else
                DevirtualizationContext.Instance.Console.Error($"Failed to resolve CustomModifierTypeSignature {signature} to typedef.");
        }
        
        signature.BaseType.AcceptVisitor(this);
        return signature;
    }

    public TypeSignature VisitGenericInstanceType(GenericInstanceTypeSignature signature)
    {
        if (SignatureComparer.Default.Equals(signature.GenericType.Scope?.GetAssembly(), _assembly))
        {
            if (signature.GenericType.Resolve() is { } typeDef)
                signature.GenericType = typeDef;
            else
                DevirtualizationContext.Instance.Console.Error($"Failed to resolve GenericInstanceTypeSignature {signature} to typedef.");
        }
        
        foreach (var typeArgument in signature.TypeArguments)
            typeArgument.AcceptVisitor(this);
        
        return signature;
    }

    public TypeSignature VisitGenericParameter(GenericParameterSignature signature)
    {
        return signature;
    }

    public TypeSignature VisitPinnedType(PinnedTypeSignature signature)
    {
        return new PinnedTypeSignature(signature.BaseType.AcceptVisitor(this));
    }

    public TypeSignature VisitPointerType(PointerTypeSignature signature)
    {
        return new PointerTypeSignature(signature.BaseType.AcceptVisitor(this));
    }

    public TypeSignature VisitSentinelType(SentinelTypeSignature signature)
    {
        return signature;
    }

    public TypeSignature VisitSzArrayType(SzArrayTypeSignature signature)
    {
        return new SzArrayTypeSignature(signature.BaseType.AcceptVisitor(this));
    }

    public TypeSignature VisitFunctionPointerType(FunctionPointerTypeSignature signature)
    {
        return signature;
    }
    
    public TypeSignature VisitTypeDefOrRef(TypeDefOrRefSignature signature)
    {
        if (!SignatureComparer.Default.Equals(signature.Type.Scope?.GetAssembly(), _assembly)) 
            return signature;
        
        if (signature.Type.Resolve() is { } typeDef)
            signature.Type = typeDef;
        else
            DevirtualizationContext.Instance.Console.Error($"Failed to resolve TypeDefOrRefSignature {signature} to typedef.");

        return signature;
    }
}