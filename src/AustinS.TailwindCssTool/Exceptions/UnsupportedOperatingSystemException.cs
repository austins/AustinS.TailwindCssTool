namespace AustinS.TailwindCssTool.Exceptions;

/// <summary>
/// Exception for an unsupported operating system.
/// </summary>
/// <param name="message">The message that describes the error.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public sealed class UnsupportedOperatingSystemException(string? message = null, Exception? innerException = null)
    : Exception(message, innerException);
