﻿/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2016 - 2022 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
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

// ReSharper disable once CheckNamespace
namespace libfintx.Logger.Debug
{
    [Obsolete("Create a logger class with library Microsoft.Extensions.Logging and use provider libfintx.Logger.FileLoggerProvider.CreateLibfintxLogger()", true)]
    public class DEBUG
    {
        public static bool Enabled { get; set; }

        /// <summary>
        /// Debug
        /// </summary>
        /// <param name="message"></param>
        public static void Write(string message)
        {
            if (Enabled)
            {
                Console.WriteLine("[" + DateTime.Now + "]" + " " + message + Environment.NewLine);
            }
        }
    }
}
