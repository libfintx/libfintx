using System;
using System.Collections.Generic;

namespace libfintx.FinTS.Exceptions;

/// <summary>
/// The TAN procedure set in <see cref="FinTsClient.HIRMS"/> is not among the procedures the bank allows for this user
/// (return value 3920). Choose one of <see cref="AllowedTanProcedures"/>. It is raised while the answer is parsed and
/// reaches the caller as an inner exception of the exception the business transaction throws.
/// </summary>
public class TanProcedureNotAllowedException : Exception
{
    public TanProcedureNotAllowedException(string tanProcedure, IReadOnlyList<string> allowedTanProcedures)
        : base($"Invalid HIRMS/Tan-Mode {tanProcedure} detected. Please choose one of the allowed modes: {string.Join(";", allowedTanProcedures)}")
    {
        TanProcedure = tanProcedure;
        AllowedTanProcedures = allowedTanProcedures;
    }

    /// <summary>The TAN procedure that was requested.</summary>
    public string TanProcedure { get; }

    /// <summary>The TAN procedures the bank allows for this user, as reported with 3920.</summary>
    public IReadOnlyList<string> AllowedTanProcedures { get; }
}
