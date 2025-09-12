using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Sixty502DotNet.Runtime;
using Sixty502DotNet.Shared;

namespace Sixty502DotNet;

public class Z80Assembler
{
    // Ensambla código Z80 usando la API moderna del upstream (ProgramParser/Interpreter)
    // y expone un resultado funcional con binario y una depuración mínima
    // derivada de las listings generadas por el intérprete.
    public Result<AssemblyData> Assemble(string input)
    {
        try
        {
            var options = new Options
            {
                ArchitectureOptions = new ArchitectureOptions
                {
                    Cpuid = "z80",
                    AutosizeRegisters = false,
                    BranchAlways = false,
                    LongAddressing = false,
                },
                GeneralOptions = new GeneralOptions
                {
                    CaseSensitive = false,
                    ResetPCOnBank = false,
                    Defines = Array.Empty<string>(),
                    IncludePath = null,
                    Sections = Array.Empty<string>()
                },
                OutputOptions = new OutputOptions
                {
                    Format = null,
                    NoSource = false,
                    NoAssembly = false,
                    NoDisassembly = false,
                    LabelsOnly = false,
                    ViceLabels = false,
                    TruncateAssembly = false,
                    VerboseList = false,
                },
                DiagnosticOptions = new DiagnosticOptions
                {
                    EchoEachPass = false,
                    NoHighlighting = true,
                    WarnJumpBug = false,
                    WarnTextInNonTextPseudoOp = false,
                    WarningsAsErrors = false,
                    WarnWhitespaceBeforeLabels = false,
                    DoNotWarnAboutUnusedSections = true,
                    WarnCaseMismatch = false,
                    WarnRegistersAsIdentifiers = false,
                    WarnSimplifyCallReturn = false,
                    WarnOfUnreferencedSymbols = false,
                }
            };

            var interpreter = new Shared.Interpreter(options, new FileSystemBinaryReader(null));
            var state = interpreter.Exec(new List<string> { input }, new StringSourceFactory());

            if (state.Errors.Count > 0)
            {
                var errText = string.Join(System.Environment.NewLine, state.Errors.Select(e =>
                {
                    var tokenPart = e.Token != null ? $" '{e.Token.Text}'" : string.Empty;
                    return $"{e.Line}:{e.Column}: {e.Message}{tokenPart}";
                }));
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
