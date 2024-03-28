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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace libfintx.EBICS.Swift
{
    public static class MT942
    {
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
        public static IEnumerable<Tuple<F61,F86>> Tupleize(this Record rec)
        {
            if (rec.L61.Count != rec.L86.Count)
                throw new Exception("Bad MT942");
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
        public class F34:FixedStr
        {
            public string currency { get; set; }
            public string sign { get; set; }
            public string limit { get; set; }
            public F34(string operand):base(operand)
            {
                currency = TakeM(3);
                sign = TakeM(1);
                limit = this.operand;
            }
        }
        public class F13 : FixedStr
        {
            public string date { get; set; }
            public string time { get; set; }
            public string sign { get; set; }
            public string offset { get; set; }
            public F13(string operand) : base(operand)
            {
                date = TakeM(6);
                time = TakeM(4);
                sign = TakeM(1);
                offset=this.operand;
            }
        }
        public class F90 : FixedStr
        {
            public string number;
            public string currency;
            public string amount;
            public F90(string operand):base(operand)
            {
                number = TakeM(5);
                currency = TakeM(3);
                amount = this.operand;
            }
        }

        public class Record
        {
            public string TRN { get; set; }
            public string Account { get; set; }
            public string f28_StatementNumber { get; set; }
            public string f28_SequenceNumber { get; set; }
            public F13 f13 { get; set; }
            public F90 f90C { get; set; }
            public F90 f90D { get; set; }
            public List<F34> L34 { get; set; } = new List<F34>();
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
                    //case "21":
                    //    RELATEDREF = operand; break;
                    case "25":
                        Account = operand; break;
                    case "28C":
                        var f28 = r28c.Match(operand);
                        f28_StatementNumber = f28.Groups[1].Value;
                        f28_SequenceNumber = f28.Groups[2].Value;
                        break;
                    //case "60F":
                    //case "60M":
                    //    f60 = new F60(operand);
                    //    break;
                    case "61":
                        L61.Add(new F61(operand));
                        break;
                    case "86":
                        L86.Add(new F86(operand));
                        break;
                    case "34F":
                        L34.Add(new F34(operand));
                        break;
                    case "13D":
                        f13 = new F13(operand);
                        break;
                    case "90D":
                        f90D=new F90(operand);
                        break;
                    case "90C":
                        f90C =new F90(operand);
                        break;

                    //case "62F":
                    //case "62M":
                    //    L62 = fno + ":" + operand;
                    //    break;
                    //case "34F":
                    //case "13D":
                    //case "90D":
                    //case "90C":
                    //    break;
                    default:
                        throw new NotImplementedException();
                }
                return true;
            }
        }
    }
}
