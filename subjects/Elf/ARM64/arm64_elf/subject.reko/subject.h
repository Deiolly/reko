// subject.h
// Generated by decompiling subject.exe
// using Reko decompiler version 0.11.2.0.

/*
// Equivalence classes ////////////
Eq_1: (struct "Globals"
		(FC0 Eq_12 t0FC0)
		(16B8 Eq_15 t16B8)
		(1738 Eq_16 t1738)
		(1FC20 (ptr64 void) ptr1FC20)
		(1FF98 (ptr64 Eq_16) ptr1FF98)
		(1FFA0 (ptr64 code) ptr1FFA0)
		(1FFD0 word64 qw1FFD0)
		(1FFD8 (ptr64 (ptr64 void)) ptr1FFD8)
		(1FFE0 (ptr64 Eq_15) ptr1FFE0)
		(1FFE8 (ptr64 Eq_12) ptr1FFE8))
	globals_t (in globals @ 00000000 : (ptr64 (struct "Globals")))
Eq_2: (fn void ())
	T_2 (in fn0000000000001498 @ 00000D90 : ptr64)
	T_3 (in signature of fn0000000000001498 @ 00001498 : void)
Eq_5: (fn void ())
	T_5 (in x0 @ 00000D98 : (ptr64 Eq_5))
	T_17 (in rtld_fini @ 00001490 : (ptr64 (fn void ())))
Eq_10: (fn int32 ((ptr64 Eq_12), int32, (ptr64 (ptr64 char)), (ptr64 Eq_15), (ptr64 Eq_16), (ptr64 Eq_5), (ptr64 void)))
	T_10 (in __libc_start_main @ 00001490 : ptr64)
	T_11 (in signature of __libc_start_main @ 00000000 : void)
Eq_12: (fn int32 (int32, (ptr64 (ptr64 char)), (ptr64 (ptr64 char))))
	T_12 (in main @ 00001490 : (ptr64 (fn int32 (int32, (ptr64 (ptr64 char)), (ptr64 (ptr64 char))))))
	T_20 (in Mem0[0x000000000001FFE8<p64>:word64] @ 00001490 : word64)
Eq_15: (fn void ())
	T_15 (in init @ 00001490 : (ptr64 (fn void ())))
	T_24 (in Mem0[0x000000000001FFE0<p64>:word64] @ 00001490 : word64)
Eq_16: (fn void ())
	T_16 (in fini @ 00001490 : (ptr64 (fn void ())))
	T_26 (in Mem0[0x000000000001FF98<p64>:word64] @ 00001490 : word64)
Eq_28: (fn void ())
	T_28 (in abort @ 00001494 : ptr64)
	T_29 (in signature of abort @ 00000000 : void)
Eq_35: (fn void ())
	T_35 (in __gmon_start__ @ 000014A4 : ptr64)
Eq_38: (union "Eq_38" (int64 u0) (ptr64 u1))
	T_38 (in 0000000000020008 @ 000014C4 : ptr64)
Eq_39: (union "Eq_39" (int64 u0) (ptr64 u1))
	T_39 (in 0000000000020008 @ 000014C4 : ptr64)
Eq_48: (fn void ((ptr64 void)))
	T_48 (in x0 @ 000014D4 : (ptr64 Eq_48))
	T_58 (in func @ 00001758 : (ptr64 (fn void ((ptr64 void)))))
Eq_56: (fn int32 ((ptr64 Eq_48), (ptr64 void), (ptr64 void)))
	T_56 (in __cxa_atexit @ 00001758 : ptr64)
	T_57 (in signature of __cxa_atexit @ 00000000 : void)
// Type Variables ////////////
globals_t: (in globals @ 00000000 : (ptr64 (struct "Globals")))
  Class: Eq_1
  DataType: (ptr64 Eq_1)
  OrigDataType: (ptr64 (struct "Globals"))
T_2: (in fn0000000000001498 @ 00000D90 : ptr64)
  Class: Eq_2
  DataType: (ptr64 Eq_2)
  OrigDataType: (ptr64 (fn T_4 ()))
T_3: (in signature of fn0000000000001498 @ 00001498 : void)
  Class: Eq_2
  DataType: (ptr64 Eq_2)
  OrigDataType: 
T_4: (in fn0000000000001498() @ 00000D90 : void)
  Class: Eq_4
  DataType: void
  OrigDataType: void
T_5: (in x0 @ 00000D98 : (ptr64 Eq_5))
  Class: Eq_5
  DataType: (ptr64 Eq_5)
  OrigDataType: (ptr64 (fn void ()))
T_6: (in dwArg00 @ 00000D98 : word32)
  Class: Eq_6
  DataType: word32
  OrigDataType: word32
T_7: (in ptrArg08 @ 00000D98 : (ptr64 char))
  Class: Eq_7
  DataType: (ptr64 char)
  OrigDataType: (ptr64 char)
T_8: (in fp @ 00001460 : (ptr64 void))
  Class: Eq_8
  DataType: (ptr64 void)
  OrigDataType: (ptr64 void)
T_9: (in qwArg00 @ 00001460 : word64)
  Class: Eq_9
  DataType: word64
  OrigDataType: word64
T_10: (in __libc_start_main @ 00001490 : ptr64)
  Class: Eq_10
  DataType: (ptr64 Eq_10)
  OrigDataType: (ptr64 (fn T_27 (T_20, T_21, T_22, T_24, T_26, T_5, T_8)))
T_11: (in signature of __libc_start_main @ 00000000 : void)
  Class: Eq_10
  DataType: (ptr64 Eq_10)
  OrigDataType: 
T_12: (in main @ 00001490 : (ptr64 (fn int32 (int32, (ptr64 (ptr64 char)), (ptr64 (ptr64 char))))))
  Class: Eq_12
  DataType: (ptr64 Eq_12)
  OrigDataType: 
T_13: (in argc @ 00001490 : int32)
  Class: Eq_13
  DataType: int32
  OrigDataType: 
T_14: (in ubp_av @ 00001490 : (ptr64 (ptr64 char)))
  Class: Eq_14
  DataType: (ptr64 (ptr64 char))
  OrigDataType: 
T_15: (in init @ 00001490 : (ptr64 (fn void ())))
  Class: Eq_15
  DataType: (ptr64 Eq_15)
  OrigDataType: 
T_16: (in fini @ 00001490 : (ptr64 (fn void ())))
  Class: Eq_16
  DataType: (ptr64 Eq_16)
  OrigDataType: 
T_17: (in rtld_fini @ 00001490 : (ptr64 (fn void ())))
  Class: Eq_5
  DataType: (ptr64 Eq_5)
  OrigDataType: 
T_18: (in stack_end @ 00001490 : (ptr64 void))
  Class: Eq_8
  DataType: (ptr64 void)
  OrigDataType: 
T_19: (in 000000000001FFE8 @ 00001490 : ptr64)
  Class: Eq_19
  DataType: (ptr64 (ptr64 Eq_12))
  OrigDataType: (ptr64 (struct (0 T_20 t0000)))
T_20: (in Mem0[0x000000000001FFE8<p64>:word64] @ 00001490 : word64)
  Class: Eq_12
  DataType: (ptr64 Eq_12)
  OrigDataType: (ptr64 (fn int32 (int32, (ptr64 (ptr64 char)), (ptr64 (ptr64 char)))))
T_21: (in SLICE(qwArg00, int32, 0) @ 00001490 : int32)
  Class: Eq_13
  DataType: int32
  OrigDataType: int32
T_22: (in &ptrArg08 @ 00001490 : (ptr64 (ptr64 char)))
  Class: Eq_14
  DataType: (ptr64 (ptr64 char))
  OrigDataType: (ptr64 (ptr64 char))
T_23: (in 000000000001FFE0 @ 00001490 : ptr64)
  Class: Eq_23
  DataType: (ptr64 (ptr64 Eq_15))
  OrigDataType: (ptr64 (struct (0 T_24 t0000)))
T_24: (in Mem0[0x000000000001FFE0<p64>:word64] @ 00001490 : word64)
  Class: Eq_15
  DataType: (ptr64 Eq_15)
  OrigDataType: (ptr64 (fn void ()))
T_25: (in 000000000001FF98 @ 00001490 : ptr64)
  Class: Eq_25
  DataType: (ptr64 (ptr64 Eq_16))
  OrigDataType: (ptr64 (struct (0 T_26 t0000)))
T_26: (in Mem0[0x000000000001FF98<p64>:word64] @ 00001490 : word64)
  Class: Eq_16
  DataType: (ptr64 Eq_16)
  OrigDataType: (ptr64 (fn void ()))
T_27: (in __libc_start_main(g_ptr1FFE8, (int32) qwArg00, &ptrArg08, g_ptr1FFE0, g_ptr1FF98, x0, fp) @ 00001490 : int32)
  Class: Eq_27
  DataType: int32
  OrigDataType: int32
T_28: (in abort @ 00001494 : ptr64)
  Class: Eq_28
  DataType: (ptr64 Eq_28)
  OrigDataType: (ptr64 (fn T_30 ()))
T_29: (in signature of abort @ 00000000 : void)
  Class: Eq_28
  DataType: (ptr64 Eq_28)
  OrigDataType: 
T_30: (in abort() @ 00001494 : void)
  Class: Eq_30
  DataType: void
  OrigDataType: void
T_31: (in 000000000001FFD0 @ 000014A0 : ptr64)
  Class: Eq_31
  DataType: (ptr64 word64)
  OrigDataType: (ptr64 (struct (0 T_32 t0000)))
T_32: (in Mem0[0x000000000001FFD0<p64>:word64] @ 000014A0 : word64)
  Class: Eq_32
  DataType: word64
  OrigDataType: word64
T_33: (in 0<64> @ 000014A0 : word64)
  Class: Eq_32
  DataType: word64
  OrigDataType: word64
T_34: (in g_qw1FFD0 == 0<64> @ 00000000 : bool)
  Class: Eq_34
  DataType: bool
  OrigDataType: bool
T_35: (in __gmon_start__ @ 000014A4 : ptr64)
  Class: Eq_35
  DataType: (ptr64 Eq_35)
  OrigDataType: (ptr64 (fn T_37 ()))
T_36: (in signature of __gmon_start__ @ 00000000 : void)
  Class: Eq_36
  DataType: Eq_36
  OrigDataType: 
T_37: (in __gmon_start__() @ 000014A4 : void)
  Class: Eq_37
  DataType: void
  OrigDataType: void
T_38: (in 0000000000020008 @ 000014C4 : ptr64)
  Class: Eq_38
  DataType: int64
  OrigDataType: (union (int64 u0) (ptr64 u1))
T_39: (in 0000000000020008 @ 000014C4 : ptr64)
  Class: Eq_39
  DataType: int64
  OrigDataType: (union (int64 u1) (ptr64 u0))
T_40: (in 0x20008<u64> - 0x20008<u64> @ 00000000 : word64)
  Class: Eq_40
  DataType: int64
  OrigDataType: int64
T_41: (in 0<64> @ 000014C4 : word64)
  Class: Eq_40
  DataType: int64
  OrigDataType: word64
T_42: (in 0x20008<u64> - 0x20008<u64> == 0<64> @ 00000000 : bool)
  Class: Eq_42
  DataType: bool
  OrigDataType: bool
T_43: (in x1_12 @ 000014CC : (ptr64 code))
  Class: Eq_43
  DataType: (ptr64 code)
  OrigDataType: (ptr64 code)
T_44: (in 000000000001FFA0 @ 000014CC : ptr64)
  Class: Eq_44
  DataType: (ptr64 (ptr64 code))
  OrigDataType: (ptr64 (struct (0 T_45 t0000)))
T_45: (in Mem0[0x000000000001FFA0<p64>:word64] @ 000014CC : word64)
  Class: Eq_43
  DataType: (ptr64 code)
  OrigDataType: word64
T_46: (in 0<64> @ 000014D0 : word64)
  Class: Eq_43
  DataType: (ptr64 code)
  OrigDataType: word64
T_47: (in x1_12 == null @ 00000000 : bool)
  Class: Eq_47
  DataType: bool
  OrigDataType: bool
T_48: (in x0 @ 000014D4 : (ptr64 Eq_48))
  Class: Eq_48
  DataType: (ptr64 Eq_48)
  OrigDataType: (ptr64 (fn void ((ptr64 void))))
T_49: (in x2_11 @ 00001744 : (ptr64 void))
  Class: Eq_49
  DataType: (ptr64 void)
  OrigDataType: (ptr64 void)
T_50: (in 0<64> @ 00001744 : word64)
  Class: Eq_49
  DataType: (ptr64 void)
  OrigDataType: word64
T_51: (in x1_6 @ 00001748 : (ptr64 (ptr64 void)))
  Class: Eq_51
  DataType: (ptr64 (ptr64 void))
  OrigDataType: (ptr64 (struct (0 T_65 t0000)))
T_52: (in 000000000001FFD8 @ 00001748 : ptr64)
  Class: Eq_52
  DataType: (ptr64 (ptr64 (ptr64 void)))
  OrigDataType: (ptr64 (struct (0 T_53 t0000)))
T_53: (in Mem0[0x000000000001FFD8<p64>:word64] @ 00001748 : word64)
  Class: Eq_51
  DataType: (ptr64 (ptr64 void))
  OrigDataType: word64
T_54: (in 0<64> @ 0000174C : word64)
  Class: Eq_51
  DataType: (ptr64 (ptr64 void))
  OrigDataType: word64
T_55: (in x1_6 == null @ 00000000 : bool)
  Class: Eq_55
  DataType: bool
  OrigDataType: bool
T_56: (in __cxa_atexit @ 00001758 : ptr64)
  Class: Eq_56
  DataType: (ptr64 Eq_56)
  OrigDataType: (ptr64 (fn T_62 (T_48, T_61, T_49)))
T_57: (in signature of __cxa_atexit @ 00000000 : void)
  Class: Eq_56
  DataType: (ptr64 Eq_56)
  OrigDataType: 
T_58: (in func @ 00001758 : (ptr64 (fn void ((ptr64 void)))))
  Class: Eq_48
  DataType: (ptr64 Eq_48)
  OrigDataType: 
T_59: (in arg @ 00001758 : (ptr64 void))
  Class: Eq_59
  DataType: (ptr64 void)
  OrigDataType: 
T_60: (in dso_handle @ 00001758 : (ptr64 void))
  Class: Eq_49
  DataType: (ptr64 void)
  OrigDataType: 
T_61: (in 0<64> @ 00001758 : word64)
  Class: Eq_59
  DataType: (ptr64 void)
  OrigDataType: (ptr64 void)
T_62: (in __cxa_atexit(x0, null, x2_11) @ 00001758 : int32)
  Class: Eq_62
  DataType: int32
  OrigDataType: int32
T_63: (in 0<64> @ 00001750 : word64)
  Class: Eq_63
  DataType: word64
  OrigDataType: word64
T_64: (in x1_6 + 0<64> @ 00001750 : word64)
  Class: Eq_64
  DataType: word64
  OrigDataType: word64
T_65: (in Mem0[x1_6 + 0<64>:word64] @ 00001750 : word64)
  Class: Eq_49
  DataType: (ptr64 void)
  OrigDataType: word64
*/
typedef struct Globals {
	Eq_12 t0FC0;	// FC0
	Eq_15 t16B8;	// 16B8
	Eq_16 t1738;	// 1738
	void * ptr1FC20;	// 1FC20
	void (* ptr1FF98)();	// 1FF98
	<anonymous> * ptr1FFA0;	// 1FFA0
	word64 qw1FFD0;	// 1FFD0
	void ** ptr1FFD8;	// 1FFD8
	void (* ptr1FFE0)();	// 1FFE0
	int32 (* ptr1FFE8)(int32 x0, char ** x1, char ** x2);	// 1FFE8
} Eq_1;

typedef void (Eq_2)();

typedef void (Eq_5)();

typedef int32 (Eq_10)( *, int32, char * *,  *,  *,  *, void);

typedef int32 (Eq_12)(int32 x0, char * * x1, char * * x2);

typedef void (Eq_15)();

typedef void (Eq_16)();

typedef void (Eq_28)();

typedef void (Eq_35)();

typedef union Eq_38 {
	int64 u0;
	ptr64 u1;
} Eq_38;

typedef union Eq_39 {
	int64 u0;
	ptr64 u1;
} Eq_39;

typedef void (Eq_48)(void);

typedef int32 (Eq_56)( *, void, void);

