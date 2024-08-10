using System.ComponentModel.DataAnnotations;

namespace libfintx.FinTS;

/// <summary>
/// The HBCI/FinTS versions.
/// </summary>
// ReSharper disable once EnumUnderlyingTypeIsInt
public enum FinTsVersion : int
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming

    /// <summary>
    /// HBCI 2.2
    /// </summary>
    [Display(Name = "HBCI 2.2")]
    v220 = 220,

    /// <summary>
    /// FinTS 3.0
    /// </summary>
    [Display(Name = "FinTS 3.0")]
    v300 = 300,

    /// <summary>
    /// FinTS 4.0. Currently not supported by this library and not implemented by most banks.
    /// </summary>
    [Display(Name = "FinTS 4.0")]
    v400 = 400,

    /// <summary>
    /// FinTS 4.1. Currently not supported by this library and not implemented by most banks.
    /// </summary>
    [Display(Name = "FinTS 4.1")]
    v410 = 410

    // ReSharper restore UnusedMember.Global
    // ReSharper restore InconsistentNaming
}
