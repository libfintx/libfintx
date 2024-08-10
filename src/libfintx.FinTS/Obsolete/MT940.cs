/*
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using libfintx.Globals;
using libfintx.Swift;

// ReSharper disable once CheckNamespace
namespace libfintx.FinTS.Statement;

// Remove in next MAJOR VERSION UPGRADE
/// <summary>
/// MT940 account statement parser
/// </summary>
// ReSharper disable once InconsistentNaming
public static class MT940
{
    /// <summary>
    /// Serializes a MT940 statement into a <see cref="SwiftStatement"/> class object.
    /// </summary>
    /// <param name="STA">
    /// The raw text of the MT940 statement.
    /// </param>
    /// <param name="account">
    /// The account name. Only required when <paramref name="writeToFile"/> is <c>true</c>.
    /// </param>
    /// <param name="writeToFile">
    /// If the MT940 statements shall be written to the disk.
    /// </param>
    /// <param name="pending"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// Is thrown when <paramref name="writeToFile"/> is <c>true</c> and <paramref name="account"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Please use MT940.Deserialize(...) instead. For parameter writeToFile, use MT940.WriteToFile(...).")]
    public static List<SwiftStatement> Serialize(string STA, string account = null, bool writeToFile = false, bool pending = false)
    {
        if (writeToFile && account == null)
        {
            throw new ArgumentNullException(nameof(account));
        }

        var mt940Parser = new MT940Parser(pending);

        if (string.IsNullOrEmpty(STA))
        {
            return new List<SwiftStatement>();
        }

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(STA));
        return mt940Parser.Deserialize(memStream).ToList();
    }

    /// <summary>
    /// Writes a MT940 statement into the library own folder for further use.
    /// </summary>
    /// <param name="sta">
    /// The raw text of the MT940 statement.
    /// </param>
    /// <param name="account">
    /// The account name.
    /// </param>
    [Obsolete("Moved internally into FinTsClient", true)]
    public static void WriteToFile(string sta, string account)
    {
        string dir = FinTsGlobals.ProgramBaseDir;

        dir = Path.Combine(dir, "STA");

        string filename = Path.Combine(dir, Helper.MakeFilenameValid(account + "_" + DateTime.Now + ".STA"));

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // STA
        if (!File.Exists(filename))
        {
            using (File.Create(filename))
            { };

            File.AppendAllText(filename, sta);
        }
        else
            File.AppendAllText(filename, sta);
    }

    /// <summary>
    /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
    /// </summary>
    /// <param name="sta">
    /// The raw text of the MT940 statement.
    /// </param>
    /// <param name="account">
    /// The account name. Only required when tracing is activated.
    /// </param>
    /// <param name="pending">
    /// If the Swift statements shall be marked as pending.
    /// </param>
    /// <returns>
    /// A enumerable object returning a list of swift statements from the MT940 statement.
    /// </returns>
    [Obsolete("Please use MT940.Deserialize(...) instead")]
    public static IEnumerable<SwiftStatement> Deserialize(string sta, string account = null, bool pending = false)
    {
        var mt940Parser = new MT940Parser(pending);
        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sta));
        return mt940Parser.Deserialize(memStream);
    }

    /// <summary>
    /// Deserializes a MT940 statement into a <see cref="SwiftStatement"/> class objects.
    /// </summary>
    /// <param name="stream">
    /// A stream providing the MT940 statement.
    /// </param>
    /// <param name="account">
    /// The account name. Only required when tracing is activated.
    /// </param>
    /// <param name="pending">
    /// If the Swift statements shall be marked as pending.
    /// </param>
    /// <returns>
    /// A enumerable object returning a list of swift statements from the MT940 statement.
    /// </returns>
    [Obsolete("Please use MT940.Deserialize(...) instead")]
    public static IEnumerable<SwiftStatement> Deserialize(Stream stream, string account = null, bool pending = false)
    {
        var mt940Parser = new MT940Parser(pending);
        return mt940Parser.Deserialize(stream);
    }
}
