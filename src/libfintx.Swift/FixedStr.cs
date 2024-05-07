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

namespace libfintx.Swift;

public class FixedStr
{
    protected string operand;
    protected FixedStr(string op) { this.operand = op; }
    protected string Take(int i)
    {
        if (operand.Length <= i)
        {
            string ret = operand;
            operand = string.Empty;
            return ret;
        }
        else
        {
            string ret = operand.Substring(0, i);
            operand = operand.Substring(i);
            return ret;
        }
    }
    protected string TakeM(int i)
    {
        if (operand.Length < i)
            throw new Exception("Mandatory Field Missing");
        if (operand.Length == i)
        {
            string ret = operand;
            operand = string.Empty;
            return ret;
        }
        else
        {
            string ret = operand.Substring(0, i);
            operand = operand.Substring(i);
            return ret;
        }
    }
}
