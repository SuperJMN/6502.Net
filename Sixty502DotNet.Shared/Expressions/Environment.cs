//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the runtime environment symbols for assembly runtime.
/// </summary>
public static class Environment
{
    private readonly static Random s_rng = new();
    private static bool s_prototypesInitialized;
    private static readonly object s_prototypeInitLock = new();

    static Environment()
    {
        ArrayType = new(TypeIds.Array, null);
        DictionaryType = new(TypeIds.Dictionary, null);
        FunctionType = new(TypeIds.Function, null);
        PrimitiveType = new(TypeIds.Primitive, null);
        StringType = new(TypeIds.String, null);
        TupleType = new(TypeIds.Tuple, null);
    }

    private static void DefineConstant(string name, double value, IScope scope)
    {
        DefineConstant(name, new NumericValue(value, true), scope);
    }

    private static void MathFunction(string name, Func<double[], double> impl, int argCount, IScope scope)
    {
        DefineConstant(name, new MathFunction(name, impl, argCount), scope);
    }

    private static void BuiltInFunction(string name, BuiltInFunctionObject function, IScope scope)
    {
        DefineConstant(name, function, scope);
    }

    // Avoid redefining prototype members across multiple Init() calls in the same process
    private static void BuiltInPrototypeFunction(string name, BuiltInFunctionObject function, Prototype prototype)
    {
        if (prototype.ResolveMember(name) == null)
        {
            DefineConstant(name, function, prototype);
        }
    }

    /// <summary>
    /// Define a constant symbol.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="value">The value bound to the constant symbol.</param>
    /// <param name="scope">The symbol's scope.</param>
    /// <param name="isBuiltIn">Indicate whether the symbol is built-in.</param>
    public static void DefineConstant(string name, ValueBase value, IScope scope, bool isBuiltIn = true)
    {
        Constant constant = new(name, value, scope, isBuiltIn);
        scope.Define(name, constant);
    }

    /// <summary>
    /// Set up the assembly environment, including defining built-in symbols.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    public static void Init(AssemblyServices services)
    {
        IScope scope = services.State.Symbols.GlobalScope;

        // Early exit if this scope was already initialized in this process
        if (scope.Lookup("CURRENT_PASS") is not null)
        {
            return;
        }

        // Global constants
        DefineConstant("CURRENT_PASS", 0, scope);
        DefineConstant("INT8_MAX", sbyte.MaxValue, scope);
        DefineConstant("INT8_MIN", sbyte.MinValue, scope);
        DefineConstant("INT16_MAX", short.MaxValue, scope);
        DefineConstant("INT16_MIN", short.MinValue, scope);
        DefineConstant("INT24_MAX", Int24.MaxValue, scope);
        DefineConstant("INT24_MIN", Int24.MinValue, scope);
        DefineConstant("INT32_MAX", int.MaxValue, scope);
        DefineConstant("INT32_MIN", int.MinValue, scope);
        DefineConstant("MATH_E", Math.E, scope);
        DefineConstant("MATH_PI", Math.PI, scope);
        DefineConstant("MATH_TAU", Math.Tau, scope);
        DefineConstant("UINT8_MAX", byte.MaxValue, scope);
        DefineConstant("UINT8_MIN", byte.MinValue, scope);
        DefineConstant("UINT16_MAX", ushort.MaxValue, scope);
        DefineConstant("UINT16_MIN", ushort.MinValue, scope);
        DefineConstant("UINT24_MAX", UInt24.MaxValue, scope);
        DefineConstant("UINT24_MIN", UInt24.MinValue, scope);
        DefineConstant("UINT32_MAX", uint.MaxValue, scope);
        DefineConstant("UINT32_MIN", uint.MinValue, scope);

        // Global built-in functions
        BuiltInFunction("binary", new BinaryFunction(), scope);
        BuiltInFunction("cbmflt", new CbmFloatFunction(services.State.Output, false), scope);
        BuiltInFunction("cbmfltp", new CbmFloatFunction(services.State.Output, true), scope);
        BuiltInFunction("char", new CharFunction(services.Encoding), scope);
        BuiltInFunction("float", new FloatFunction(), scope);
        BuiltInFunction("format", new FormatFunction(), scope);
        BuiltInFunction("int", new IntFunction(), scope);
        BuiltInFunction("peek", new PeekFunction(services.State.Output), scope);
        BuiltInFunction("poke", new PokeFunction(services.State.Output), scope);
        BuiltInFunction("range", new RangeFunction(), scope);
        BuiltInFunction("section", new SectionFunction(services), scope);
        BuiltInFunction("sizeof", new SizeofFunction(), scope);
        BuiltInFunction("typeof", new TypeofFunction(services), scope);

        // Math functions
        MathFunction("abs", p => Math.Abs(p[0]), 1, scope);
        MathFunction("atan", p => Math.Atan(p[0]), 1, scope);
        MathFunction("atan2", p => Math.Atan2(p[0], p[1]), 2, scope);
        MathFunction("atanh", p => Math.Atanh(p[0]), 1, scope);
        MathFunction("byte", p => (int)p[0] & 0xff, 1, scope);
        MathFunction("cbrt", p => Math.Cbrt(p[0]), 1, scope);
        MathFunction("ceiling", p => Math.Ceiling(p[0]), 1, scope);
        MathFunction("cos", p => Math.Cos(p[0]), 1, scope);
        MathFunction("cosh", p => Math.Cosh(p[0]), 1, scope);
        MathFunction("deg", p => p[0] * 180.0 / Math.PI, 1, scope);
        MathFunction("dword", p => (long)p[0] & 0xffffffff, 1, scope);
        MathFunction("exp", p => Math.Exp(p[0]), 1, scope);
        MathFunction("floor", p => Math.Floor(p[0]), 1, scope);
        MathFunction("frac", p => p[0] - Convert.ToInt64(p[0]), 1, scope);
        MathFunction("hypot", p => Math.Sqrt(p[0] * p[0] + p[1] * p[1]), 2, scope);
        MathFunction("ln", p => Math.Log(p[0]), 1, scope);
        MathFunction("log", p => Math.Log(p[0], p[1]), 2, scope);
        MathFunction("log2", p => Math.Log2(p[0]), 1, scope);
        MathFunction("log10", p => Math.Log10(p[0]), 1, scope);
        MathFunction("long", p => (int)p[0] & 0xffffff, 1, scope);
        MathFunction("max", p => Math.Max(p[0], p[1]), 2, scope);
        MathFunction("min", p => Math.Min(p[0], p[1]), 2, scope);
        MathFunction("pow", p => Math.Pow(p[0], p[1]), 2, scope);
        MathFunction("rad", p => p[0] * Math.PI / 180.0, 1, scope);
        MathFunction("random", p => s_rng.Next((int)p[0], (int)p[1]), 2, scope);
        MathFunction("round", p => Math.Round(p[0]), 1, scope);
        MathFunction("sgn", p => Math.Sign(p[0]), 1, scope);
        MathFunction("sin", p => Math.Sin(p[0]), 1, scope);
        MathFunction("sinh", p => Math.Sinh(p[0]), 1, scope);
        MathFunction("sqrt", p => Math.Sqrt(p[0]), 1, scope);
        MathFunction("tan", p => Math.Tan(p[0]), 1, scope);
        MathFunction("tanh", p => Math.Tanh(p[0]), 1, scope);
        MathFunction("word", p => (int)p[0] & 0xffff, 1, scope);

        // Populate prototype methods once per process (do not define prototypes in the scope)
        if (!s_prototypesInitialized)
        {
            lock (s_prototypeInitLock)
            {
                if (s_prototypesInitialized)
                {
                    // Another thread already initialized
                    goto AfterPrototypeInit;
                }

                BuiltInPrototypeFunction("concat", new ConcatMethod(services), ArrayType);
                BuiltInPrototypeFunction("contains", new ContainsMethod(services), ArrayType);
            BuiltInPrototypeFunction("every", new EveryMethod(services), ArrayType);
            BuiltInPrototypeFunction("filter", new FilterMethod(services), ArrayType);
            BuiltInPrototypeFunction("indexOf", new IndexOfMethod(services), ArrayType);
            BuiltInPrototypeFunction("intersect", new IntersectMethod(services), ArrayType);
            BuiltInPrototypeFunction("len", new LenMethod(services), ArrayType);
            BuiltInPrototypeFunction("map", new MapMethod(services), ArrayType);
            BuiltInPrototypeFunction("reduce", new ReduceMethod(services), ArrayType);
            BuiltInPrototypeFunction("reverse", new ReverseMethod(services), ArrayType);
            BuiltInPrototypeFunction("size", new SizeMethod(services), ArrayType);
            BuiltInPrototypeFunction("skip", new SkipMethod(services), ArrayType);
            BuiltInPrototypeFunction("some", new SomeMethod(services), ArrayType);
            BuiltInPrototypeFunction("sort", new SortMethod(services), ArrayType);
            BuiltInPrototypeFunction("take", new TakeMethod(services), ArrayType);
            BuiltInPrototypeFunction("toString", new ToStringMethod(services), ArrayType);
            BuiltInPrototypeFunction("toTuple", new ToTupleMethod(services), ArrayType);
            BuiltInPrototypeFunction("union", new UnionMethod(services), ArrayType);

            BuiltInPrototypeFunction("concat", new ConcatMethod(services), DictionaryType);
            BuiltInPrototypeFunction("containsKey", new ContainsKeyMethod(services), DictionaryType);
            BuiltInPrototypeFunction("keys", new KeysMethod(services), DictionaryType);
            BuiltInPrototypeFunction("len", new LenMethod(services), DictionaryType);
            BuiltInPrototypeFunction("size", new SizeMethod(services), DictionaryType);
            BuiltInPrototypeFunction("toString", new ToStringMethod(services), DictionaryType);

            BuiltInPrototypeFunction("contains", new ContainsMethod(services), StringType);
            BuiltInPrototypeFunction("concat", new ConcatMethod(services), StringType);
            BuiltInPrototypeFunction("indexOf", new IndexOfMethod(services), StringType);
            BuiltInPrototypeFunction("len", new LenMethod(services), StringType);
            BuiltInPrototypeFunction("size", new SizeMethod(services), StringType);
            BuiltInPrototypeFunction("substring", new SubstringMethod(services), StringType);
            BuiltInPrototypeFunction("toArray", new ToArrayMethod(services), StringType);
            BuiltInPrototypeFunction("toLower", new ToLowerMethod(services), StringType);
            BuiltInPrototypeFunction("toString", new ToStringMethod(services), StringType);
            BuiltInPrototypeFunction("toUpper", new ToUpperMethod(services), StringType);

            BuiltInPrototypeFunction("contains", new ContainsMethod(services), TupleType);
            BuiltInPrototypeFunction("len", new LenMethod(services), TupleType);
            BuiltInPrototypeFunction("size", new SizeMethod(services), TupleType);
            BuiltInPrototypeFunction("skip", new SkipMethod(services), TupleType);
            BuiltInPrototypeFunction("take", new TakeMethod(services), TupleType);
            BuiltInPrototypeFunction("toArray", new ToArrayMethod(services), TupleType);
            BuiltInPrototypeFunction("toString", new ToStringMethod(services), TupleType);

            BuiltInPrototypeFunction("toCbmFlt", new ToCbmFltMethod(services, false), PrimitiveType);
            BuiltInPrototypeFunction("toCbmFltp", new ToCbmFltMethod(services, true), PrimitiveType);
            BuiltInPrototypeFunction("size", new SizeMethod(services), PrimitiveType);
            BuiltInPrototypeFunction("toString", new ToStringMethod(services), PrimitiveType);

            BuiltInPrototypeFunction("toString", new ToStringMethod(services), FunctionType);

                s_prototypesInitialized = true;
            }
        }

AfterPrototypeInit:
        // set architecture specific encodings
        EncodingMappings.MapEncodings(services.Encoding);
    }

    /// <summary>
    /// Get the type scope for the array type.
    /// </summary>
    public static Prototype ArrayType { get; }

    /// <summary>
    /// Get the type scope for the dictionary type.
    /// </summary>
    public static Prototype DictionaryType { get; }

    /// <summary>
    /// Get the type scope for a callable object type.
    /// </summary>
    public static Prototype FunctionType { get; }

    /// <summary>
    /// Get the type scope for primitive types.
    /// </summary>
    public static Prototype PrimitiveType { get; }

    /// <summary>
    /// Get the type scope for the string type.
    /// </summary>
    public static Prototype StringType { get; }

    /// <summary>
    /// Get the type scope for the tuple type.
    /// </summary>
    public static Prototype TupleType { get; }
}
