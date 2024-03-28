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

using Microsoft.Extensions.Logging;
using libfintx.EBICS.Commands;
using libfintx.EBICSConfig;
using libfintx.EBICS.Parameters;
using libfintx.EBICS.Responses;
using System;
using ebics = libfintx.Xsd.H004;

namespace libfintx.EBICS.Handler
{
    internal class CommandHandler
    {
        private static readonly ILogger s_logger = EbicsLogging.CreateLogger<CommandHandler>();

        internal Config Config { private get; set; }
        internal ProtocolHandler ProtocolHandler { private get; set; }

        private Command CreateCommand(Params cmdParams)
        {
            using (new MethodLogger(s_logger))
            {
                Command cmd = null;

                s_logger.LogDebug("Parameters: {params}", cmdParams.ToString());

                switch (cmdParams)
                {
                    case IniParams ini:
                        cmd = new IniCommand { Params = ini, Config = Config };
                        break;
                    case HiaParams hia:
                        cmd = new HiaCommand { Params = hia, Config = Config };
                        break;
                    case HpbParams hpb:
                        cmd = new HpbCommand {Params = hpb, Config = Config};
                        break;
                    case PtkParams ptk:
                        cmd = new PtkCommand {Params = ptk, Config = Config};
                        break;
                    case HvdParams hvd:
                        cmd = new HvdCommand { Params = hvd, Config = Config };
                        break;
                    case HvuParams hvu:
                        cmd = new HvuCommand { Params = hvu, Config = Config };
                        break;
                    case HvzParams hvz:
                        cmd = new HvzCommand { Params = hvz, Config = Config };
                        break;
                    case HtdParams htd:
                        cmd = new HtdCommand { Params = htd, Config = Config };
                        break;
                    case StaParams sta:
                        cmd = new StaCommand { Params = sta, Config = Config };
                        break;
                    case HpdParams hpd:
                        cmd = new HpdCommand { Params = hpd, Config = Config };
                        break;
                    case VmkParams vmk:
                        cmd = new VmkCommand { Params = vmk, Config = Config };
                        break;
                    case HaaParams haa:
                        cmd = new HaaCommand { Params = haa, Config = Config };
                        break;
                    case HkdParams hkd:
                        cmd = new HkdCommand { Params = hkd, Config = Config };
                        break;
                    case HevParams hev:
                        cmd = new HevCommand { Params = hev, Config = Config };
                        break;
                    case XxcParams xxc:
                        cmd = new XxcCommand { Params = xxc, Config = Config };
                        break;
                    case CctParams cct:
                        cmd = new CctCommand { Params = cct, Config = Config };
                        break;
                    default:
                        throw new NotImplementedException();
                }

                s_logger.LogDebug("Command created: {cmd}", cmd?.ToString());

                return cmd;
            }
        }

        internal T Send<T>(Params cmdParams) where T : Response
        {
            dynamic cmd = CreateCommand(cmdParams);
            ProtocolHandler.Send(cmd);
            return cmd.Response;
        }
    }
}
