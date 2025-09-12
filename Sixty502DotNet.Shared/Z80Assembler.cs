using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CSharpFunctionalExtensions;
using Sixty502DotNet.Runtime;
using SystemEnv = System.Environment;

namespace Sixty502DotNet.Shared;

public class Z80Assembler
{
    // Ensambla código Z80 usando la API moderna del upstream (ProgramParser/Interpreter)
    // y expone un resultado funcional con binario y una depuración mínima
    // derivada de las listings generadas por el intérprete.
    public Result<AssemblyData> Assemble(string input)
    {
        try
        {
            // Build options directly for Z80 assembly
            var options = new Options
            {
                ArchitectureOptions = new ArchitectureOptions
                {
                    Cpuid = "z80"
                },
                GeneralOptions = new GeneralOptions
                {
                    CaseSensitive = false // Z80 mnemonics in tests are uppercase (LD, PUSH). Parser expects case-insensitive keywords.
                },
                OutputOptions = new OutputOptions
                {
                    NoAssembly = false,
                    NoDisassembly = false,
                    NoSource = false,
                    VerboseList = false,
                    TruncateAssembly = false
                },
                DiagnosticOptions = new DiagnosticOptions
                {
                    NoHighlighting = true,
                    DoNotWarnAboutUnusedSections = false // equivalent to WarnNotUnusedSections = true
                }
            };

            // Keep original input; just ensure a trailing newline to help parser emit NL/EOS
            var inputNormalized = input;
            if (!inputNormalized.EndsWith("\n") && !inputNormalized.EndsWith("\r"))
            {
                inputNormalized += "\n";
            }

            // Prefix with .cpu to ensure Z80 is detected even if options are ignored
            var source = $".cpu \"z80\"\n{inputNormalized}";
            var interpreter = new Shared.Interpreter(options, new FileSystemBinaryReader(null));
            var state = interpreter.Exec(new List<string> { source }, new StringSourceFactory());

            if (state.Errors.Count > 0)
            {
                var errText = string.Join(System.Environment.NewLine, state.Errors.Select(e =>
                {
                    var tokenPart = e.Token != null ? $" '{e.Token.Text}'" : string.Empty;
                    return $"{e.Line}:{e.Column}: {e.Message}{tokenPart}";
                }));

                // Extra: dump first tokens to help debug unexpected parse state
                try
                {
                    var lexer = new SyntaxLexer(CharStreams.fromString(source))
                    {
                        IsCaseSensitive = options.GeneralOptions.CaseSensitive,
                        ReservedWords = new Dictionary<string, int>(InstructionSets.GetByCpuid("z80"), options.GeneralOptions.CaseSensitive.ToStringComparer())
                    };
                    var ts = new CommonTokenStream(lexer);
                    ts.Fill();
                    var tokens = ts.GetTokens();
                    var preview = tokens.Take(20)
                        .Select(t => $"[{t.Line}:{t.Column}] {lexer.Vocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString()} -> '{t.Text?.Replace("\r","\\r").Replace("\n","\\n")}'");
                    var previewText = string.Join(SystemEnv.NewLine, preview);
                    Console.WriteLine("=== Z80 Lexer preview (first 20 tokens) ===");
                    Console.WriteLine(previewText);
                    Console.WriteLine("=== END preview ===");
                }
                catch { }

                return Result.Failure<AssemblyData>(errText);
            }

            var bytes = state.Output.GetCompilation().ToArray();
            var debug = BuildDebugInfoFromListings(state.StatementListings);
            return Result.Success(new AssemblyData(bytes, debug));
        }
        catch (Exception ex)
        {
            return Result.Failure<AssemblyData>(ex.Message);
        }
    }

    private static IEnumerable<DebugEntry> BuildDebugInfoFromListings(IList<string> listings)
    {
        // Cada elemento en listings puede contener múltiples líneas; la primera línea
        // suele incluir dirección y el texto de la instrucción origen al final.
        // Formato (aprox):
        //   {PChex}    {bytes...}[padding]{disasm-padded}{source}
        var entries = new List<DebugEntry>();
        var rePc = new Regex(@"^[^0-9a-fA-F]?([0-9a-fA-F]{4})\s+", RegexOptions.Compiled);

        foreach (var block in listings)
        {
            var firstLine = block.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstLine)) continue;

            var m = rePc.Match(firstLine);
            if (!m.Success) continue;
            var pc = Convert.ToInt32(m.Groups[1].Value, 16);

            // La fuente suele empezar después de 32 (addr+bytes) + 24 (disasm) columnas ≈ 56
            string lineText = firstLine.Length > 56 ? firstLine[56..].Trim() : firstLine.Trim();
            entries.Add(new DebugEntry(0, pc, lineText));
        }
        return entries;
    }
}
