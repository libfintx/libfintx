/*
 *
 *  This file is part of libfintx.
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
using System.Text;

namespace libfintx.Logger
{
    internal static class MessageMasking
    {
        /// <summary>
        /// Masks a message for the trace log: the PIN/TAN element of HNSHA, found by the segment
        /// structure so that escaped characters cannot bypass it, and every occurrence of the user id.
        /// </summary>
        internal static string MaskUserTan(string message, string userId)
        {
            var sb = new StringBuilder(message.Length);
            int pos = 0;
            int start;
            while ((start = message.IndexOf("HNSHA:", pos, StringComparison.Ordinal)) >= 0)
            {
                // HNSHA:n:v+reference+certificate+PIN[:TAN]' -> mask everything after the third '+'
                int i = start;
                int plus = 0;
                while (i < message.Length && plus < 3)
                {
                    if (message[i] == '?') i++;
                    else if (message[i] == '+') plus++;
                    i++;
                }
                int end = i;
                while (end < message.Length && message[end] != '\'')
                {
                    if (message[end] == '?') end++;
                    end++;
                }
                end = Math.Min(end, message.Length);
                sb.Append(message, pos, i - pos);
                if (plus == 3)
                    sb.Append("***");
                else
                    sb.Append(message, i, end - i);
                pos = end;
            }
            sb.Append(message, pos, message.Length - pos);
            message = sb.ToString();

            // string.Replace(string, string) compares ordinally
            if (!string.IsNullOrEmpty(userId))
                message = message.Replace(userId, new string('X', userId.Length));
            return message;
        }
    }
}
