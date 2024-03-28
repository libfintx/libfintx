/*	
* 	
*  This file is part of libfintx.
*  
*  Copyright (C) 2018 Bjoern Kuensting
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
*  Updates done by Torsten Klement <torsten.klinger@googlemail.com>
*  
*  Updates Copyright (c) 2024 Torsten Klement
* 	
*/

using StatePrinting;
using StatePrinting.OutputFormatters;

namespace libfintx.EBICS.Responses
{
    public abstract class Response
    {
        static readonly Stateprinter _printer;

        public int TechnicalReturnCode { get; set; }
        public int BusinessReturnCode { get; set; }
        public string ReportText { get; set; }

        static Response()
        {
            _printer = new Stateprinter();
            _printer.Configuration.SetIndentIncrement(" ");
            _printer.Configuration.SetOutputFormatter(new JsonStyle(_printer.Configuration));
        }

        public override string ToString() => _printer.PrintObject(this);
    }

    interface IResponse
    {
        int TechnicalReturnCode { get; }
        int BusinessReturnCode { get; }
        string ReportText { get; }

        string ResponseData { get; }
    }

    interface IResonse<T> : IResponse
    {
        T Response { get; }
    }
}
