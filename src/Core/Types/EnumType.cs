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
using System.Linq;
using System.Text;

namespace Reko.Core.Types
{
    public class EnumType : DataType
    {
        public EnumType()
            : base(Domain.Enum)
        {
            this.Members = new SortedList<string, long>();
        }

        public EnumType(string name)
            : base(Domain.Enum, name)
        {
            this.Members = new SortedList<string, long>();
        }

        public EnumType(EnumType other) : this(other.Name)
        {
            this.Members = new SortedList<string, long>(other.Members);
        }

        public override int Size { get; set; }
        public SortedList<string, long> Members { get; set; }

        public override void Accept(IDataTypeVisitor v)
        {
            v.VisitEnum(this);
        }

        public override T Accept<T>(IDataTypeVisitor<T> v)
        {
            return v.VisitEnum(this);
        }

        public override DataType Clone(IDictionary<DataType, DataType>? clonedTypes)
        {
            return new EnumType(this)
            {
                Qualifier = this.Qualifier
            };
        }
    }
}
