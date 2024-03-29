﻿/*	
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
*/

using System;
using Microsoft.Extensions.Logging;
using libfintx.EBICS.Responses;

namespace libfintx.EBICS.Commands
{
    internal abstract class GenericCommand<T> : Command where T : Response
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<GenericCommand<T>>();

        protected T _response;

        internal T Response
        {
            get
            {
                if (_response == null)
                {
                    _response = Activator.CreateInstance<T>();
                }

                return _response;
            }
            set => _response = value;
        }

        internal override DeserializeResponse Deserialize(string payload)
        {
            using (new MethodLogger(s_logger))
            {
                var dr = Deserialize(payload);
                UpdateResponse(Response, dr);
                return dr;
            }
        }
    }
}
