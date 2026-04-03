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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using libfintx.FinTS.Segments;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static class HKKAZ
    {
        /// <summary>
        /// Transactions
        /// </summary>
        public static async Task<String> Init_HKKAZ(FinTsClient client, string FromDate, string ToDate, string Startpoint)
        {
            client.Logger.LogInformation("Starting job HKKAZ: Request transactions");

            SEG sEG = new SEG();
            string segments = string.Empty;
            var activeAccount = ResolveAccountForHkkaz(client);

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);

            if (string.IsNullOrEmpty(FromDate))
            {
                if (string.IsNullOrEmpty(Startpoint))
                {
                    if (client.HIKAZS < 7)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter + DEG.DeAdd);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA {
                            Header = "HKKAZ", Num = client.SEGNUM, Version = client.HIKAZS, RefNum = 0, RawData = rawData}
                        );
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N'";
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountIban);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBic);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA {
                            Header = "HKKAZ", Num = client.SEGNUM, Version = client.HIKAZS, RefNum = 0, RawData = rawData });

                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountIban + ":" + activeAccount.AccountBic + ":" + activeAccount.AccountNumber + "::" + SEG_COUNTRY.Germany + ":" + activeAccount.AccountBankCode + "+N'";
                    }
                }
                else
                {
                    if (client.HIKAZS < 7)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(Startpoint);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG( new SEG_DATA {
                            Header = "HKKAZ", Num = client.SEGNUM, Version = client.HIKAZS, RefNum = 0, RawData = rawData });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N++++" + Startpoint + "'";
                    }  
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountIban);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBic);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(Startpoint);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA { Header = "HKKAZ", Num = client.SEGNUM, Version = client.HIKAZS,
                            RefNum = 0, RawData = rawData });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountIban + ":" + activeAccount.AccountBic + ":" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N++++" + Startpoint + "'";
                    }   
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Startpoint))
                {
                    if (client.HIKAZS < 7)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(FromDate);
                        sb.Append(sEG.Delimiter);
                        sb.Append(ToDate);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA
                        {
                            Header = "HKKAZ",
                            Num = client.SEGNUM,
                            Version = client.HIKAZS,
                            RefNum = 0,
                            RawData = rawData
                        });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N+" + FromDate + "+" + ToDate + "'";
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountIban);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBic);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(FromDate);
                        sb.Append(sEG.Delimiter);
                        sb.Append(ToDate);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA
                        {
                            Header = "HKKAZ",
                            Num = client.SEGNUM,
                            Version = client.HIKAZS,
                            RefNum = 0,
                            RawData = rawData
                        });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountIban + ":" + activeAccount.AccountBic + ":" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N+" + FromDate + "+" + ToDate + "'";
                    }
                }
                else
                {
                    if (client.HIKAZS < 7)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(FromDate);
                        sb.Append(sEG.Delimiter);
                        sb.Append(ToDate);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(Startpoint);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA { Header = "HKKAZ", Num = client.SEGNUM,
                            Version = client.HIKAZS, RefNum = 0, RawData = rawData });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeAccount.AccountIban);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBic);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountNumber);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.SubAccountFeature);
                        sb.Append(DEG.Separator);
                        sb.Append(SEG_COUNTRY.Germany);
                        sb.Append(DEG.Separator);
                        sb.Append(activeAccount.AccountBankCode);
                        sb.Append(sEG.Delimiter);
                        sb.Append(DEG.DeAdd);
                        sb.Append(sEG.Delimiter);
                        sb.Append(FromDate );
                        sb.Append(sEG.Delimiter);
                        sb.Append(ToDate);
                        sb.Append(sEG.Delimiter);
                        sb.Append(sEG.Delimiter);
                        sb.Append(Startpoint);
                        sb.Append(sEG.Terminator);
                        string rawData = sb.ToString();
                        segments = sEG.toSEG(new SEG_DATA { Header = "HKKAZ", Num = client.SEGNUM,
                            Version = client.HIKAZS, RefNum = 0, RawData = rawData });
                        //segments = "HKKAZ:" + client.SEGNUM + ":" + client.HIKAZS + "+" + activeAccount.AccountIban + ":" + activeAccount.AccountBic + ":" + activeAccount.AccountNumber + "::280:" + activeAccount.AccountBankCode + "+N+" + FromDate + "+" + ToDate + "++" + Startpoint + "'";
                    }    
                }
            }

            if (client.BPD.IsTANRequired("HKKAZ"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKKAZ");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Message(response);

            return response;
        }

        private static AccountInformation ResolveAccountForHkkaz(FinTsClient client)
        {
            if (client.activeAccount != null)
            {
                client.Logger.LogInformation(
                    "Kontozuordnung (HKKAZ): bereits gesetztes aktives Konto wird verwendet. IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}",
                    MaskValue(client.activeAccount.AccountIban),
                    MaskValue(client.activeAccount.AccountNumber),
                    MaskValue(client.activeAccount.SubAccountFeature),
                    MaskValue(client.activeAccount.AccountBankCode));
                return client.activeAccount;
            }

            var fallbackAccount = CreateAccountFromConnection(client.ConnectionDetails);
            var candidates = UPD.AccountList ?? new List<AccountInformation>();
            if (candidates.Count == 0)
            {
                client.Logger.LogInformation(
                    "Kontozuordnung (HKKAZ): keine UPD-Konten vorhanden, konfiguriertes Konto wird verwendet. IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}",
                    MaskValue(fallbackAccount.AccountIban),
                    MaskValue(fallbackAccount.AccountNumber),
                    MaskValue(fallbackAccount.SubAccountFeature),
                    MaskValue(fallbackAccount.AccountBankCode));
                return fallbackAccount;
            }

            var connection = client.ConnectionDetails;
            var bestIdentityScore = int.MinValue;
            var scored = new List<(AccountInformation Account, int IdentityScore, int PreferenceScore)>();
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                var identityScore = CalculateIdentityScore(candidate, connection);
                var preferenceScore = CalculatePreferenceScore(candidate);
                scored.Add((candidate, identityScore, preferenceScore));
                if (identityScore > bestIdentityScore)
                {
                    bestIdentityScore = identityScore;
                }
            }

            if (bestIdentityScore <= 0)
            {
                client.Logger.LogInformation(
                    "Kontozuordnung (HKKAZ): kein passendes UPD-Konto gefunden, konfiguriertes Konto wird verwendet. IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}",
                    MaskValue(fallbackAccount.AccountIban),
                    MaskValue(fallbackAccount.AccountNumber),
                    MaskValue(fallbackAccount.SubAccountFeature),
                    MaskValue(fallbackAccount.AccountBankCode));
                return fallbackAccount;
            }

            var topByIdentity = scored
                .Where(c => c.IdentityScore == bestIdentityScore)
                .ToList();

            var bestPreference = topByIdentity.Max(c => c.PreferenceScore);
            var topCandidates = topByIdentity
                .Where(c => c.PreferenceScore == bestPreference)
                .Select(c => c.Account)
                .ToList();

            if (topCandidates.Count == 1)
            {
                var selected = topCandidates[0];
                client.Logger.LogInformation(
                    "Kontozuordnung (HKKAZ): UPD-Konto ausgewählt. IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}, HKKAZ={HasHkkaz}",
                    MaskValue(selected.AccountIban),
                    MaskValue(selected.AccountNumber),
                    MaskValue(selected.SubAccountFeature),
                    MaskValue(selected.AccountBankCode),
                    HasSegmentPermission(selected, "HKKAZ") ? "ja" : "nein");
                return selected;
            }

            client.Logger.LogWarning(
                "Mehrdeutige Kontozuordnung (HKKAZ, {Count} Treffer). Es wird auf das konfigurierte Konto zurückgefallen.",
                topCandidates.Count);

            for (int i = 0; i < topCandidates.Count; i++)
            {
                var candidate = topCandidates[i];
                client.Logger.LogInformation(
                    "Kandidat {Index}: IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}, HKKAZ={HasHkkaz}",
                    i + 1,
                    MaskValue(candidate.AccountIban),
                    MaskValue(candidate.AccountNumber),
                    MaskValue(candidate.SubAccountFeature),
                    MaskValue(candidate.AccountBankCode),
                    HasSegmentPermission(candidate, "HKKAZ") ? "ja" : "nein");
            }

            client.Logger.LogInformation(
                "Kontozuordnung (HKKAZ): konfiguriertes Konto wird verwendet. IBAN={Iban}, Konto={Account}, Unterkonto={SubAccount}, BLZ={Blz}",
                MaskValue(fallbackAccount.AccountIban),
                MaskValue(fallbackAccount.AccountNumber),
                MaskValue(fallbackAccount.SubAccountFeature),
                MaskValue(fallbackAccount.AccountBankCode));
            return fallbackAccount;
        }

        private static AccountInformation CreateAccountFromConnection(ConnectionDetails connectionDetails)
        {
            return new AccountInformation
            {
                AccountNumber = connectionDetails.Account,
                AccountBankCode = connectionDetails.Blz.ToString(),
                SubAccountFeature = connectionDetails.SubAccount,
                AccountIban = connectionDetails.Iban,
                AccountBic = connectionDetails.Bic,
            };
        }

        private static int CalculateIdentityScore(AccountInformation account, ConnectionDetails connection)
        {
            var score = 0;

            var accountIban = NormalizeIban(account.AccountIban);
            var configuredIban = NormalizeIban(connection.Iban);
            if (!string.IsNullOrEmpty(accountIban) && !string.IsNullOrEmpty(configuredIban) && accountIban == configuredIban)
            {
                score += 1000;
            }

            var accountBlz = NormalizeDigits(account.AccountBankCode);
            var configuredBlz = NormalizeDigits(connection.Blz.ToString());
            var accountNumber = NormalizeAccountNumber(account.AccountNumber);
            var configuredAccount = NormalizeAccountNumber(connection.Account);

            var exactAccount = !string.IsNullOrEmpty(account.AccountNumber)
                               && !string.IsNullOrEmpty(connection.Account)
                               && string.Equals(account.AccountNumber, connection.Account, StringComparison.Ordinal);
            var normalizedAccount = !string.IsNullOrEmpty(accountNumber)
                                    && !string.IsNullOrEmpty(configuredAccount)
                                    && string.Equals(accountNumber, configuredAccount, StringComparison.Ordinal);
            var blzMatches = !string.IsNullOrEmpty(accountBlz)
                             && !string.IsNullOrEmpty(configuredBlz)
                             && string.Equals(accountBlz, configuredBlz, StringComparison.Ordinal);

            if (exactAccount && blzMatches)
            {
                score += 900;
            }
            else if (normalizedAccount && blzMatches)
            {
                score += 800;
            }
            else if (exactAccount)
            {
                score += 400;
            }
            else if (normalizedAccount)
            {
                score += 300;
            }

            if (blzMatches)
            {
                score += 100;
            }

            if (!string.IsNullOrWhiteSpace(connection.SubAccount)
                && !string.IsNullOrWhiteSpace(account.SubAccountFeature)
                && string.Equals(account.SubAccountFeature.Trim(), connection.SubAccount.Trim(), StringComparison.Ordinal))
            {
                score += 50;
            }

            return score;
        }

        private static int CalculatePreferenceScore(AccountInformation account)
        {
            var score = 0;

            if (HasSegmentPermission(account, "HKKAZ"))
            {
                score += 10;
            }

            if (!string.IsNullOrWhiteSpace(account.AccountIban))
            {
                score += 2;
            }

            if (!string.IsNullOrWhiteSpace(account.AccountNumber))
            {
                score += 1;
            }

            return score;
        }

        private static bool HasSegmentPermission(AccountInformation account, string segment)
        {
            return account?.AccountPermissions?.Any(p => string.Equals(p.Segment, segment, StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static string NormalizeIban(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        }

        private static string NormalizeDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static string NormalizeAccountNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            if (trimmed.All(char.IsDigit))
            {
                var withoutLeadingZeros = trimmed.TrimStart('0');
                return withoutLeadingZeros.Length == 0 ? "0" : withoutLeadingZeros;
            }

            return trimmed.ToUpperInvariant();
        }

        private static string MaskValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(leer)";
            }

            var normalized = value.Trim();
            if (normalized.Length <= 8)
            {
                return new string('*', normalized.Length);
            }

            return normalized.Substring(0, 4)
                   + new string('*', normalized.Length - 8)
                   + normalized.Substring(normalized.Length - 4);
        }
    }
}
