﻿namespace ajivac_lib.AST;

public enum TypeKind : long
{
    Unknown = 0,
    Bit = 1,
    U32 = 2,
    U64 = 3,
    I32 = 4,
    I64 = 5,
    F32 = 6,
    F64 = 7,
    Str = 8,
    Chr = 9,
    Void = 10,
    Reserved1 = 11,
    Reserved2 = 12,
    Reserved3 = 13,
    Reserved4 = 14,
    Reserved5 = 15,
    Reserved6 = 16,
    Reserved7 = 17,
    Reserved8 = 18,
    Reserved9 = 19,
    Reserved10 = 20,
    Reserved11 = 21,
    Reserved12 = 22,
    Reserved13 = 23,
    Reserved14 = 24,
    Reserved15 = 25,
    Reserved16 = 26,
    Reserved17 = 27,
    Reserved18 = 28,
    Reserved19 = 29,
    Reserved20 = 30,
    Reserved21 = 31,
    UserDefinedBegin = 32,
}