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
using System.Linq;

namespace libfintx.FinTS
{
    /// <summary>
    /// Builds the parts of the HKDSE/HKDME segments that depend on the bank: the pain
    /// descriptor, the segment version and the binary data element carrying the pain message.
    /// </summary>
    internal static class DirectDebitSegment
    {
        /// <summary>
        /// The descriptor German banks expect since November 2025. <c>pain.008.002.02</c> is
        /// accepted only until 14 November 2026.
        /// </summary>
        internal const string CurrentDescriptor = "pain.008.001.08";

        private const string UrnPrefix = "urn:iso:std:iso:20022:tech:xsd:";

        /// <summary>
        /// The descriptor for the segment: the one requested by the caller, else the one the
        /// bank announced in HISPAS, else <see cref="CurrentDescriptor"/>.
        /// </summary>
        internal static string ResolveDescriptor(FinTsClient client, string requested)
        {
            if (!string.IsNullOrWhiteSpace(requested))
                return requested;

            if (client != null && !string.IsNullOrWhiteSpace(client.HISPAS_PainDirectDebit))
                return client.HISPAS_PainDirectDebit;

            return CurrentDescriptor;
        }

        /// <summary>The descriptor as an escaped data element, urn prefix included.</summary>
        internal static string EscapedDescriptor(string descriptor)
        {
            var value = string.IsNullOrWhiteSpace(descriptor) ? CurrentDescriptor : descriptor;

            if (value.IndexOf("urn:", StringComparison.Ordinal) < 0)
                value = UrnPrefix + value;

            return Helper.EscapeHbciString(value);
        }

        /// <summary>
        /// The highest version of the given segment the bank announced in the BPD, at most
        /// <paramref name="highest"/>; <paramref name="fallback"/> if the BPD has no entry.
        /// HKDME version 2 carries the total amount as its own data element, version 1 does not.
        /// </summary>
        internal static int SegmentVersion(FinTsClient client, string name, int fallback, int highest)
        {
            var bpd = client == null ? null : client.BPD;

            if (bpd == null || bpd.SegmentList == null)
                return fallback;

            var announced = bpd.SegmentList
                .Where(s => s != null && s.Name == name && s.Version <= highest)
                .Select(s => s.Version)
                .DefaultIfEmpty(0)
                .Max();

            return announced > 0 ? announced : fallback;
        }

        /// <summary>
        /// Replaces the trailing <c>@@</c> of a segment with the pain message as binary data
        /// and terminates the segment.
        /// </summary>
        /// <remarks>
        /// The existing HKDSE/HKDME methods use <c>"@" + (message.Length - 1) + "@" + message</c>
        /// because <c>pain00800202.Create</c> ends the message with the segment terminator. A
        /// plain XML document does not, so the length is taken in full here and the terminator
        /// is appended. A message that already ends with <c>'</c> is accepted as well.
        /// </remarks>
        internal static string AttachPayload(string segment, string painXml)
        {
            if (segment == null || !segment.EndsWith("@@", StringComparison.Ordinal))
                throw new ArgumentException("The segment must end with the placeholder @@.", nameof(segment));

            if (string.IsNullOrWhiteSpace(painXml))
                throw new ArgumentException("A collection needs a pain message.", nameof(painXml));

            var payload = painXml.EndsWith("'", StringComparison.Ordinal)
                ? painXml.Substring(0, painXml.Length - 1)
                : painXml;

            return segment.Substring(0, segment.Length - 2) + "@" + payload.Length + "@" + payload + "'";
        }
    }
}
