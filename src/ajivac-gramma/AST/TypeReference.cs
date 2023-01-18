namespace ajivac_lib.AST;

public  record struct TypeReference(TypeKind Kind)
{
    public bool IsUserDefined => Kind >= TypeKind.UserDefinedBegin;
    public bool IsPrimitive => Kind < TypeKind.UserDefinedBegin;
    public long UserDefinedIndex => Kind - TypeKind.UserDefinedBegin;
    public static TypeReference MakeUserDefined(long index) => new TypeReference(TypeKind.UserDefinedBegin + index);
    
    public static readonly TypeReference Bit = new TypeReference(TypeKind.Bit);
    public static readonly TypeReference U32 = new TypeReference(TypeKind.U32);
    public static readonly TypeReference U64 = new TypeReference(TypeKind.U64);
    public static readonly TypeReference I32 = new TypeReference(TypeKind.I32);
    public static readonly TypeReference I64 = new TypeReference(TypeKind.I64);
    public static readonly TypeReference F32 = new TypeReference(TypeKind.F32);
    public static readonly TypeReference F64 = new TypeReference(TypeKind.F64);
    public static readonly TypeReference Str = new TypeReference(TypeKind.Str);
    public static readonly TypeReference Chr = new TypeReference(TypeKind.Chr);
    public static readonly TypeReference Void = new TypeReference(TypeKind.Void);
    public static readonly TypeReference Unknown = new TypeReference(TypeKind.Unknown);
}
