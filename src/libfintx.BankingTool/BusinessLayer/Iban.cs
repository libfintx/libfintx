using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libfintx.BankingTool.BusinessLayer;

/// <summary>
/// A class holding a IBAN number.
/// </summary>
public struct Iban : IConvertible, IEquatable<Iban>, IFormattable
{
    private string? _iban;
    private string? _countryCode;
    private int? _checkDigit;

    private string? _bankIdent;
    private string? _accountNumber;

    public Iban()
    {
        _iban = null;
        _countryCode = null;
        _checkDigit = null;
        _bankIdent = null;
        _accountNumber = null;
    }

    public Iban(string? iban)
    {
        _iban = CleanIban(iban);
        _countryCode = null;
        _checkDigit = null;
        _bankIdent = null;
        _accountNumber = null;
    }

    /// <summary>
    /// The country code of the IBAN.
    /// </summary>
    public string? CountryCode
    {
        get
        {
            if (_countryCode != null)
                return _countryCode;

            if (_iban != null)
            {
                _countryCode = DiscoverCountryCode();
            }

            return _countryCode;
        }
        set
        {
            if (value == CountryCode)
                return;

            if (value != null && value.Length != 2)
                throw new ArgumentException($"You can only set {nameof(CountryCode)} to a 2 alpha numeric country code or null.", nameof(value));

            _countryCode = value?.ToUpper();

            _iban = null;
            _checkDigit = null;
        }
    }

    public int? CheckDigit
    {
        get
        {
            if (_checkDigit != null)
                return _checkDigit;

            if (_iban != null)
            {
                _checkDigit = DiscoverCheckDigit();
            }

            return _checkDigit;
        }
        set => _checkDigit = value;
    }

    public string? BankIdent
    {
        get
        {
            if (_bankIdent != null)
                return _bankIdent;

            if (_iban != null)
            {
                _bankIdent = DiscoverBankIdent();
            }

            return _bankIdent;
        }
        set
        {
            if (_bankIdent == value)
                return;

            _bankIdent = value;

            _iban = null;
            _checkDigit = null;
        }
    }

    public string? AccountNumber
    {
        get
        {
            if (_accountNumber != null)
                return _accountNumber;

            if (_iban != null)
            {
                _accountNumber = DiscoverAccountNumber();
            }

            return _accountNumber;
        }
        set
        {
            if (_accountNumber == value)
                return;

            _accountNumber = value;

            _iban = null;
            _checkDigit = null;
        }
    }

    // ReSharper disable once InconsistentNaming
    public string? BBAN
    {
        get
        {
            if (_iban != null && _iban.Length > 4)
                return _iban.Substring(4);

            return null;
        }
    }

    // ReSharper disable once InconsistentNaming
    public string? IBAN
    {
        get
        {
            return _iban ??= BuildIbanInternal(false);
        }
        set
        {
            if (_iban == value)
                return;

            _iban = CleanIban(value);
            _countryCode = null;
            _checkDigit = null;
            _bankIdent = null;
            _accountNumber = null;
        }
    }

    /// <summary>
    /// Creates a IBAN which is machine friendly:
    /// 
    /// - trim all whitespace chars at the beginning and end of the IBAN
    /// - remove all spaces in the IBAN
    /// - transform all chars to uppercase
    /// </summary>
    /// <example>
    /// <c>"DE07 1234 1234 1234 1234 12"</c> is converted to <c>"DE07123412341234123412"</c>
    /// </example>
    /// <returns></returns>
    public static string? CleanIban(string? iban)
    {
        return iban?.Trim().Replace(" ", "").ToUpper();
    }

    /// <summary>
    /// Reads the Country code from the IBAN.
    /// </summary>
    /// <returns></returns>
    private string? DiscoverCountryCode()
    {
        if (_iban == null || _iban.Length < 2)
            return null;

        return _iban.Substring(0, 2);
    }

    /// <summary>
    /// Reads the Check Digits from the IBAN.
    /// </summary>
    /// <returns></returns>
    private int? DiscoverCheckDigit()
    {
        if (_iban == null) return null;
        int.TryParse(_iban.Substring(2, 2), out var checkDigit);
        return checkDigit;

    }

    /// <summary>
    /// Reads the Bank Ident from the IBAN.
    /// </summary>
    /// <returns></returns>
    private string? DiscoverBankIdent()
    {
        if (CountryCode == null)
            return null;

        var countryInfo = IbanCountryInfo.GetDefault(CountryCode);

        if (_iban == null || _iban.Length < 4 + countryInfo.BankIdentLength)
            return null;

        return _iban.Substring(4, countryInfo.BankIdentLength);
    }

    /// <summary>
    /// Reads the Account Number from the IBAN.
    /// </summary>
    /// <returns></returns>
    private string? DiscoverAccountNumber()
    {
        if (CountryCode == null)
            return null;

        var countryInfo = IbanCountryInfo.GetDefault(CountryCode);

        if (_iban == null || _iban.Length < 4 + countryInfo.BankIdentLength + countryInfo.AccountNumberLength)
            return null;

        return _iban.Substring(4 + countryInfo.BankIdentLength, countryInfo.AccountNumberLength);
    }

    /// <summary>
    /// Checks if the IBAN number is valid.
    /// </summary>
    /// <returns>
    /// Is <c>true</c> if the IBAN is valid, otherwise <c>false</c>.
    /// </returns>
    public bool Validate()
    {
        if (_iban == null)
        {
            _iban = BuildIbanInternal(false);

            if (_iban == null)
                return false;
        }
        else if (CheckDigit == null)
        {
            // the check digit cannot be converted to integer --> throw error
            return false;
        }

        if (CountryCode == null)
            return false;

        IbanCountryInfo ibanCountryInfo;
        try
        {
            ibanCountryInfo = IbanCountryInfo.GetDefault(CountryCode);
        }
        catch (NotSupportedException)
        {
            return false;
        }

        if (ibanCountryInfo.RegularExpression != null)
        {
            return ibanCountryInfo.RegularExpression.IsMatch(_iban);
        }

        return _iban.Length == 4 + ibanCountryInfo.BankIdentLength + ibanCountryInfo.AccountNumberLength;
    }

    private string? BuildBban(IbanCountryInfo countryInfo, bool raiseError = true)
    {
        if (string.IsNullOrEmpty(_bankIdent))
        {
            if (raiseError)
                throw new InvalidOperationException("IBAN BankIdent not set, could not generate IBAN");
            return null;
        }

        if (_bankIdent.Length > countryInfo.BankIdentLength)
        {
            if (raiseError)
                throw new InvalidOperationException($"IBAN BankIdent is too long for country {_countryCode}. It must be {countryInfo.BankIdentLength} chars.");
            return null;
        }
        if (_bankIdent.Length < countryInfo.BankIdentLength)
        {
            if (raiseError)
                throw new InvalidOperationException($"IBAN BankIdent is too short for country {_countryCode}. It must be {countryInfo.BankIdentLength} chars.");
            return null;
        }

        if (string.IsNullOrEmpty(_accountNumber))
        {
            if (raiseError)
                throw new InvalidOperationException("IBAN AccountNumber not set, could not generate IBAN");
            return null;
        }

        if (_accountNumber.Length > countryInfo.AccountNumberLength)
        {
            if (raiseError)
                throw new InvalidOperationException($"IBAN BankIdent is too long for country {_countryCode}. It must be {countryInfo.AccountNumberLength} chars.");
            return null;
        }
        if (_accountNumber.Length < countryInfo.AccountNumberLength)
        {
            _accountNumber = _accountNumber.PadLeft(countryInfo.AccountNumberLength, '0');
        }

        return $"{_bankIdent}{_accountNumber}";
    }

    /// <summary>
    /// Builds the iban from the given fields.
    /// </summary>
    /// <returns>
    /// Returns a valid iban number.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Is thrown when the building of the IBAN failed because of wrong data.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Is thrown when there exist no known IBAN specification for the given country. 
    /// </exception>
    public string BuildIban()
    {
#pragma warning disable CS8603
        return BuildIbanInternal();
#pragma warning restore CS8603
    }

    private string? BuildIbanInternal(bool raiseError = true)
    {
        Debug.Assert(_iban == null);

        if (_countryCode == null)
        {
            if (raiseError)
                throw new InvalidOperationException("IBAN Country code not set, could not generate IBAN");
            return null;
        }

        IbanCountryInfo countryInfo;
        try
        {
            countryInfo = IbanCountryInfo.GetDefault(_countryCode);
        }
        catch (NotSupportedException)
        {
            if (raiseError)
                throw;
            return null;
        }

        var bban = BuildBban(countryInfo, raiseError);
        if (bban == null)
            return null;

        if (_checkDigit == null)
        {
            _checkDigit = 98 - Modulo97(bban, _countryCode);
        }

        return $"{CountryCode}{CheckDigit:00}{_bankIdent}{_accountNumber}";
    }

    /// <summary>
    /// Converts bban with letters into bban of numbers.
    /// </summary>
    /// <param name="bban">The given bban.</param>
    /// <returns>The converted bban.</returns>
    private static string BbanToNumber(string bban)
    {
        if (long.TryParse(bban, out _))
            return bban;

        string result = string.Empty;

        for (int i = 0; i < bban.Length; i++)
        {
            if (long.TryParse(bban[i].ToString(), out _))
                result += bban[i];
            else
                result += bban[i] - 55;
        }

        return result;
    }

    /// <summary>
    /// Converts a country code into a string of numbers.
    /// </summary>
    /// <param name="countryCode">The given country code.</param>
    /// <returns>The converted country code.</returns>
    private static string CountryCodeToNumber(string countryCode)
    {
        string result = string.Empty;

        for (int i = 0; i < countryCode.Length; i++)
        {
            result += countryCode[i] - 55;
        }

        return result;
    }

    /// <summary>
    /// Calculates the modulo of 97 for a iban.
    /// </summary>
    /// <returns>The calculated result.</returns>
    private static int Modulo97(string bban, string countryCode)
    {
        string input = BbanToNumber(bban) + CountryCodeToNumber(countryCode) + "00";
        string output = string.Empty;

        for (int i = 0; i < input.Length; i++)
        {
            if (output.Length < 9)
                output += input[i];
            else
            {
                output = (int.Parse(output) % 97).ToString() + input[i];
            }
        }

        return int.Parse(output) % 97;
    }

    public override string? ToString()
    {
        return IBAN;
    }

    public string ToString(string? format)
    {
        return ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the value of the current Iban object to its equivalent string representation.
    /// </summary>
    /// <param name="format">
    /// Available format options:
    ///
    /// M - a cleaned IBAN number in machine readable format, for example <c>"IE12BOFI90000112345678"</c>
    /// H - a nice formatted IBAN in a human readable format, splitting the number into four char segments, for example <c>IE12 BOFI 9000 0112 3456 78</c>
    ///
    /// Default option is "M"
    /// </param>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (_iban == null)
            return string.Empty;

        // Set default format
        format ??= "M";

        if (format.StartsWith("H"))
        {
            // human readable format
            var sb = new StringBuilder();
            for (var index = 0; index < _iban.Length; index++)
            {
                var c = _iban[index];

                sb.Append(c);

                if ((index + 1) % 4 == 0)
                    sb.Append(" ");
            }

            return sb.ToString();
        }

        if (format.StartsWith("M"))
            // machine readable format
            return _iban ?? string.Empty;

        throw new FormatException($"Unknown format specifier \"{format}\" for Iban.");

    }

    #region IEquatable<Iban>

    public bool Equals(Iban other)
    {
        return _iban == other._iban;
    }

    #endregion

    public override bool Equals(object? obj)
    {
        return obj is Iban other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_iban);
    }

    #region IConvertible

    public TypeCode GetTypeCode()
    {
        return _iban == null ? TypeCode.DBNull : TypeCode.String;
    }

    bool IConvertible.ToBoolean(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    byte IConvertible.ToByte(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    char IConvertible.ToChar(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    DateTime IConvertible.ToDateTime(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    decimal IConvertible.ToDecimal(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    double IConvertible.ToDouble(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    short IConvertible.ToInt16(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    int IConvertible.ToInt32(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    long IConvertible.ToInt64(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    sbyte IConvertible.ToSByte(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    float IConvertible.ToSingle(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    string IConvertible.ToString(IFormatProvider? provider)
    {
        return _iban ?? string.Empty;
    }

    public object ToType(Type conversionType, IFormatProvider? provider)
    {
        if (conversionType == typeof(object))
            // TODO this is a hack to make Json serializion work quick and dirty. There should be a more proper implementation here (!)
            conversionType = typeof(string);

        if (conversionType != typeof(string))
            throw new InvalidCastException();

        return ((IConvertible) this).ToString(provider);
    }

    ushort IConvertible.ToUInt16(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    uint IConvertible.ToUInt32(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    ulong IConvertible.ToUInt64(IFormatProvider? provider)
    {
        throw new InvalidCastException();
    }

    #endregion

    public static bool operator ==(Iban iban1, Iban iban2)
    {
        if (iban1._iban == null)
        {
            return iban2._iban == null;
        }

        if (iban2._iban == null)
        {
            return false;
        }

        return iban1.Equals(iban2);
    }

    public static bool operator ==(Iban iban, string ibanAsString)
    {
        if (iban._iban == null)
        {
            return string.IsNullOrEmpty(ibanAsString);
        }

        if (string.IsNullOrEmpty(ibanAsString))
            return false;

        return iban.Equals(new Iban(ibanAsString));
    }

    public static bool operator !=(Iban iban1, Iban iban2)
    {
        if (iban1._iban == null)
        {
            return iban2._iban != null;
        }

        if (iban2._iban == null)
        {
            return true;
        }

        return !iban1.Equals(iban2);
    }

    public static bool operator !=(Iban iban, string ibanAsString)
    {
        if (iban._iban == null)
        {
            return !string.IsNullOrEmpty(ibanAsString);
        }

        if (string.IsNullOrEmpty(ibanAsString))
            return true;

        return !iban.Equals(new Iban(ibanAsString));
    }

    public static explicit operator string?(Iban iban)
    {
        return iban.ToString();
    }
}
