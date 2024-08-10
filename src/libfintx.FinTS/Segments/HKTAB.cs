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
using System.Threading.Tasks;
using libfintx.FinTS.Message;
using libfintx.FinTS.Segments;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKTAB
    {
        /// <summary>
        /// TAN Medium Art für Elementversion #1
        ///
        /// dient der Klassifizierung der gesamten dem Kunden zugeordneten TANMedien. Bei Geschäftsvorfällen zum Management des TAN-Generators kann
        /// aus diesen nach folgender Codierung selektiert werden.
        /// 
        /// Siehe https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv3/FinTS_3.0_Security_Sicherheitsverfahren_PINTAN_2018-02-23_final_version.pdf
        /// Seite 128
        /// </summary>
        public enum TanMediumType1
        {
            All = 0, // Alle
            Active = 2, // Aktiv
            Available = 3 // Verfügbar
        }

        /// <summary>
        /// TAN Medium Art für Elementversion #2
        ///
        /// dient der Klassifizierung der gesamten dem Kunden zugeordneten TANMedien. Bei Geschäftsvorfällen zum Management des TAN-Generators kann
        /// aus diesen nach folgender Codierung selektiert werden.
        /// 
        /// Siehe https://www.hbci-zka.de/dokumente/spezifikation_deutsch/fintsv3/FinTS_3.0_Security_Sicherheitsverfahren_PINTAN_2018-02-23_final_version.pdf
        /// Seite 128
        /// </summary>
        public enum TanMediumType2
        {
            All = 0, // Alle
            Active = 1, // Aktiv
            Available = 2 // Verfügbar
        }

        /// <summary>
        /// TAN-Medium-Klasse, Elementversion #1
        ///
        /// dient der Klassifizierung der möglichen TAN-Medien. Bei Geschäftsvorfällen
        /// zum Management der TAN-Medien kann aus diesen nach folgender Codierung selektiert werden.
        /// </summary>
        public static class TanMediumClass1
        {
            /// <summary>
            /// Liste
            /// </summary>
            public const string List = "L";

            /// <summary>
            /// TAN-Generator
            /// </summary>
            public const string TanGenerator = "G";

            /// <summary>
            /// Mobiltelefon mit mobileTAN
            /// </summary>
            public const string MobileTan = "M";
        }

        /// <summary>
        /// TAN-Medium-Klasse, Elementversion #2
        ///
        /// dient der Klassifizierung der möglichen TAN-Medien. Bei Geschäftsvorfällen
        /// zum Management der TAN-Medien kann aus diesen nach folgender Codierung selektiert werden.
        /// </summary>
        public static class TanMediumClass2
        {
            /// <summary>
            /// Liste
            /// </summary>
            public const string List = "L";

            /// <summary>
            /// TAN-Generator
            /// </summary>
            public const string TanGenerator = "G";

            /// <summary>
            /// Mobiltelefon mit mobileTAN
            /// </summary>
            public const string MobileTan = "M";

            /// <summary>
            /// Secoder
            /// </summary>
            public const string Secoder = "S";
        }

        /// <summary>
        /// TAN-Medium-Klasse, Elementversion #3
        ///
        /// dient der Klassifizierung der möglichen TAN-Medien. Bei Geschäftsvorfällen
        /// zum Management der TAN-Medien kann aus diesen nach folgender Codierung selektiert werden.
        /// </summary>
        public static class TanMediumClass3
        {
            /// <summary>
            /// Alle Medien
            /// </summary>
            public const string All = "A";

            /// <summary>
            /// Liste
            /// </summary>
            public const string List = "L";

            /// <summary>
            /// TAN-Generator
            /// </summary>
            public const string TanGenerator = "G";

            /// <summary>
            /// Mobiltelefon mit mobileTAN
            /// </summary>
            public const string MobileTan = "M";

            /// <summary>
            /// Secoder
            /// </summary>
            public const string Secoder = "S";
        }

        /// <summary>
        /// TAN-Medium-Klasse, Elementversion #4
        ///
        /// dient der Klassifizierung der möglichen TAN-Medien. Bei Geschäftsvorfällen
        /// zum Management der TAN-Medien kann aus diesen nach folgender Codierung selektiert werden.
        /// </summary>
        public static class TanMediumClass4
        {
            /// <summary>
            /// Alle Medien
            /// </summary>
            public const string All = "A";

            /// <summary>
            /// Liste
            /// </summary>
            public const string List = "L";

            /// <summary>
            /// TAN-Generator
            /// </summary>
            public const string TanGenerator = "G";

            /// <summary>
            /// Mobiltelefon mit mobileTAN
            /// </summary>
            public const string MobileTan = "M";

            /// <summary>
            /// Secoder
            /// </summary>
            public const string Secoder = "S";

            /// <summary>
            /// Bilateral vereinbart
            /// </summary>
            public const string BilateralAgreement = "B";
        }

        /// <summary>
        /// Request TAN medium name
        /// </summary>
        public static async Task<string> Init_HKTAB(FinTsClient client)
        {
            client.Logger.LogInformation("Starting job HKTAB: Request tan medium name");

            var seg = new SEG();

            // HKTAB version:
            //   - version 4: use TanMediumType2, TanMediumClass3
            //   - version 5: use TanMediumType2, TanMediumClass4

            var tanMediumType = TanMediumType2.All;
            var tanMediumClass = TanMediumClass3.All;

            var segments = seg.toSEG(new SEG_DATA
            {
                Header = "HKTAB",
                Num = Convert.ToInt16(SEG_NUM.Seg3),
                Version = 4,
                RefNum = 0,
                RawData = $"{(int)tanMediumType}+{tanMediumClass}{seg.Terminator}"
            });
            //segments = "HKTAB:" + SEG_NUM.Seg3 + ":4+0+A'";

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            return await FinTSMessage.Send(client, message);
        }
    }
}
