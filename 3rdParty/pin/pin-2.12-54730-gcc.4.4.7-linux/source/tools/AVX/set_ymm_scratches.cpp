/*BEGIN_LEGAL 
Intel Open Source License 

Copyright (c) 2002-2012 Intel Corporation. All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.  Redistributions
in binary form must reproduce the above copyright notice, this list of
conditions and the following disclaimer in the documentation and/or
other materials provided with the distribution.  Neither the name of
the Intel Corporation nor the names of its contributors may be used to
endorse or promote products derived from this software without
specific prior written permission.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
END_LEGAL */

#include "pin.H"
#include <stdio.h>





extern "C" unsigned int ymmInitVals[];
unsigned int ymmInitVals[64];
extern "C" unsigned int xmmSaveVal[];
unsigned int xmmSaveVal[4];


extern "C" int SetYmmScratchesFun(ADDRINT ymmInitVals, ADDRINT xmmSaveVal);



// Pin calls this function every time a new instruction is encountered
VOID Instruction(INS ins, VOID *v)
{
    BOOL doInstrument =FALSE;
    xed_iclass_enum_t iclass = (xed_iclass_enum_t) INS_Opcode(ins);
    if (INS_Opcode(ins)==XED_ICLASS_FXSAVE || INS_Opcode(ins)==XED_ICLASS_FXSAVE64
        || INS_Opcode(ins)==XED_ICLASS_XSAVE || INS_Opcode(ins)==XED_ICLASS_XSAVE64)
    {
        doInstrument = TRUE;
    }
    else
    {
        for (REG reg=REG_XMM_BASE; reg <= REG_YMM_LAST; 
            reg=static_cast<REG>((static_cast<INT32>(reg)+1)))
        {
            if (INS_RegRContain(ins, reg))
            {
                doInstrument = TRUE;
                break;
            }
            else if (INS_RegWContain(ins, reg))
            {
                doInstrument = TRUE;
                break;
            }
        }
    }
    if (doInstrument)
    {
        INS_InsertCall(ins, IPOINT_BEFORE, (AFUNPTR)SetYmmScratchesFun, IARG_ADDRINT, ymmInitVals, IARG_ADDRINT, xmmSaveVal, IARG_END);
    }
}


// argc, argv are the entire command line, including pin -t <toolname> -- ...
int main(int argc, char * argv[])
{
    // initialize memory area used to set values in ymm regs
    for (int i =0; i<64; i++)
    {
        ymmInitVals[i] = 0xdeadbeef;
    }

    // Initialize pin
    PIN_Init(argc, argv);

    // Register Instruction to be called to instrument instructions
    INS_AddInstrumentFunction(Instruction, 0);
    
    // Start the program, never returns
    PIN_StartProgram();
    
    return 0;
}
