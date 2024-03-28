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

using libfintx.EBICSConfig;
using libfintx.EBICS.Parameters;
using libfintx.EBICS.Responses;
using ebics = libfintx.Xsd.H004;

namespace Nelibfintx.EBICStEbics
{
    public interface IEbicsClient
    {
        HpbResponse HPB(HpbParams p);
        PtkResponse PTK(PtkParams p);
        CctResponse CCT(CctParams p);
        IniResponse INI(IniParams p);
        HiaResponse HIA(HiaParams p);
        SprResponse SPR(SprParams p);
        CddResponse CDD(CddParams p);
        HvzResponse HVZ(HvzParams p);
        HvuResponse HVU(HvuParams p);
        HvdResponse HVD(HvdParams p);
        HtdResponse HTD(HtdParams p);
        StaResponse STA(StaParams p);
        VmkResponse VMK(VmkParams p);
        HpdResponse HPD(HpdParams p);
        HvtResponse HVT(HvtParams p);

        HaaResponse HAA(HaaParams p);
        HkdResponse HKD(HkdParams p);
        HevResponse HEV(HevParams p);
        XxcResponse XXC(XxcParams p);

        C52Response C52(C52Params p);

        C53Response C53(C53Params p);
    }
}
