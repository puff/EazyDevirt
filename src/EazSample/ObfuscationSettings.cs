using System.Reflection;

// Encrypt and compress any .png resources
[assembly: Obfuscation(Feature = "encrypt resources [compress] *.png", Exclude = false)]

// Encrypt any .jpg resources
[assembly: Obfuscation(Feature = "encrypt resources *.jpg", Exclude = false)]

// Encrypt symbol names with password "EazSample"
[assembly: Obfuscation(Feature = "encrypt symbol names with password EazSample", Exclude = false)]