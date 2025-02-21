namespace AustinS.TailwindCssTool.Exceptions;

/// <summary>
/// Exception for when a Tailwind CSS binary is not found.
/// </summary>
public sealed class BinaryNotFoundException(string? message = null, Exception? innerException = null) : Exception(
    message,
    innerException);
