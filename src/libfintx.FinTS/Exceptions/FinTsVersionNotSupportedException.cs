using System;

namespace libfintx.FinTS.Exceptions;

public class FinTsVersionNotSupportedException : NotSupportedException
{
    public FinTsVersionNotSupportedException(FinTsVersion requestedVersion, FinTsVersion[] supportedVersions)
        : base($"HBCI/FinTS version {requestedVersion} is not supported. Supported versions are {string.Join(", ", supportedVersions)}")
    {
    }
}
