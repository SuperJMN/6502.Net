﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using DotNetAsm;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpcodeTable = System.Collections.Generic.Dictionary<string, DotNetAsm.Instruction>;

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will assemble into 6502 instructions.
    /// </summary>
    public sealed partial class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Members

        string _cpu;

        OpcodeTable _filteredOpcodes;

        bool _m16, _x16;

        OpcodeTable _opcodes;

        #endregion

        #region Methods

        void ConstructOpcodeTable()
        {
             _opcodes = new OpcodeTable(Controller.Options.StringComparar)
            {
                { "brk",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x00 } },
                { "ora (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x01 } },
                { "jam",                new Instruction  { CPU = "6502i", Size = 1,  Opcode = 0x02 } },
                { "cop",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x02 } },
                { "cop #${0:x2}",       new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x02 } },
                { "slo (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x03 } },
                { "ora ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x03 } },
                { "dop ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x04 } },
                { "tsb ${0:x2}",        new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x04 } },
                { "ora ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x05 } },
                { "asl ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x06 } },
                { "slo ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x07 } },
                { "ora [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x07 } },
                { "php",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x08 } },
                { "ora #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x09 } },
                { "asl",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x0a } },
                { "phd",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x0b } },
                { "top",                new Instruction  { CPU = "6502i", Size = 1,  Opcode = 0x0c } },
                { "top ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x0c } },
                { "tsb ${0:x4}",        new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x0c } },
                { "ora ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x0d } },
                { "asl ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x0e } },
                { "slo ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x0f } },
                { "ora ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x0f } },
                { "bpl ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x10 } },
                { "ora (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x11 } },
                { "stp",                new Instruction  { CPU = "6502i", Size = 1,  Opcode = 0x12 } },
                { "ora (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x12 } },
                { "slo (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x13 } },
                { "ora (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x13 } },
                { "dop ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x14 } },
                { "trb ${0:x2}",        new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x14 } },
                { "ora ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x15 } },
                { "asl ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x16 } },
                { "slo ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x17 } },
                { "ora [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x17 } },
                { "clc",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x18 } },
                { "ora ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x19 } },
                { "inc",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0x1a } },
                { "slo ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x1b } },
                { "tcs",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x1b } },
                { "top ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x1c } },
                { "trb ${0:x4}",        new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x1c } },
                { "ora ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x1d } },
                { "asl ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x1e } },
                { "slo ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x1f } },
                { "ora ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x1f } },
                { "jsr ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x20 } },
                { "and (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x21 } },
                { "jsl ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x22 } },
                { "jsr ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x22 } },
                { "rla (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x23 } },
                { "and ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x23 } },
                { "bit ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x24 } },
                { "and ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x25 } },
                { "rol ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x26 } },
                { "rla ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x27 } },
                { "and [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x27 } },
                { "plp",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x28 } },
                { "and #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x29 } },
                { "rol",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x2a } },
                { "anc #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x2b } },
                { "pld",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x2b } },
                { "bit ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x2c } },
                { "and ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x2d } },
                { "rol ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x2e } },
                { "rla ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x2f } },
                { "and ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x2f } },
                { "bmi ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x30 } },
                { "and (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x31 } },
                { "and (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x32 } },
                { "rla (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x33 } },
                { "and (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x33 } },
                { "bit ${0:x2},x",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x34 } },
                { "and ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x35 } },
                { "rol ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x36 } },
                { "rla ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x37 } },
                { "and [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x37 } },
                { "sec",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x38 } },
                { "and ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x39 } },
                { "dec",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0x3a } },
                { "rla ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x3b } },
                { "tsc",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x3b } },
                { "bit ${0:x4},x",      new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x3c } },
                { "and ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x3d } },
                { "rol ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x3e } },
                { "rla ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x3f } },
                { "and ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x3f } },
                { "rti",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x40 } },
                { "eor (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x41 } },
                { "wdm",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x42 } },
                { "sre (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x43 } },
                { "eor ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x43 } },
                { "mvp ${0:x2},${1:x2}",new Instruction  { CPU = "65816", Size = 3,  Opcode = 0x44 } },
                { "eor ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x45 } },
                { "lsr ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x46 } },
                { "sre ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x47 } },
                { "eor [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x47 } },
                { "pha",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x48 } },
                { "eor #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x49 } },
                { "lsr",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x4a } },
                { "asr #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x4b } },
                { "phk",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x4b } },
                { "jmp ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x4c } },
                { "eor ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x4d } },
                { "lsr ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x4e } },
                { "sre ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x4f } },
                { "eor ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x4f } },
                { "bvc ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x50 } },
                { "eor (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x51 } },
                { "eor (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x52 } },
                { "sre (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x53 } },
                { "eor (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x53 } },
                { "mvn ${0:x2},${1:x2}",new Instruction  { CPU = "65816", Size = 3,  Opcode = 0x54 } },
                { "eor ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x55 } },
                { "lsr ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x56 } },
                { "sre ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x57 } },
                { "eor [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x57 } },
                { "cli",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x58 } },
                { "eor ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x59 } },
                { "phy",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0x5a } },
                { "sre ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x5b } },
                { "tcd",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x5b } },
                { "jml ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x5c } },
                { "jmp ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x5c } },
                { "eor ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x5d } },
                { "lsr ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x5e } },
                { "sre ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x5f } },
                { "eor ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x5f } },
                { "rts",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x60 } },
                { "adc (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x61 } },
                { "per ${0:x4}",        new Instruction  { CPU = "65816", Size = 3,  Opcode = 0x62 } },
                { "rra (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x63 } },
                { "adc ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x63 } },
                { "stz ${0:x2}",        new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x64 } },
                { "adc ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x65 } },
                { "ror ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x66 } },
                { "rra ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x67 } },
                { "adc [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x67 } },
                { "pla",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x68 } },
                { "adc #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x69 } },
                { "ror",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x6a } },
                { "arr #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x6b } },
                { "rtl",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x6b } },
                { "jmp (${0:x4})",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x6c } },
                { "adc ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x6d } },
                { "ror ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x6e } },
                { "rra ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x6f } },
                { "adc ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x6f } },
                { "bvs ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x70 } },
                { "adc (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x71 } },
                { "adc (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x72 } },
                { "rra (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x73 } },
                { "adc (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x73 } },
                { "stz ${0:x2},x",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x74 } },
                { "adc ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x75 } },
                { "ror ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x76 } },
                { "rra ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x77 } },
                { "adc [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x77 } },
                { "sei",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x78 } },
                { "adc ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x79 } },
                { "ply",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0x7a } },
                { "rra ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x7b } },
                { "tdc",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x7b } },
                { "jmp (${0:x4},x)",    new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x7c } },
                { "adc ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x7d } },
                { "ror ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x7e } },
                { "rra ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x7f } },
                { "adc ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x7f } },
                { "dop",                new Instruction  { CPU = "6502i", Size = 1,  Opcode = 0x80 } },
                { "dop #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x80 } },
                { "bra ${0:x4}",        new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x80 } },
                { "sta (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x81 } },
                { "brl ${0:x4}",        new Instruction  { CPU = "65816", Size = 3,  Opcode = 0x82 } },
                { "sax (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x83 } },
                { "sta ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x83 } },
                { "sty ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x84 } },
                { "sta ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x85 } },
                { "stx ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x86 } },
                { "sax ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x87 } },
                { "sta [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x87 } },
                { "dey",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x88 } },
                { "bit #${0:x2}",       new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x89 } },
                { "txa",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x8a } },
                { "ane #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x8b } },
                { "phb",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x8b } },
                { "sty ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x8c } },
                { "sta ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x8d } },
                { "stx ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x8e } },
                { "sax ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x8f } },
                { "sta ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x8f } },
                { "bcc ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x90 } },
                { "sta (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x91 } },
                { "sta (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0x92 } },
                { "sha (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x93 } },
                { "sta (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x93 } },
                { "sty ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x94 } },
                { "sta ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x95 } },
                { "stx ${0:x2},y",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0x96 } },
                { "sax ${0:x2},y",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0x97 } },
                { "sta [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0x97 } },
                { "tya",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x98 } },
                { "sta ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x99 } },
                { "txs",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0x9a } },
                { "tas ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x9b } },
                { "txy",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0x9b } },
                { "shy ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x9c } },
                { "stz ${0:x4}",        new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x9c } },
                { "sta ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0x9d } },
                { "shx ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x9e } },
                { "stz ${0:x4},x",      new Instruction  { CPU = "65C02", Size = 3,  Opcode = 0x9e } },
                { "sha ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0x9f } },
                { "sta ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0x9f } },
                { "ldy #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa0 } },
                { "lda (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa1 } },
                { "ldx #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa2 } },
                { "lax (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xa3 } },
                { "lda ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xa3 } },
                { "ldy ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa4 } },
                { "lda ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa5 } },
                { "ldx ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa6 } },
                { "lax ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xa7 } },
                { "lda [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xa7 } },
                { "tay",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xa8 } },
                { "lda #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xa9 } },
                { "tax",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xaa } },
                { "plb",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0xab } },
                { "ldy ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xac } },
                { "lda ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xad } },
                { "ldx ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xae } },
                { "lax ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xaf } },
                { "lda ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xaf } },
                { "bcs ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xb0 } },
                { "lda (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xb1 } },
                { "lda (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0xb2 } },
                { "lax (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xb3 } },
                { "lda (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xb3 } },
                { "ldy ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xb4 } },
                { "lda ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xb5 } },
                { "ldx ${0:x2},y",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xb6 } },
                { "lax ${0:x2},y",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xb7 } },
                { "lda [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xb7 } },
                { "clv",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xb8 } },
                { "lda ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xb9 } },
                { "tsx",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xba } },
                { "las ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xbb } },
                { "tyx",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0xbb } },
                { "ldy ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xbc } },
                { "lda ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xbd } },
                { "ldx ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xbe } },
                { "lax ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xbf } },
                { "lda ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xbf } },
                { "cpy #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc0 } },
                { "cmp (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc1 } },
                { "rep #${0:x2}",       new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xc2 } },
                { "dcp (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xc3 } },
                { "cmp ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xc3 } },
                { "cpy ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc4 } },
                { "cmp ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc5 } },
                { "dec ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc6 } },
                { "dcp ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xc7 } },
                { "cmp [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xc7 } },
                { "iny",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xc8 } },
                { "cmp #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xc9 } },
                { "dex",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xca } },
                { "sax #${0:x2}",       new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xcb } },
                { "wai",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0xcb } },
                { "cpy ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xcc } },
                { "cmp ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xcd } },
                { "dec ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xce } },
                { "dcp ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xcf } },
                { "cmp ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xcf } },
                { "bne ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xd0 } },
                { "cmp (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xd1 } },
                { "cmp (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0xd2 } },
                { "dcp (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xd3 } },
                { "cmp (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xd3 } },
                { "pei (${0:x2})",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xd4 } },
                { "cmp ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xd5 } },
                { "dec ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xd6 } },
                { "dcp ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xd7 } },
                { "cmp [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xd7 } },
                { "cld",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xd8 } },
                { "cmp ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xd9 } },
                { "phx",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0xda } },
                { "dcp ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xdb } },
                { "jmp [${0:x4}]",      new Instruction  { CPU = "65816", Size = 3,  Opcode = 0xdc } },
                { "cmp ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xdd } },
                { "dec ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xde } },
                { "dcp ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xdf } },
                { "cmp ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xdf } },
                { "cpx #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe0 } },
                { "sbc (${0:x2},x)",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe1 } },
                { "sep #${0:x2}",       new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xe2 } },
                { "isb (${0:x2},x)",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xe3 } },
                { "sbc ${0:x2},s",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xe3 } },
                { "cpx ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe4 } },
                { "sbc ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe5 } },
                { "inc ${0:x2}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe6 } },
                { "isb ${0:x2}",        new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xe7 } },
                { "sbc [${0:x2}]",      new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xe7 } },
                { "inx",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xe8 } },
                { "sbc #${0:x2}",       new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xe9 } },
                { "nop",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xea } },
                { "xba",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0xeb } },
                { "cpx ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xec } },
                { "sbc ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xed } },
                { "inc ${0:x4}",        new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xee } },
                { "isb ${0:x4}",        new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xef } },
                { "sbc ${0:x6}",        new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xef } },
                { "beq ${0:x4}",        new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xf0 } },
                { "sbc (${0:x2}),y",    new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xf1 } },
                { "sbc (${0:x2})",      new Instruction  { CPU = "65C02", Size = 2,  Opcode = 0xf2 } },
                { "isb (${0:x2}),y",    new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xf3 } },
                { "sbc (${0:x2},s),y",  new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xf3 } },
                { "pea ${0:x4}",        new Instruction  { CPU = "65816", Size = 3,  Opcode = 0xf4 } },
                { "sbc ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xf5 } },
                { "inc ${0:x2},x",      new Instruction  { CPU = "6502",  Size = 2,  Opcode = 0xf6 } },
                { "isb ${0:x2},x",      new Instruction  { CPU = "6502i", Size = 2,  Opcode = 0xf7 } },
                { "sbc [${0:x2}],y",    new Instruction  { CPU = "65816", Size = 2,  Opcode = 0xf7 } },
                { "sed",                new Instruction  { CPU = "6502",  Size = 1,  Opcode = 0xf8 } },
                { "sbc ${0:x4},y",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xf9 } },
                { "plx",                new Instruction  { CPU = "65C02", Size = 1,  Opcode = 0xfa } },
                { "isb ${0:x4},y",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xfb } },
                { "xce",                new Instruction  { CPU = "65816", Size = 1,  Opcode = 0xfb } },
                { "sbc ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xfd } },
                { "inc ${0:x4},x",      new Instruction  { CPU = "6502",  Size = 3,  Opcode = 0xfe } },
                { "isb ${0:x4},x",      new Instruction  { CPU = "6502i", Size = 3,  Opcode = 0xff } },
                { "sbc ${0:x6},x",      new Instruction  { CPU = "65816", Size = 4,  Opcode = 0xff } }
            };
        }

        #endregion

    }
}