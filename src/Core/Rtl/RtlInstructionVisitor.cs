#region License
/* 
 * Copyright (C) 1999-2022 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Reko.Core.Rtl
{
    public interface RtlInstructionVisitor<T>
    {
        T VisitAssignment(RtlAssignment ass);
        T VisitBranch(RtlBranch branch);
        T VisitCall(RtlCall call);
        T VisitGoto(RtlGoto go);
        T VisitIf(RtlIf rtlIf);
        T VisitInvalid(RtlInvalid invalid);
        T VisitMicroGoto(RtlMicroGoto uGoto);
        T VisitMicroLabel(RtlMicroLabel uLabel);
        T VisitNop(RtlNop rtlNop);
        T VisitReturn(RtlReturn ret);
        T VisitSideEffect(RtlSideEffect side);
        T VisitSwitch(RtlSwitch sw);
    }
}
