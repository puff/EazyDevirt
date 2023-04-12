using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using EazyDevirt.Core;
using EazyDevirt.Core.Abstractions;
using EazyDevirt.Core.IO;
using EazyDevirt.Logging;
using EazyDevirt.PatternMatching;
using EazyDevirt.PatternMatching.Patterns;
using Org.BouncyCastle.Math;

#pragma warning disable CS8618

namespace EazyDevirt.Devirtualization.Pipeline;

internal sealed class ResourceParsing : StageBase
{
    private byte[] _keyBytes;
    private string _modulusString;
    private ManifestResource? _resource;
    private MethodDefinition? _resourceGetterMethod;
    private MethodDefinition? _resourceInitializationMethod;
    private MethodDefinition? _resourceModulusStringMethod;

    private string _resourceString;

    public ResourceParsing(Context ctx) : base(ctx)
    {
    }

    private protected override bool Init()
    {
        var found = FindVMStreamMethods();

        if (_resourceGetterMethod == null)
            Logger.Error("Failed to find VM resource stream getter method.");

        if (_resourceInitializationMethod == null)
            Logger.Error("Failed to find VM resource stream initialization method.");

        if (_resourceModulusStringMethod == null || _resourceModulusStringMethod.CilMethodBody!.Instructions.All
                (i => i.OpCode != CilOpCodes.Ldstr))
            Logger.Error("Failed to find valid VM resource modulus string method. Have strings been decrypted?");

        if (found)
        {
            Logger.Success("Found VM resource stream getter, initializer, and modulus string methods!",
                VerboseLevel.Verbose);
            Logger.InfoStr("VM Resource Stream Getter", _resourceGetterMethod!.MetadataToken,
                VerboseLevel.VeryVerbose);
            Logger.InfoStr("VM Resource Stream Initializer", _resourceInitializationMethod!.MetadataToken,
                VerboseLevel.VeryVerbose);
            Logger.InfoStr("VM Resource Modulus String Method",
                _resourceModulusStringMethod!.MetadataToken, VerboseLevel.VeryVerbose);

            _resourceString = _resourceGetterMethod!.CilMethodBody!.Instructions[5].Operand?.ToString()!;
            _resource = Ctx.Module.Resources.FirstOrDefault(r => r.Name == _resourceString);
            if (_resource == null)
            {
                Logger.Error("Failed to get VM resource");
                found = false;
            }

            Logger.Success("Found VM resource!", VerboseLevel.Verbose);
            Logger.InfoStr("VM Resource", _resourceString, VerboseLevel.VeryVerbose);

            var a1 = (SerializedFieldDefinition)_resourceGetterMethod!.CilMethodBody!.Instructions[10].Operand!;
            if (!a1.HasFieldRva || a1.FieldRva!.GetType() != typeof(DataSegment))
            {
                Logger.Error("Failed to get VM resource stream key byte array.");
                found = false;
            }

            _keyBytes = ((DataSegment)a1.FieldRva!).Data;

            Logger.Success("Found VM resource stream key bytes!", VerboseLevel.Verbose);
            Logger.InfoStr("VM Resource Stream Key Bytes", BitConverter.ToString(_keyBytes),
                VerboseLevel.VeryVerbose);

            _modulusString = _resourceModulusStringMethod!.CilMethodBody!.Instructions.FirstOrDefault
                (i => i.OpCode == CilOpCodes.Ldstr)?.Operand?.ToString()!;
            if (string.IsNullOrWhiteSpace(_modulusString))
            {
                Logger.Error("VM resource modulus string is null.");
                found = false;
            }

            Logger.Success("Found VM resource modulus string!", VerboseLevel.Verbose);
            Logger.InfoStr("VM Resource Modulus String", _modulusString, VerboseLevel.VeryVerbose);
        }

        Ctx.VMResourceGetterMdToken = _resourceGetterMethod!.MetadataToken;

        return found;
    }

    public override bool Run()
    {
        // the fun begins...
        if (!Init()) return false;

        var modulus1 = Convert.FromBase64String(_modulusString);
        var modulus2 = new byte[_keyBytes.Length + _keyBytes.Length];
        Buffer.BlockCopy(_keyBytes, 0, modulus2, 0, _keyBytes.Length);
        Buffer.BlockCopy(modulus1, 0, modulus2, _keyBytes.Length, modulus1.Length);

        var mod = new BigInteger(1, modulus2);
        var exp = BigInteger.ValueOf(65537L);

        var buffer = _resource!.GetData()!;
        Ctx.VMStream = new VMCipherStream(buffer, mod, exp);
        Ctx.VMResolverStream = new VMCipherStream(buffer, mod, exp);

        return true;
    }

    private bool FindVMStreamMethods()
    {
        foreach (var type in Ctx.Module.GetAllTypes())
        {
            if (_resourceGetterMethod != null && _resourceInitializationMethod != null) return true;
            foreach (var method in type.Methods.Where(m =>
                         m is { Managed: true, IsPublic: true, IsStatic: true } &&
                         m.Signature?.ReturnType.FullName == typeof(Stream).FullName))
            {
                if (_resourceGetterMethod != null && _resourceInitializationMethod != null) return true;
                // TODO: make a better way of using non-vm patterns
                if (_resourceGetterMethod != null ||
                    !PatternMatcher.MatchesPattern(new GetVMStreamPattern(), method)) continue;

                _resourceGetterMethod = method;
                _resourceInitializationMethod =
                    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[13].Operand!;
                _resourceModulusStringMethod =
                    (SerializedMethodDefinition)method.CilMethodBody!.Instructions[12].Operand!;
                var getVmInstanceMethod = type.Methods.First(m =>
                    m.MetadataToken != _resourceGetterMethod.MetadataToken &&
                    m.MetadataToken != _resourceModulusStringMethod.MetadataToken);

                if (!getVmInstanceMethod.Signature!.ReturnsValue)
                    throw new Exception("Failed to get VM Declaring type!");

                Ctx.VMDeclaringType = (TypeDefinition)getVmInstanceMethod.Signature.ReturnType.ToTypeDefOrRef();
            }
        }

        return false;
    }
}