namespace AustinS.TailwindCssTool.Exceptions;

/// <summary>
/// Exception for an unsupported operating system.
/// </summary>
public sealed class UnsupportedOperatingSystemException(string? message = null, Exception? innerException = null)
    : Exception(message, innerException);
