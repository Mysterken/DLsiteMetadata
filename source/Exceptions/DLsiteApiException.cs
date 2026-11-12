using System;

namespace DLsiteMetadata.Exceptions;

/// <summary>DLsite answered, but not in a way that holds a product (unexpected status, code or shape).</summary>
public class DLsiteApiException : Exception
{
    public DLsiteApiException(string message) : base(message) { }
}
