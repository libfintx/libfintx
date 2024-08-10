/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2024 Torsten Klement
*  
*  This program is free software; you can redistribute it and/or
*  modify it under the terms of the GNU Lesser General Public
*  License as published by the Free Software Foundation; either
*  version 3 of the License, or (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
*  Lesser General Public License for more details.
*
*  You should have received a copy of the GNU Lesser General Public License
*  along with this program; if not, write to the Free Software Foundation,
*  Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
* 	
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace libfintx.Swift;

/// <summary>
/// MT940 is a specific SWIFT message type used by the SWIFT network to send and receive end-of-day bank account statements.
///
/// This class holds functions to process MT940 messages.
/// </summary>
public static class MT940
{
    /// <summary>
    /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
    /// </summary>
    /// <param name="stream">
    /// A stream providing the raw text of the MT940 statement.
    /// </param>
    /// <param name="pending">
    /// If the Swift statements shall be marked as pending.
    /// </param>
    /// <param name="loggerFactory">
    /// A logging factory to create the logging where to write warnings.
    /// </param>
    /// <returns>
    /// A enumerable object returning a list of swift statements from the MT940 statement.
    /// </returns>
    public static IEnumerable<SwiftStatement> Deserialize(Stream stream, bool pending = false, ILoggerFactory? loggerFactory = null)
    {
        var mt940Parser = new MT940Parser(pending, loggerFactory);
        return mt940Parser.Deserialize(stream);
    }

    /// <summary>
    /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
    /// </summary>
    /// <param name="sta">
    /// The raw text of the MT940 statement.
    /// </param>
    /// <param name="pending">
    /// If the Swift statements shall be marked as pending.
    /// </param>
    /// <param name="loggerFactory">
    /// A logging factory to create the logging where to write warnings.
    /// </param>
    /// <returns>
    /// A enumerable object returning a list of swift statements from the MT940 statement.
    /// </returns>
    public static IEnumerable<SwiftStatement> Deserialize(string sta, bool pending = false, ILoggerFactory? loggerFactory = null)
    {
        return Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(sta)), pending, loggerFactory);
    }

    public static List<Record> Parse(string source)
    {
        var ret = new List<Record>();
        Record rec = new Record();
        foreach (var line in LineJoiner(source.Replace("\r\n","\r").Split('\r')))
        {
            if (!rec.loadline(line))
            {
                if (rec.mandatory())
                    ret.Add(rec);
                rec = new Record();
            }
        };
        if (rec.mandatory())
            ret.Add(rec);
        return ret;
    }
    private static IEnumerable<Tuple<F61,F86>> Tupleize(this Record rec)
    {
        if (rec.L61.Count != rec.L86.Count)
            throw new Exception("Bad MT940");
        for (int i=0;i<rec.L61.Count;i++)
        {
            yield return new Tuple<F61, F86>(rec.L61[i], rec.L86[i]);
        }
    }
    public static IEnumerable<string> LineJoiner(IEnumerable<string> source)
    {
        string prevLine = null;
        foreach (var line in source)
        {
            if (prevLine == null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                if (!line.StartsWith(':'))
                    continue;
                prevLine = line;
                continue;
            }
            if (string.IsNullOrEmpty(line) || line.StartsWith(':'))
            {
                yield return prevLine;
                prevLine = line;
            }
            else
                prevLine = prevLine + line;
        }
        if (prevLine != null)
            yield return prevLine;
    }
    public class F60
    {
        static Regex r60c = new Regex("^([CD])([0-9]{6})([A-Z]{3})(.*)$", RegexOptions.Compiled);
        public string sign { get; set; }
        public string date { get; set; }
        public string currency { get; set; }
        public string balance { get; set; }
        public F60(string operand)
        {
            var f60 = r60c.Match(operand);
            sign = f60.Groups[1].Value;
            date = f60.Groups[2].Value;
            currency = f60.Groups[3].Value;
            balance = f60.Groups[4].Value;
        }
    }
    public class Record
    {
        public string TRN { get; set; }
        public string RELATEDREF { get; set; }
        public string Account { get; set; }
        public string f28_StatementNumber { get; set; }
        public string f28_SequenceNumber { get; set; }
        public F60 f60 { get; set; }
        public string L62 { get; set; }
        public List<F61> L61 { get; set; } = new List<F61>();
        public List<F86> L86 { get; set; } = new List<F86>();

        static Regex r28c = new Regex("^([^/]*)/(.*)$", RegexOptions.Compiled);

        public bool mandatory()
        {
            if (!string.IsNullOrEmpty(TRN)
                && !string.IsNullOrEmpty(Account))
                return true;
            return false;
        }
        public bool loadline(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;
            if (!line.StartsWith(':'))
                throw new InvalidOperationException("Bad MT940 Format");
            var spl = line.Split(':', 3);
            string fno = spl[1].ToUpper();
            string operand = spl[2];
            switch (fno)
            {
                case "20":
                    TRN = operand; break;
                case "21":
                    RELATEDREF = operand; break;
                case "25":
                    Account = operand; break;
                case "28C":
                    var f28 = r28c.Match(operand);
                    f28_StatementNumber = f28.Groups[1].Value;
                    f28_SequenceNumber = f28.Groups[2].Value;
                    break;
                case "60F":
                case "60M":
                    f60 = new F60(operand);
                    break;
                case "61":
                    L61.Add(new F61(operand));
                    break;
                case "86":
                    L86.Add(new F86(operand));
                    break;
                case "62F":
                case "62M":
                    L62 = fno + ":" + operand;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return true;
        }
    }
}
