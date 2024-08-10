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

using libfintx.FinTS.Data;
using libfintx.FinTS.BankParameterData;
using libfintx.Sepa;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using libfintx.Globals;
using libfintx.Logger;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public partial class FinTsClient : IFinTsClient
    {
        private readonly ILoggerFactory _loggerFactory;
        internal readonly ILogger<FinTsClient> Logger;
        private BPD? _bpd;

        public bool Anonymous { get; }
        public ConnectionDetails ConnectionDetails { get; }
        public AccountInformation activeAccount { get; set; }
        public string SystemId { get; internal set; }

        /// <summary>
        /// The bank parameter data store.
        /// </summary>
        internal IBpdStore BdpStore { get; }

        /// <summary>
        /// The bank parameter data of the bank given in the connection details.
        /// </summary>
        public BPD? BPD
        {
            get => _bpd ?? BPD.Parse(BdpStore.GetBPD(280, ConnectionDetails.Blz).Result, Logger);
            set => _bpd = value;
        }

        public string HITAB { get; set; }
        public string HIRMS { get; set; }
        public int HITANS { get; set; }

        internal int SEGNUM { get; set; }
        internal string HIRMSf { get; set; }
        internal string HNHBK { get; set; }
        internal int HNHBS { get; set; }
        internal int HISALS { get; set; }
        internal int HIKAZS { get; set; }
        internal int HICAZS => 1;
        public string HICAZS_Camt { get; set; }
        internal string HITAN { get; set; }
        internal int HISPAS { get; set; }
        internal int HISPAS_Pain { get; set; }
        internal bool HISPAS_AccountNationalAllowed { get; set; }

        /// <summary>
        /// Initializes a new FinTS client.
        /// </summary>
        /// <param name="connection">ConnectionDetails object must at least contain the fields: Url, HBCIVersion, UserId, Pin, Blz</param>
        /// <param name="anonymous"></param>
        /// <param name="bpdDataStore">
        /// A data store for the bank parameter data (BPD). If not given, a file store in the folder <c>FinTsGlobals.ProgramBaseDir</c> will be used.
        /// </param>
        /// <param name="loggerFactory">
        /// A logger factory to where to log information to. When not given, a default file logger is used.
        /// It is recommended to passa logger factory.
        /// <b>Note </b> that at next major version upgrade, the default file logger will be replaced by
        /// a null logger not logging output anywhere.
        /// </param>
        public FinTsClient(ConnectionDetails connection, bool anonymous = false, IBpdStore? bpdDataStore = null, ILoggerFactory? loggerFactory = null)
        {
            ConnectionDetails = connection;
            Anonymous = anonymous;
            BdpStore = bpdDataStore
                       ?? new BpdFileStore(Path.Combine(FinTsGlobals.ProgramBaseDir, "BPD"));
            activeAccount = null;

            _loggerFactory = loggerFactory ?? LoggerFactory.Create(builder => { builder.AddProvider(FileLoggerProvider.CreateLibfintxLogger()); });
            Logger = _loggerFactory.CreateLogger<FinTsClient>();

            // When deprecating the default file logger maybe in next MAJOR VERSION UPGRADE, use this:
            //_logger = loggerFactory?.CreateLogger<FinTsClient>()
            //    ?? NullLoggerFactory.Instance.CreateLogger<FinTsClient>();
        }

        internal async Task<HBCIDialogResult> InitializeConnection(string hkTanSegmentId = "HKIDN")
        {
            Logger.LogInformation("Initializing connection ...");

            HBCIDialogResult result;
            string BankCode;

            // Check if the user provided a SystemID
            if (ConnectionDetails.CustomerSystemId == null)
            {
                result = await Synchronization();
                if (!result.IsSuccess)
                {
                    Logger.LogInformation("Synchronisation failed.");
                    return result;
                }
            }
            else
            {
                SystemId = ConnectionDetails.CustomerSystemId;
            }

            try
            {
                BankCode = await Transaction.INI(this, hkTanSegmentId);
            }
            catch (Exception e)
            {
                Logger.LogInformation(e.ToString());

                throw new Exception("Software error", e);
            }

            var bankMessages = Parse_BankCode(BankCode);
            result = new HBCIDialogResult(bankMessages, BankCode);
            if (!result.IsSuccess)
                Logger.LogInformation("Initialisation failed: " + result);

            return result;
        }

        /// <summary>
        /// Synchronize bank connection
        /// </summary>
        /// <returns>
        /// Customer System ID
        /// </returns>
        public async Task<HBCIDialogResult<string>> Synchronization()
        {
            var bpdVersion = await BdpStore.GetBPDVersion(280, ConnectionDetails.Blz);

            string data = await Transaction.HKSYN(this, bpdVersion);

            var messages = Parse_BankCode(data);

            return new HBCIDialogResult<string>(messages, data, SystemId);
        }

        /// <summary>
        /// Retrieves the accounts for this client
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <returns>Gets informations about the accounts</returns>
        public async Task<HBCIDialogResult<List<AccountInformation>>> Accounts(TANDialog tanDialog)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<List<AccountInformation>>();

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result.TypedResult<List<AccountInformation>>();

            return new HBCIDialogResult<List<AccountInformation>>(result.Messages, UPD.Value, UPD.AccountList);
        }

        /// <summary>
        /// Account balance
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <returns>The balance for this account</returns>
        public async Task<HBCIDialogResult<AccountBalance>> Balance(TANDialog tanDialog)
        {
            HBCIDialogResult result = await InitializeConnection();
            if (result.HasError)
                return result.TypedResult<AccountBalance>();

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            // Success
            string BankCode = await Transaction.HKSAL(this);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result.TypedResult<AccountBalance>();

            result = await ProcessSCA(result, tanDialog);
            if (!result.IsSuccess)
                return result.TypedResult<AccountBalance>();

            BankCode = result.RawData;
            AccountBalance balance = Parse_Balance(BankCode);
            return result.TypedResult(balance);
        }

        /// <summary>
        /// Rebook money from one to another account - General method
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <param name="receiverName">Name of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="receiverIBAN">IBAN of the recipient</param>
        /// <param name="receiverBIC">BIC of the recipient</param>
        /// <param name="amount"></param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>      
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> Rebooking(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC,
            decimal amount, string purpose, string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKCUM(this, receiverName, receiverIBAN, receiverBIC, amount, purpose);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collect money from another account - General method
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <param name="payerName">Name of the payer</param>
        /// <param name="payerIBAN">IBAN of the payer</param>
        /// <param name="payerBIC">BIC of the payer</param>         
        /// <param name="amount">Amount to transfer</param>
        /// <param name="purpose">Short description of the transfer (dt. Verwendungszweck)</param>    
        /// <param name="settlementDate"></param>
        /// <param name="mandateNumber"></param>
        /// <param name="mandateDate"></param>
        /// <param name="creditorIdNumber"></param>
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> Collect(TANDialog tanDialog, string payerName, string payerIBAN, string payerBIC,
            decimal amount, string purpose, DateTime settlementDate, string mandateNumber, DateTime mandateDate, string creditorIdNumber,
            string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKDSE(this, payerName, payerIBAN, payerBIC, amount, purpose, settlementDate, mandateNumber, mandateDate, creditorIdNumber);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Collective collect money from other accounts - General method
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <param name="settlementDate"></param>
        /// <param name="painData"></param>
        /// <param name="numberOfTransactions"></param>
        /// <param name="totalAmount"></param>        
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> CollectiveCollect(TANDialog tanDialog, DateTime settlementDate, List<Pain00800202CcData> painData,
           string numberOfTransactions, decimal totalAmount, string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKDME(this, settlementDate, painData, numberOfTransactions, totalAmount);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Load mobile phone prepaid card - General method
        /// </summary>
        /// <param name="tanDialog">The TAN Dialog</param>
        /// <param name="mobileServiceProvider"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="amount">Amount to transfer</param>            
        /// <param name="hirms">Numerical SecurityMode; e.g. 911 for "Sparkasse chipTan optisch"</param>
        /// <returns>
        /// Bank return codes
        /// </returns>
        public async Task<HBCIDialogResult> Prepaid(TANDialog tanDialog, int mobileServiceProvider, string phoneNumber,
            int amount, string hirms)
        {
            var result = await InitializeConnection();
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog, true);
            if (!result.IsSuccess)
                return result;

            TransactionConsole.Output = string.Empty;

            if (!string.IsNullOrEmpty(hirms))
                HIRMS = hirms;

            string BankCode = await Transaction.HKPPD(this, mobileServiceProvider, phoneNumber, amount);
            result = new HBCIDialogResult(Parse_BankCode(BankCode), BankCode);
            if (result.HasError)
                return result;

            result = await ProcessSCA(result, tanDialog);

            return result;
        }

        /// <summary>
        /// Process required SCA (strong customer authentication).
        /// </summary>
        /// <param name="result"></param>
        /// <param name="tanDialog"></param>
        /// <param name="ini">Wenn die SCA direkt nach der Dialog-Initialisierung erforderlich ist.</param>
        /// <returns></returns>
        private async Task<HBCIDialogResult> ProcessSCA(HBCIDialogResult result, TANDialog tanDialog, bool ini = false)
        {
            if (!result.IsSCARequired)
            {
                return result;
            }

            tanDialog.DialogResult = result;
            if (result.IsTanRequired)
            {
                string tan = await Helper.WaitForTanAsync(this, result, tanDialog);
                if (tan == null)
                {
                    // Wenn der User keine TAN eingegeben hat, können wir nichts tun
                }
                else
                {
                    result = await TAN(tan);
                }
            }
            else if (result.IsApprovalRequired)
            {
                // Ohne automatisierte Statusabfrage:
                // await Helper.WaitForTanAsync(this, result, tanDialog);
                // result = await TAN(null);


                // Mit automatisierter Statusabfrage
                await tanDialog.WaitForTanAsync(); // Dem Benutzer signalisieren, dass auf die Freigabe gewartet wird

                const int Delay = 2000; // Der minimale Zeitraum zwischen zwei Statusabfragen steht in HITANS, wir nehmen einfach 2 Sek
                await Task.Delay(Delay);
                result = await TAN(null);
                while (!result.IsSuccess && !result.HasError && result.IsWaitingForApproval) // Freigabe wurde noch nicht erteilt
                {
                    await Task.Delay(Delay);
                    if (tanDialog.IsCancelWaitForApproval)
                    {
                        // Nichts tun
                    }
                    else
                    {
                        result = await TAN(null);
                    }
                }

                await tanDialog.OnTransactionEndAsync(result.IsSuccess); // Dem Benutzer signalisieren, dass die Transaktion beendet ist
            }

            if (result.IsSuccess && ini)
            {
                // Fand die SCA direkt nach der Initialisierung statt, ist in der Antwort BPD/UPD enthalten
                Parse_Segments(result.RawData);
            }

            return result;
        }
    }
}
