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

namespace libfintx.EBICS.Swift
{
    public class F86
    {
        public string TRXCODE { get; private set; }
        public string journal_no { get; private set; }
        public string posting { get; private set; }
        public Dictionary<string, string> RemInfo { get; private set; } = new Dictionary<string, string>();
        public string BIC { get; private set; }
        public string IBAN { get; private set; }
        public string Payer_Name { get; private set; }
        public string SepaCode { get; private set; }
        public F86() { throw new NotImplementedException(); }
        public F86(string data)
        {
            TRXCODE = data.Substring(0, 3);
            data = data.Substring(3);
            var lines = data.Split('?');
            string lastreminfo=string.Empty;
            foreach (var line in lines)
            {
                if (line.Length == 0)
                    continue;
                var fno = int.Parse(line.Substring(0, 2));
                var operand = line.Substring(2);
                switch (fno)
                {
                    case 0:
                        posting = operand; break;
                    case 10:
                        journal_no = operand; break;
                    case int n1 when (n1 >= 20 && n1 <= 29):
                    case int n2 when (n2 >= 60 && n2 <= 63):
                        var o = operand.Split('+', 2);
                        if (o.Length == 2)
                        {
                            lastreminfo = o[0];
                            if (RemInfo.ContainsKey(o[0]))
                                RemInfo[o[0]] += o[1];
                            else
                                RemInfo.Add(o[0], o[1]);
                        }
                        else if (!string.IsNullOrEmpty(lastreminfo))
                            RemInfo[lastreminfo] += operand;
                        else
                            RemInfo.Add(fno.ToString(), operand);
                        break;
                    case 30:
                        BIC = operand; break;
                    case 31:
                        IBAN = operand; break;
                    case 32:
                    case 33:
                        Payer_Name += operand; break;
                    case 34:
                        SepaCode = operand; break;
                    default:
                        throw new Exception("Unknown F86 subfield");
                }
            }
        }
    }
}
