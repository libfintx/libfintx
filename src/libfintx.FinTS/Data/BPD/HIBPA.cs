using System.ComponentModel.DataAnnotations;
using libfintx.FinTS.Data.Segment;

namespace libfintx.FinTS.Segments;

public class HIBPA : SegmentBase
{
    public HIBPA(Segment segment) : base(segment)
    {
    }

    /// <summary>
    /// BPD-Version
    /// </summary>
    public int BpdVersion { get; set; }

    /// <summary>
    /// Kreditinstitutkennzeichnung - Land
    /// </summary>
    public int BankCountry { get; set; }

    /// <summary>
    /// Kreditinstitutkennzeichnung - Banknr.
    /// </summary>
    public int BankCode { get; set; }

    /// <summary>
    /// Kreditinstitutsbezeichnung
    /// </summary>
    [StringLength(60)]
    public string BankName { get; set; }
}
