﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// Implements an assembly controller to process source input and convert 
    /// to assembled output.
    /// </summary>
    public sealed class AssemblyController
    {
        #region Members

        readonly AssemblyServices _services;
        readonly List<AssemblerBase> _assemblers;
        readonly ProcessorOptions _processorOptions;
        readonly Func<string, AssemblyServices, CpuAssembler> _cpuSetHandler;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of an <see cref="AssemblyController"/>, which controls
        /// the assembly process.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <param name="cpuSetHandler">The <see cref="CpuAssembler"/> selection handler.</param>
        /// <param name="formatSelector">The format selector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AssemblyController(IEnumerable<string> args,
                                  Func<string, AssemblyServices, CpuAssembler> cpuSetHandler,
                                  Func<string, string, IBinaryFormatProvider> formatSelector)
        {
            if (args == null || cpuSetHandler == null || formatSelector == null)
                throw new ArgumentNullException("One or more arguments was null.");
            _services = new AssemblyServices(Options.FromArgs(args));
            _services.PassChanged += (s, a) => _services.Output.Reset();
            _services.PassChanged += (s, a) => _services.SymbolManager.Reset();
            _services.FormatSelector = formatSelector;
            _processorOptions = new ProcessorOptions
            {
                CaseSensitive = _services.Options.CaseSensitive,
                Log = _services.Log,
                IncludePath = _services.Options.IncludePath,
                IgnoreCommentColons = _services.Options.IgnoreColons,
                WarnOnLabelLeft = _services.Options.WarnLeft,
                InstructionLookup = symbol => _services.InstructionLookupRules.Any(ilr => ilr(symbol)),
                IsMacroNameValid = symbol => !_assemblers.Any(asm => asm.Assembles(symbol))
            };
            _assemblers = new List<AssemblerBase>
            {
                new AssignmentAssembler(_services),
                new BlockAssembler(_services),
                new EncodingAssembler(_services),
                new PseudoAssembler(_services),
                new MiscAssembler(_services)
            };
            _cpuSetHandler = cpuSetHandler;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <returns>The time in seconds for the assembly to complete.</returns>
        /// <exception cref="Exception"></exception>
        public double Assemble()
        {
            if (_services.Options.InputFiles.Count == 0)
                _services.Log.LogEntrySimple("One or more required input files was not specified.");
            else
                Initialize();

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // process cmd line args
            if (_services.Options.Quiet)
                Console.SetOut(TextWriter.Null);

            var preprocessor = new Preprocessor(_processorOptions);
            var processed = new List<SourceLine>();
            try
            {
                if (!_services.Log.HasErrors)
                {
                    // preprocess all passed option defines and sections
                    foreach (var define in _services.Options.LabelDefines)
                        processed.Add(preprocessor.ProcessDefine(define));
                    foreach (var section in _services.Options.Sections)
                        processed.AddRange(preprocessor.Process(string.Empty, processed.Count, $".dsection {section}"));

                    // preprocess all input files 
                    foreach (var inputFile in _services.Options.InputFiles)
                        processed.AddRange(preprocessor.Process(inputFile));
                }
                if (!_services.Options.NoStats)
                {
                    Console.WriteLine($"{Assembler.AssemblerName}");
                    Console.WriteLine($"{Assembler.AssemblerVersion}");
                }
                var multiLineAssembler = new MultiLineAssembler()
                    .WithAssemblers(_assemblers)
                    .WithOptions(new MultiLineAssembler.Options
                    {
                        AllowReturn = false,
                        DisassembleAll = _services.Options.VerboseList,
                        ErrorHandler = AssemblyErrorHandler,
                        StopDisassembly = () => _services.PrintOff,
                        Evaluator = _services.Evaluator

                    });

                var disassembly = string.Empty;

                // while passes are needed
                while (_services.PassNeeded && !_services.Log.HasErrors)
                {
                    if (_services.DoNewPass() == 4)
                        throw new Exception("Too many passes attempted.");
                    _ = multiLineAssembler.AssembleLines(processed.GetIterator(), out disassembly);
                    if (!_services.PassNeeded && _services.CurrentPass == 0)
                        _services.PassNeeded = _services.SymbolManager.SearchedNotFound;
                }
                if (!_services.Options.WarnNotUnusedSections && !_services.Log.HasErrors)
                {
                    var unused = _services.Output.UnusedSections;
                    if (unused.Any())
                    {
                        foreach (var section in unused)
                            _services.Log.LogEntrySimple($"Section {section} was defined but never used.", false);
                    }
                }
                var byteCount = 0;
                if (_services.Log.HasErrors)
                {
                    if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                        _services.Log.DumpWarnings();
                    DumpErrors(false);
                }
                else
                {
                    var passedArgs = _services.Options.GetPassedArgs();
                    var exec = Process.GetCurrentProcess().MainModule.ModuleName;
                    var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                    var fullDisasm = $"// {Assembler.AssemblerNameSimple}\n" +
                                     $"// {exec} {string.Join(' ', passedArgs)}\n" +
                                     $"// {DateTime.Now:f}\n\n// Input files:\n\n" +
                                     $"// {inputFiles}\n\n" + disassembly;
                    byteCount = WriteOutput(fullDisasm);
                    if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                        _services.Log.DumpWarnings();
                    if (!_services.Options.NoStats)
                    {
                        Console.WriteLine("\n*********************************");
                        Console.WriteLine($"Assembly start: ${_services.Output.ProgramStart:X4}");
                        if (_services.Output.ProgramEnd > BinaryOutput.MaxAddress && _services.Options.LongAddressing)
                            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd:X6}");
                        else
                            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd & BinaryOutput.MaxAddress:X4}");
                        Console.WriteLine($"Passes: {_services.CurrentPass + 1}");
                    }
                }
                if (!_services.Options.NoStats)
                {   
                    Console.WriteLine($"Number of errors: {_services.Log.ErrorCount}");
                    Console.WriteLine($"Number of warnings: {_services.Log.WarningCount}");
                }
                stopWatch.Stop();
                var ts = stopWatch.Elapsed.TotalSeconds;
                if (!_services.Log.HasErrors && !_services.Options.NoStats)
                {
                    var section = _services.Options.OutputSection;
                    if (!string.IsNullOrEmpty(section))
                        Console.Write($"[{section}] ");

                    if (!string.IsNullOrEmpty(_services.Options.Patch))
                        Console.WriteLine($"{byteCount} (Offs:{_services.Options.Patch}), {ts} sec.");
                    else
                        Console.WriteLine($"{byteCount} bytes, {ts} sec.");
                    if (_services.Options.ShowChecksums)
                        Console.WriteLine($"Checksum: {_services.Output.GetOutputHash(section)}");
                    Console.WriteLine("*********************************");
                    Console.Write("Assembly completed successfully.");
                }
                _services.Log.ClearAll();
                return ts;
            }
            catch (Exception ex)
            {
                _services.Log.LogEntrySimple(ex.Message);
                return double.NaN;
            }
            finally
            {
                if (_services.Log.HasErrors)
                    DumpErrors(true);
            }
        }

        void Initialize()
        {
            CpuAssembler cpuAssembler = null;
            var cpu = _services.Options.CPU;
            if (!string.IsNullOrEmpty(cpu))
                cpuAssembler = _cpuSetHandler(cpu, _services);
            if (_services.Options.InputFiles.Count > 0)
            {
                try
                {
                    var src = new Preprocessor(_processorOptions).ProcessToFirstDirective(_services.Options.InputFiles[0]);
                    if (src != null && src.Instruction != null && src.Instruction.Name.Equals(".cpu", _services.StringViewComparer))
                    {
                        if (src.Operands.Count != 1 || !src.Operands[0].IsDoubleQuote())
                        {
                            _services.Log.LogEntry(src.Instruction,
                                "Invalid expression for directive \".cpu\".");
                        }
                        else
                            cpu = src.Operands[0].Name.ToString().TrimOnce('"');
                    }
                }
                catch (Exception ex)
                {
                    _services.Log.LogEntrySimple(ex.Message);
                }
            }
            _services.CPU = cpu;
            if (!string.IsNullOrEmpty(cpu) && cpuAssembler != null && !cpuAssembler.IsCpuValid(cpu))
            {
                _services.Log.LogEntrySimple($"Invalid CPU \"{cpu}\" specified.");
            }
            else
            {
                if (cpuAssembler == null)
                    cpuAssembler = _cpuSetHandler(cpu, _services);
                _assemblers.Add(cpuAssembler);
                _processorOptions.LineTerminates = _services.LineTerminates;
            }
        }

        void DumpErrors(bool clearLog)
        {
            _services.Log.DumpErrors();
            if (!string.IsNullOrEmpty(_services.Options.ErrorFile))
            {
                using FileStream fs = new FileStream(_services.Options.ErrorFile, FileMode.Create);
                using StreamWriter writer = new StreamWriter(fs);
                writer.WriteLine($"{Assembler.AssemblerNameSimple}");
                writer.WriteLine($"Error file generated {DateTime.Now:f}");
                writer.WriteLine($"{_services.Log.ErrorCount} error(s):\n");
                _services.Log.DumpErrors(writer);
            }
            if (clearLog) _services.Log.ClearAll();
        }

        bool AssemblyErrorHandler(AssemblerBase assembler, SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            switch (reason)
            {
                case AssemblyErrorReason.NotFound:
                    _services.Log.LogEntry(line.Instruction,
                                           $"Unknown instruction \"{line.Instruction.Name}\".");
                    return true;
                case AssemblyErrorReason.ReturnNotAllowed:
                    _services.Log.LogEntry(line.Instruction,
                                           "Directive \".return\" not valid outside of function block.");
                    return true;
                case AssemblyErrorReason.ExceptionRaised:
                    {
                        if (ex is ErrorLogFullException logExc)
                        {
                            logExc.Log.LogEntrySimple(logExc.Message);
                        }
                        else if (ex is SymbolException symbEx)
                        {
                            if (symbEx.SymbolToken != null)
                                _services.Log.LogEntry(symbEx.SymbolToken, symbEx.Message);
                            else
                                _services.Log.LogEntry(line, symbEx.Position, symbEx.Message, symbEx.SymbolName.ToString());
                            return true;
                        }
                        else if (ex is SyntaxException synEx)
                        {
                            if (synEx.Token != null)
                                _services.Log.LogEntry(synEx.Token, synEx.Message);
                            else if (line != null)
                                _services.Log.LogEntry(line, synEx.Position, synEx.Message);
                            else
                                _services.Log.LogEntrySimple(synEx.Message);
                        }
                        else if (ex is InvalidCpuException cpuEx)
                        {
                            _services.Log.LogEntry(line.Instruction, cpuEx.Message);
                        }
                        else if (ex is ReturnException ||
                                 ex is BlockAssemblerException ||
                                 ex is SectionException)
                        {
                            if (ex is ReturnException retEx)
                                _services.Log.LogEntry(line, retEx.Position, retEx.Message, line.Source);
                            else
                                _services.Log.LogEntry(line.Instruction, ex.Message);
                        }
                        else
                        {
                            if (_services.CurrentPass <= 0 || _services.PassNeeded)
                            {
                                if (assembler != null && !(ex is ProgramOverflowException))
                                {
                                    var instructionSize = assembler.GetInstructionSize(line);
                                    if (_services.Output.AddressIsValid(instructionSize + _services.Output.LogicalPC))
                                        _services.Output.AddUninitialized(instructionSize);

                                }
                                _services.PassNeeded = true;
                                return true;
                            }
                            else
                            {
                                if (ex is ExpressionException expEx)
                                {
                                    if (ex is IllegalQuantityException illegalExp)
                                    {
                                        _services.Log.LogEntry(line, illegalExp.Position,
                                            $"Illegal quantity for \"{line.Instruction.Name}\" ({illegalExp.Quantity}).");
                                        return true;
                                    }
                                    else
                                        _services.Log.LogEntry(line, expEx.Position, ex.Message);
                                }
                                else if (ex is ProgramOverflowException)
                                {
                                    _services.Log.LogEntry(line.Instruction, ex.Message);
                                }
                                else if (ex is InvalidPCAssignmentException pcEx)
                                {
                                    if (pcEx.SectionNotUsedError)
                                        _services.Log.LogEntry(line.Instruction,
                                            pcEx.Message);
                                    else
                                        _services.Log.LogEntry(line.Operands[0],
                                            $"Invalid Program Counter assignment {pcEx.Message} in expression.");
                                }
                                else
                                {
                                    _services.Log.LogEntry(line.Operands[0], ex.Message);
                                }
                            }
                        }
                    }
                    return false;
                default:
                    return true;
            }
        }

        int WriteOutput(string disassembly)
        {
            // no errors finish up
            // save to disk
            var section = _services.Options.OutputSection;
            var outputFile = _services.Options.OutputFile;
            var objectCode = _services.Output.GetCompilation(section);
            if (!string.IsNullOrEmpty(_services.Options.Patch))
            {
                if (!string.IsNullOrEmpty(_services.Options.OutputFile) ||
                    !string.IsNullOrEmpty(_services.Options.Format))
                    _services.Log.LogEntrySimple("Output options ignored for patch mode.", false);
                try
                {
                    var offsetLine = new Preprocessor(_processorOptions).ProcessDefine("patch=" + _services.Options.Patch);
                    var patchExp = offsetLine.Operands.GetIterator();
                    var offset = _services.Evaluator.Evaluate(patchExp, 0, ushort.MaxValue);
                    if (patchExp.Current != null || _services.PassNeeded)
                        throw new Exception($"Patch offset specified in the expression \"{_services.Options.Patch}\" is not valid.");
                    var filePath = Preprocessor.GetFullPath(outputFile, _services.Options.IncludePath);
                    var fileBytes = File.ReadAllBytes(filePath);
                    Array.Copy(objectCode.ToArray(), 0, fileBytes, (int)offset, objectCode.Count);
                    File.WriteAllBytes(filePath, fileBytes);
                }
                catch
                {
                    throw new Exception($"Cannot patch file \"{outputFile}\". One or more arguments is not valid, or the file does not exist.");
                }
            }
            else
            {
                var formatProvider = _services.FormatSelector?.Invoke(_services.CPU, _services.OutputFormat);
                if (formatProvider != null)
                {
                    var startAddress = _services.Output.ProgramStart;
                    if (!string.IsNullOrEmpty(section))
                        startAddress = _services.Output.GetSectionStart(section);
                    var format = _services.Options.CaseSensitive ?
                                 _services.OutputFormat :
                                 _services.OutputFormat.ToLower();
                    var info = new FormatInfo(outputFile, format, startAddress, objectCode);
                    File.WriteAllBytes(outputFile, formatProvider.GetFormat(info).ToArray());
                }
                else
                {
                    File.WriteAllBytes(outputFile, objectCode.ToArray());
                }
            }   
            // write disassembly
            if (!string.IsNullOrEmpty(disassembly) && !string.IsNullOrEmpty(_services.Options.ListingFile))
                File.WriteAllText(_services.Options.ListingFile, disassembly);

            // write labels
            if (!string.IsNullOrEmpty(_services.Options.LabelFile))
                File.WriteAllText(_services.Options.LabelFile, _services.SymbolManager.ListLabels(!_services.Options.LabelsAddressesOnly));
            else if (_services.Options.LabelsAddressesOnly && string.IsNullOrEmpty(_services.Options.LabelFile))
                _services.Log.LogEntrySimple("Label listing not specified; option '-labels-addresses-only' ignored.",
                    false);
            return objectCode.Count;
        }
        #endregion
    }
}