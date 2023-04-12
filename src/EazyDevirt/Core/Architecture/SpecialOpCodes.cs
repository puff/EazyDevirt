namespace EazyDevirt.Core.Architecture;

/// <summary>
///     These opcodes typically pertain to actions within the vm itself.
/// </summary>
internal enum SpecialOpCodes : uint
{
    /// <summary>
    ///     Used when calling a virtualized method from within another virtualized method.
    /// </summary>
    EazCall, // 0x060003CD

    /// <summary>
    ///     Marks the beginning of eazfuscator's homomorphic encryption feature.
    /// </summary>
    StartHomomorphic, // 0x0600042D

    /// <summary>
    ///     Marks the end of eazfuscator's homomorphic encryption feature.
    /// </summary>
    EndHomomorphic // 0x06000414
}