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

namespace libfintx.FinTS
{
    /// <summary>
    /// What the bank did with a payment order, see <see cref="HBCIDialogResult.GetOrderStatus"/>.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>Accepted for execution (0010/0020; instant payments also 3045/3047/3270).</summary>
        Accepted,

        /// <summary>Collective order only partly accepted (3210, 3220, 9220): check the failed items, do not resubmit all.</summary>
        PartiallyAccepted,

        /// <summary>Not executed: rejected (9xxx, 3042) or not released (0030, 3945).</summary>
        NotExecuted,

        /// <summary>Unclear, e.g. a decoupled approval is still pending (3955/3956): check the order, do not resubmit.</summary>
        Unclear,
    }
}
