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

using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Machine;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reko.Arch.Arm.AArch32
{
    public partial class ArmRewriter
    {
        private void RewriteVbic()
        {
            if (instr.Operands.Length == 3)
            {
                RewriteVectorBinOp("__vbic_{0}", instr.vector_data, 0, 1, 2);
            }
            else
            {
                RewriteVectorBinOp("__vbic_{0}", instr.vector_data, 0, 0, 1);
            }
        }

        private void RewriteVbsl()
        {
            var src1 = Operand(1);
            var src2 = Operand(2);
            var dst = Operand(0);
            m.Assign(dst, m.Fn(vbsl_intrinsic.MakeInstance(dst.DataType), src1, src2));
        }

        private void RewriteVecBinOp(Func<Expression, Expression, Expression> fn)
        {
            if (instr.Operands.Length == 3)
            {
                var src1 = Operand(1);
                var src2 = Operand(2);
                var dst = Operand(0, PrimitiveType.Word32, true);
                m.Assign(dst, fn(src1, src2));
            }
            else
            {
                var src1 = Operand(0);
                var src2 = Operand(1);
                var dst = Operand(0, PrimitiveType.Word32, true);
                m.Assign(dst, fn(src1, src2));
            }
        }

        private void RewriteVcmp()
        {
            var src1 = Operand(0, PrimitiveType.Word32, true);
            var src2 = Operand(1);
            var fpscr = binder.EnsureFlagGroup(Registers.fpscr, 0xF0000000, "NZCV", PrimitiveType.Word32);
            m.Assign(fpscr, m.Cond(m.FSub(src1, src2)));
        }

        private void RewriteVcvt()
        {
            var src = Operand(1);
            var dst = Operand(0, PrimitiveType.Word32, true);
            DataType dstType;
            DataType srcType;
            switch (instr.vector_data)
            {
            case ArmVectorData.F32F64: dstType = PrimitiveType.Real32; srcType = PrimitiveType.Real64; break;
            case ArmVectorData.F32S16: dstType = PrimitiveType.Real32; srcType = PrimitiveType.Int16; break;
            case ArmVectorData.F32S32: dstType = PrimitiveType.Real32; srcType = PrimitiveType.Int32; break;
            case ArmVectorData.F32U32: dstType = PrimitiveType.Real32; srcType = PrimitiveType.UInt32; break;
            case ArmVectorData.F64S16: dstType = PrimitiveType.Real64; srcType = PrimitiveType.Int16; break;
            case ArmVectorData.F64S32: dstType = PrimitiveType.Real64; srcType = PrimitiveType.Int32; break;
            case ArmVectorData.F64F32: dstType = PrimitiveType.Real64; srcType = PrimitiveType.Real32; break;
            case ArmVectorData.F64U32: dstType = PrimitiveType.Real64; srcType = PrimitiveType.UInt32; break;
            case ArmVectorData.S32F32: dstType = PrimitiveType.Int32; srcType = PrimitiveType.Real32; break;
            default: NotImplementedYet(); return;
            }
            if (dst.DataType.BitSize == dstType.BitSize && src.DataType.BitSize == srcType.BitSize)
            {
                m.Assign(dst, m.Convert(src, srcType, dstType));
            }
            else
            {
                var cElems = dst.DataType.BitSize / dstType.BitSize;
                var aSrc = new ArrayType(srcType, cElems);
                var aDst = new ArrayType(dstType, cElems);
                var tmpSrc = binder.CreateTemporary(aSrc);
                m.Assign(tmpSrc, src);
                m.Assign(dst, host.Intrinsic($"__vcvt_{vectorConversionNames[instr.vector_data]}", true, aDst, tmpSrc));
            }
        }

        private void RewriteVcvtr()
        {
            var src = Operand(1);
            var dst = Operand(0, PrimitiveType.Word32, true);
            DataType srcType;
            DataType dstType;
            switch (instr.vector_data)
            {
            case ArmVectorData.S32F32: srcType = PrimitiveType.Real32; dstType = PrimitiveType.Int32; break;
            case ArmVectorData.S32F64: srcType = PrimitiveType.Real64; dstType = PrimitiveType.Int32; break;
            case ArmVectorData.U32F32: srcType = PrimitiveType.Real32; dstType = PrimitiveType.UInt32; break;
            case ArmVectorData.U32F64: srcType = PrimitiveType.Real64; dstType = PrimitiveType.UInt32; break;
            default: NotImplementedYet(); return;
            }
            src = host.Intrinsic("trunc", true, src.DataType, src);
            m.Assign(dst, m.Convert(src, srcType, dstType));
        }

        private void RewriteVext()
        {
            var src1 = Operand(1);
            var src2 = Operand(2);
            var src3 = Operand(3);
            var dst = Operand(0, PrimitiveType.Word32, true);
            var intrinsic = host.Intrinsic("__vext", true, dst.DataType, src1, src2, src3);
            m.Assign(dst, intrinsic);
        }

        private void RewriteVldmia()
        {
            var rSrc = this.Operand(0, PrimitiveType.Word32, true);
            var offset = 0;
            foreach (var r in ((MultiRegisterOperand)instr.Operands[1]).GetRegisters())
            {
                var dst = Reg(r);
                Expression ea =
                    offset != 0
                    ? m.IAddS(rSrc, offset)
                    : rSrc;
                m.Assign(dst, m.Mem(dst.DataType, ea));
                offset += dst.DataType.Size;
            }
            if (instr.Writeback)
            {
                m.Assign(rSrc, m.IAddS(rSrc, offset));
            }
        }

        private void RewriteVld1()
        {
            RegisterStorage[] regs;
            if (instr.Operands[0] is VectorMultipleRegisterOperand vopDst)
            {
                regs = vopDst.GetRegisters().ToArray();
            }
            else
            {
                var opDst = (MultiRegisterOperand) instr.Operands[0];
                regs = opDst.GetRegisters().ToArray();
            }

            var opSrc = (MemoryOperand) instr.Operands[1];
            if (opSrc.BaseRegister is null)
            {
                iclass = InstrClass.Invalid;
                m.Invalid();
                return;
            }
            var tmp = binder.CreateTemporary(PrimitiveType.CreateWord(32));
            var dt = Arm32Architecture.VectorElementDataType(instr.vector_data);
            var reg = binder.EnsureRegister(opSrc.BaseRegister);

            var nRegs = regs.Length;
            m.Assign(tmp, m.Fn(vld1_multi_intrinsic.MakeInstance(32, dt), reg));
            int bitOffset = 64 * (nRegs - 1);
            foreach (var dreg in regs)
            {
                m.Assign(
                    binder.EnsureRegister(dreg),
                    m.Slice(tmp, dreg.DataType, bitOffset));
                bitOffset -= 64;
            }
            if (instr.Writeback)
            {
                if (opSrc.Index is not null)
                    throw new NotImplementedException();
                m.Assign(reg, m.IAddS(reg, nRegs * 8));
            }
        }

        private void RewriteVldN(IntrinsicProcedure intrinsic)
        {
            RegisterStorage[] regs;
            if (instr.Operands[0] is VectorMultipleRegisterOperand vopDst)
            {
                regs = vopDst.GetRegisters().ToArray();
            }
            else
            {
                var opDst = (MultiRegisterOperand) instr.Operands[0];
                regs = opDst.GetRegisters().ToArray();
            }
            var opSrc = (MemoryOperand) instr.Operands[1];
            if (opSrc.BaseRegister is null)
            {
                iclass = InstrClass.Invalid;
                m.Invalid();
                return;
            }
            var tmp = binder.CreateTemporary(PrimitiveType.CreateWord(32));
            var dt = Arm32Architecture.VectorElementDataType(instr.vector_data);
            var reg = binder.EnsureRegister(opSrc.BaseRegister);

            int nRegs = regs.Length;
             m.Assign(tmp, m.Fn(intrinsic.MakeInstance(32, dt), reg));
            int bitOffset = 64 * (nRegs - 1);
            foreach (var dreg in regs)
            {
                m.Assign(
                    binder.EnsureRegister(dreg),
                    m.Slice(tmp, dreg.DataType, bitOffset));
                bitOffset -= 64;
            }
            if (instr.Writeback)
            {
                if (opSrc.Index is not null)
                    throw new NotImplementedException();
                m.Assign(reg, m.IAddS(reg, nRegs * 8));
            }
        }

        private void RewriteVldr()
        {
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var src = this.Operand(1);
            m.Assign(dst, src);
        }

        private void RewriteVmov()
        {
            //if (instr.ops.Length > 2) throw new NotImplementedException();
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var src = this.Operand(1);
            if (instr.vector_data != ArmVectorData.INVALID && !(src is ArrayAccess))
            {
                var dt = Arm32Architecture.VectorElementDataType(instr.vector_data);
                var dstType = dst.DataType;
                var srcType = src.DataType;
                var srcElemSize = Arm32Architecture.VectorElementDataType(instr.vector_data);
                var celemSrc = dstType.BitSize / srcElemSize.BitSize;
                var arrDst = new ArrayType(dstType, celemSrc);

                if (instr.Operands[1] is ImmediateOperand imm)
                {
                    if (dst.DataType.BitSize == 128)
                    {
                        src = m.Seq(src, src);
                    }
                    m.Assign(dst, src);
                    return;
                }
 
                var fname = $"__vmov_{VectorElementTypeName(instr.vector_data)}";
                var intrinsic = host.Intrinsic(fname, true, dstType, src);
                m.Assign(dst, m.Fn(intrinsic));
            }
            else
            {
                if (dst.DataType.BitSize != src.DataType.BitSize)
                {
                    src = m.Convert(src, src.DataType, dst.DataType);
                }
                m.Assign(dst, src);
            }
        }

        private void RewriteVmrs()
        {
            var expSysreg = instr.Operands[1];
            var dst = Operand(0);
            RegisterStorage sysreg;
            if (expSysreg is RegisterStorage regSysreg)
            {
                sysreg = regSysreg;
            }
            else
            {
                var nsysreg = ((ImmediateOperand) expSysreg).Value.ToInt32();
                switch (nsysreg)
                {
                case 0: sysreg = Registers.fpsid; break;
                case 1:
                    sysreg = Registers.fpscr;
                    dst = NZCV();
                    break;
                case 5: sysreg = Registers.mvfr2; break;
                case 6: sysreg = Registers.mvfr1; break;
                case 7: sysreg = Registers.mvfr0; break;
                case 8: sysreg = Registers.fpexc; break;
                default: Invalid(); return;
                }
            }
            m.Assign(dst, binder.EnsureRegister(sysreg));
        }

        private void RewriteVmvn()
        {
            if (instr.Operands[1] is ImmediateOperand)
            {
                RewriteVectorUnaryOp("__vmvn_imm_{0}");
            }
            else
            {
                RewriteVectorUnaryOp("__vmvn_{0}");
            }
        }

        private void RewriteVpop()
        {
            throw new NotImplementedException();
            /*
	int offset = 0;
	var begin = &instr.operands[0];
	var end = begin + instr.op_count;
            var sp = Reg(Registers.sp);
	var reg_size = type_sizes[(int)register_types[begin->reg]];
	for (var r = begin; r != end; ++r)
	{
		var dst = Reg(r->reg);
		Expression ea = offset != 0
			? m.IAdd(sp, m.Int32(offset))
			: sp;
		var dt = register_types[r->reg];
		m.Assign(dst, m.Mem(dt, ea));
		offset += type_sizes[(int)dt];
	}
	// Release space used by registers
	m.Assign(sp, m.IAdd(sp, m.Int32(instr.op_count * reg_size)));
    */
        }

        private void RewriteVpush()
        {
            throw new NotImplementedException();
            /*
            int offset = 0;
            var begin = &instr.operands[0];
            var end = begin + instr.op_count;
            var sp = Reg(ARM_REG_SP);
            var reg_size = type_sizes[(int)register_types[begin->reg]];
            // Allocate space for the registers
            m.Assign(sp, m.ISub(sp, m.Int32(instr.op_count * reg_size)));
            for (var r = begin; r != end; ++r)
            {
                var src = Reg(r->reg);
                Expression ea = offset != 0
                    ? m.IAdd(sp, m.Int32(offset))
                    : sp;
                var dt = register_types[r->reg];
                m.Assign(m.Mem(dt, ea), src);
                offset += type_sizes[(int)dt];
            }
            */
        }

        private void RewriteVstmia(bool add, bool writeback)
        {
            var rSrc = this.Operand(0, PrimitiveType.Word32, true);
            var regs = ((MultiRegisterOperand)instr.Operands[1]).GetRegisters().ToArray();
            int totalRegsize = regs.Length * regs[0].DataType.Size;
            int offset = add ? 0 : -totalRegsize;
            foreach (var r in regs)
            {
                var dst = Reg(r);
                Expression ea =
                    offset != 0
                    ? m.IAddS(rSrc, offset)
                    : rSrc;
                m.Assign(m.Mem(r.DataType, ea), dst);
                offset += r.DataType.Size;
            }
            if (instr.Writeback)
            {
                if (add)
                {
                    m.Assign(rSrc, m.IAddS(rSrc, totalRegsize));
                }
                else
                {
                    m.Assign(rSrc, m.ISubS(rSrc, totalRegsize));
                }
            }
        }

        private void RewriteVsqrt()
        {
            var fnname = instr.vector_data == ArmVectorData.F32 ? "sqrtf" : "sqrt";
            var dt = instr.vector_data == ArmVectorData.F32 ? PrimitiveType.Real32 : PrimitiveType.Real64;
            var src = this.Operand(1);
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var intrinsic = host.Intrinsic(fnname, true, dt, src);
            m.Assign(dst, intrinsic);
        }

        private void RewriteVdup()
        {
            var src = this.Operand(1);
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var dstType = dst.DataType;
            var srcType = src.DataType;
            int elemBitSize = BitSize(instr.vector_data);
            var celem = dstType.BitSize / elemBitSize;
            var arrType = new ArrayType(srcType, celem);
            var fnName = $"__vdup_{elemBitSize}";
            var intrinsic = host.Intrinsic(fnName, true, arrType, src);
            m.Assign(dst, intrinsic);
        }

        private void RewriteVfmas(string intrinsicName, Func<Expression,Expression,Expression> scalar)
        {
            Expression RegOperand(int iOp, PrimitiveType dt)
            {
                var op = instr.Operands[iOp];
                var reg = binder.EnsureRegister((RegisterStorage)op);
                if (reg.DataType.BitSize > dt.BitSize)
                {
                    var tmp = binder.CreateTemporary(dt);
                    m.Assign(tmp, m.Slice(reg, dt, 0));
                    return tmp;
                }
                return reg;
            }
            var dt = VectorElementType(instr.vector_data);
            if (dt.BitSize > instr.Operands[1].Width.BitSize)
            {
                EmitUnitTest(instr);
                m.Invalid();
                iclass = InstrClass.Invalid;
            }
            else
            {
                var op1 = RegOperand(1, dt);
                var op2 = RegOperand(2, dt);
                var dst = RegOperand(0, dt);
                var result = scalar(dst, m.FMul(op1, op2));
                if (result.DataType.BitSize < dst.DataType.BitSize)
                {
                    result = m.Seq(m.Word(dst.DataType.BitSize - result.DataType.BitSize, 0), result);
                }
                m.Assign(dst, result);
            }
        }

        private void RewriteVmul()
        {
            var src1 = this.Operand(1);
            var src2 = this.Operand(2);
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var dstType = dst.DataType;
            var srcType = src1.DataType;
            var srcElemSize = Arm32Architecture.VectorElementDataType(instr.vector_data);
            var celemSrc = srcType.BitSize / srcElemSize.BitSize;
            var arrSrc = new ArrayType(srcType, celemSrc);
            var arrDst = new ArrayType(dstType, celemSrc);
            var fnName = $"__vmul_{VectorElementTypeName(instr.vector_data)}";
            var intrinsic = host.Intrinsic(fnName, true, arrDst, src1, src2);
            m.Assign(dst, m.Fn(intrinsic));
        }

        private void RewriteVectorUnaryOp(string fnNameFormat)
        {
            RewriteVectorUnaryOp(fnNameFormat, instr.vector_data);
        }

        private void RewriteVectorUnaryOp(string fnNameFormat, ArmVectorData elemType)
        {
            var src1 = this.Operand(1);
            var dst = this.Operand(0, PrimitiveType.Word32, true);
            var dstType = dst.DataType;
            var srcType = src1.DataType;
            var srcElemSize = Arm32Architecture.VectorElementDataType(elemType);
            var celemSrc = srcType.BitSize / srcElemSize.BitSize;
            var arrSrc = new ArrayType(srcType, celemSrc);
            var arrDst = new ArrayType(dstType, celemSrc);
            var fnName = string.Format(fnNameFormat, VectorElementTypeName(elemType));
            var intrinsic = host.Intrinsic(fnName, true, arrDst, src1);
            m.Assign(dst, intrinsic);
        }

        private void RewriteVectorBinOp(string fnNameFormat)
        {
            RewriteVectorBinOp(fnNameFormat, instr.vector_data, 0, 1, 2);
        }

        private void RewriteVectorBinOp(string fnNameFormat, ArmVectorData elemType)
        {
            RewriteVectorBinOp(fnNameFormat, elemType, 0, 1, 2);
        }

        private void RewriteVectorBinOp(
            string fnNameFormat, 
            ArmVectorData elemType, 
            int iopDst, 
            int iopSrc1, 
            int iopSrc2)
        {
            var src1 = this.Operand(iopSrc1);
            var src2 = this.Operand(iopSrc2);
            var dst = this.Operand(iopDst, PrimitiveType.Word32, true);
            var dstType = dst.DataType;
            var srcType = src1.DataType;
            var srcElemSize = Arm32Architecture.VectorElementDataType(elemType);
            //$BUG: some instructions are returned with srcElemnSize == 0!
            var celemSrc = srcType.BitSize / (srcElemSize.BitSize != 0 ? srcElemSize.BitSize : 8);
            var arrSrc = new ArrayType(srcType, celemSrc);
            var arrDst = new ArrayType(dstType, celemSrc);
            var fnName = string.Format(fnNameFormat, VectorElementTypeName(elemType));
            var intrinsic = host.Intrinsic(fnName, true, arrDst, src1, src2);
            m.Assign(dst, intrinsic);
        }

        private void RewriteVstN(IntrinsicProcedure intrinsic)
        {
            RegisterStorage[] regs;
            if (instr.Operands[0] is VectorMultipleRegisterOperand vopDst)
            {
                regs = vopDst.GetRegisters().ToArray();
            }
            else
            {
                var opDst = (MultiRegisterOperand) instr.Operands[0];
                regs = opDst.GetRegisters().ToArray();
            }
            var opSrc = (MemoryOperand) instr.Operands[1];
            if (opSrc.BaseRegister is null)
            {
                iclass = InstrClass.Invalid;
                m.Invalid();
                return;
            }
            var seq = binder.EnsureSequence(
                PrimitiveType.CreateWord(64 * regs.Length),
                regs);
            var reg = binder.EnsureRegister(opSrc.BaseRegister);

            int nRegs = regs.Length;
            var dtElem = Arm32Architecture.VectorElementDataType(instr.vector_data);
            m.SideEffect(m.Fn(intrinsic.MakeInstance(32, dtElem, seq.DataType), seq, reg));
            if (instr.Writeback)
            {
                if (opSrc.Index is not null)
                    throw new NotImplementedException();
                m.Assign(reg, m.IAddS(reg, nRegs * 8));
            }
        }

        private void RewriteVstr()
        {
            var src = this.Operand(0, PrimitiveType.Word32, true);
            var dst = this.Operand(1);
            m.Assign(dst, src);
        }

        private void RewriteVtbl()
        {
            var idxs = this.Operand(2);
            var table = (MultiRegisterOperand) instr.Operands[1];
            var regs = table.GetRegisters().ToArray();
            var dt = PrimitiveType.CreateWord(regs.Length * regs[0].DataType.BitSize);
            var seqTable = binder.EnsureSequence(dt, regs);
            var dst = this.Operand(0);
            m.Assign(dst, m.Fn(vtbl_intrinsic.MakeInstance(dt), seqTable, idxs));
        }

        PrimitiveType VectorElementType(ArmVectorData elemType)
        {
            switch (elemType)
            {
            case ArmVectorData.I8: return PrimitiveType.Byte;
            case ArmVectorData.S8: return PrimitiveType.Int8;
            case ArmVectorData.U8: return PrimitiveType.UInt8;
            case ArmVectorData.F16: return PrimitiveType.Real16;
            case ArmVectorData.I16: return PrimitiveType.Word16;
            case ArmVectorData.S16: return PrimitiveType.Int16;
            case ArmVectorData.U16: return PrimitiveType.UInt16;
            case ArmVectorData.F32: return PrimitiveType.Real32;
            case ArmVectorData.I32: return PrimitiveType.Word32;
            case ArmVectorData.S32: return PrimitiveType.Int32;
            case ArmVectorData.U32: return PrimitiveType.UInt32;
            case ArmVectorData.F64: return PrimitiveType.Real64;
            case ArmVectorData.I64: return PrimitiveType.Word64;
            case ArmVectorData.S64: return PrimitiveType.Int64;
            case ArmVectorData.U64: return PrimitiveType.UInt64;
            default: NotImplementedYet(); return PrimitiveType.Bool;
            }
        }

        string VectorElementTypeName(ArmVectorData elemType)
        {
            switch (elemType)
            {
            case ArmVectorData.I8: return "i8";
            case ArmVectorData.S8: return "s8";
            case ArmVectorData.U8: return "u8";
            case ArmVectorData.F16: return "f16";
            case ArmVectorData.I16: return "i16";
            case ArmVectorData.S16: return "s16";
            case ArmVectorData.U16: return "u16";
            case ArmVectorData.F32: return "f32";
            case ArmVectorData.I32: return "i32";
            case ArmVectorData.S32: return "s32";
            case ArmVectorData.U32: return "u32";
            case ArmVectorData.F64: return "f64";
            case ArmVectorData.I64: return "i64";
            case ArmVectorData.S64: return "s64";
            case ArmVectorData.U64: return "u64";
            default: NotImplementedYet(); return "(NYI)";
            }
        }

        private static readonly Dictionary<ArmVectorData, string> vectorConversionNames = new Dictionary<ArmVectorData, string>
        {
            { ArmVectorData.S16F16, "i16_f16" },
            { ArmVectorData.S16F32, "i16_f32" },
            { ArmVectorData.S16F64, "i16_f64" },
            { ArmVectorData.S32F16, "i32_f16" },
            { ArmVectorData.S32F32, "i32_f32" },
            { ArmVectorData.S32F64, "i32_f64" },

            { ArmVectorData.U16F16, "u16_f16" },
            { ArmVectorData.U16F32, "u16_f32" },
            { ArmVectorData.U16F64, "u16_f64" },
            { ArmVectorData.U32F16, "u32_f16" },
            { ArmVectorData.U32F32, "u32_f32" },
            { ArmVectorData.U32F64, "u32_f64" },

            { ArmVectorData.F16F32, "f16_f32" },
            { ArmVectorData.F16F64, "f16_f64" },
            { ArmVectorData.F16S16, "f16_i16" },
            { ArmVectorData.F16S32, "f16_i32" },
            { ArmVectorData.F16U16, "f16_i16" },
            { ArmVectorData.F16U32, "f16_i32" },

            { ArmVectorData.F32S16, "f32_i16" },
            { ArmVectorData.F32S32, "f32_i32" },
            { ArmVectorData.F32F16, "f32_f16" },
            { ArmVectorData.F32F64, "f32_f64" },
            { ArmVectorData.F32U16, "f32_u16" },
            { ArmVectorData.F32U32, "f32_u32" },

            { ArmVectorData.F64S16, "f64_i16" },
            { ArmVectorData.F64S32, "f64_i32" },
            { ArmVectorData.F64U16, "f64_u16" },
            { ArmVectorData.F64U32, "f64_u32" },
            { ArmVectorData.F64F16, "f64_f16" },
            { ArmVectorData.F64F32, "f64_f32" },
        };
    }
}