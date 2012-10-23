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
#include <stdio.h>
#include <stdlib.h>
#include "pin.H"

/*
This tool is used in conjunction with  set_fp_context_xmm_regs_app.cpp applictaion
- the test verifies that the xmm registers can be set for the application by
  PIN_CallApplicationFunction,  PIN_ExecuteAt and the function registered by PIN_AddContextChangeFunction
  that is invoked when an exception or non-fatal (i.e. handled) signal oocurs.
  Note that the thre callback function registered by the PIN_AddThreadStartFunction, can also change the xmm
  registers, however since there is much code executed before the actual application Thread main function is
  invoked, they will likely change before reaching there
*/


#ifdef TARGET_IA32E
#define NUM_XMM_REGS 16
#else
#define NUM_XMM_REGS 8
#endif

KNOB<BOOL> KnobUseIargConstContext(KNOB_MODE_WRITEONCE, "pintool",
                                   "const_context", "0", "use IARG_CONST_CONTEXT");

ADDRINT executeAtAddr = 0;
ADDRINT dumpXmmRegsAtExceptionAddr = 0;

VOID REPLACE_ReplacedXmmRegs(CONTEXT *context, THREADID tid, AFUNPTR originalFunction)
{
    printf ("TOOL in REPLACE_ReplacedXmmRegs\n");
    fflush (stdout);

    CONTEXT writableContext, *ctxt;
    if (KnobUseIargConstContext)
    { // need to copy the ctxt into a writable context
        PIN_SaveContext(context, &writableContext);
        ctxt = &writableContext;
    }
    else
    {
        ctxt = context;
    }

    /* set the xmm regs in the ctxt which is used to execute the 
       originalFunction (via PIN_CallApplicationFunction) */
    CHAR fpContextSpaceForFpConextFromPin[FPSTATE_SIZE];
    FPSTATE *fpContextFromPin = reinterpret_cast<FPSTATE *>(fpContextSpaceForFpConextFromPin);
        
    PIN_GetContextFPState(ctxt, fpContextFromPin);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[0] = 0xacdcacdc;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[1] = 0xacdcacdc;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[2] = 0xacdcacdc;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[3] = 0xacdcacdc;
    }

    PIN_SetContextFPState(ctxt, fpContextFromPin);

    // verify the xmm regs were set in the ctxt
    CHAR fpContextSpaceForFpConextFromPin2[FPSTATE_SIZE];
    FPSTATE *fpContextFromPin2 = reinterpret_cast<FPSTATE *>(fpContextSpaceForFpConextFromPin2);
    PIN_GetContextFPState(ctxt, fpContextFromPin2);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        if ((fpContextFromPin->fxsave_legacy._xmms[i]._vec64[0] !=fpContextFromPin2->fxsave_legacy._xmms[i]._vec64[0]) ||
            (fpContextFromPin->fxsave_legacy._xmms[i]._vec64[1] !=fpContextFromPin2->fxsave_legacy._xmms[i]._vec64[1]))
        {
            printf("TOOL ERROR at xmm[%d]  (%lx %lx %lx %lx) (%lx %lx %lx %lx)\n", i,
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[0],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[1],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[2],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[3],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[0],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[1],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[2],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[3]);
            exit (-1);
        }
    }

    // call the originalFunction function with the xmm regs set from above
    printf("TOOL Calling replaced ReplacedXmmRegs()\n");
    fflush (stdout);
    PIN_CallApplicationFunction(ctxt, tid, CALLINGSTD_DEFAULT, 
                                originalFunction, PIN_PARG_END());
    printf("TOOL Returned from replaced ReplacedXmmRegs()\n");
    fflush (stdout);

    if (executeAtAddr != 0)
    {
        // set xmm regs to other values
        for (int i=0; i<NUM_XMM_REGS; i++)
        {
            fpContextFromPin->fxsave_legacy._xmms[i]._vec32[0] = 0xdeadbeef;
            fpContextFromPin->fxsave_legacy._xmms[i]._vec32[1] = 0xdeadbeef;
            fpContextFromPin->fxsave_legacy._xmms[i]._vec32[2] = 0xdeadbeef;
            fpContextFromPin->fxsave_legacy._xmms[i]._vec32[3] = 0xdeadbeef;
        }

        PIN_SetContextFPState(ctxt, fpContextFromPin);
        // execute the application function ExecuteAtFunc with the xmm regs set
        PIN_SetContextReg(ctxt, REG_INST_PTR, executeAtAddr);
        printf("TOOL Calling ExecutedAtFunc\n");
        fflush (stdout);
        PIN_ExecuteAt (ctxt);
        printf("TOOL returned from ExecutedAtFunc\n");
        fflush (stdout);
    }
}



VOID Image(IMG img, void *v)
{
    RTN rtn = RTN_FindByName(img, "ReplacedXmmRegs");
    if (RTN_Valid(rtn))
    {
        PROTO proto = PROTO_Allocate(PIN_PARG(int), CALLINGSTD_DEFAULT, "ReplacedXmmRegs", PIN_PARG_END());
        RTN_ReplaceSignature(rtn, AFUNPTR(REPLACE_ReplacedXmmRegs),
            IARG_PROTOTYPE, proto,
            (KnobUseIargConstContext)?IARG_CONST_CONTEXT:IARG_CONTEXT,
            IARG_THREAD_ID,
            IARG_ORIG_FUNCPTR,
            IARG_END);
        PROTO_Free(proto);
        printf ("TOOL found and replaced ReplacedXmmRegs\n");
        fflush (stdout);

        RTN rtn = RTN_FindByName(img, "ExecutedAtFunc");
        if (RTN_Valid(rtn))
        {
            executeAtAddr = RTN_Address(rtn);
            printf ("TOOL found ExecutedAtFunc for later PIN_ExecuteAt\n");
            fflush (stdout);
        }

        rtn = RTN_FindByName(img, "DumpXmmRegsAtException");
        if (RTN_Valid(rtn))
        {
            dumpXmmRegsAtExceptionAddr = RTN_Address(rtn);
            printf ("TOOL found DumpXmmRegsAtException for later Exception\n");
            fflush (stdout);
        }
    }
}

VOID OnThread(THREADID threadIndex, CONTEXT *ctxt, INT32 flags, VOID *v)
{
    printf ("TOOL OnThread callback\n");
    fflush (stdout);
    /* set the xmm regs in the ctxt which is used to execute the thread */
    CHAR fpContextSpaceForFpConextFromPin[FPSTATE_SIZE];
    FPSTATE *fpContextFromPin = reinterpret_cast<FPSTATE *>(fpContextSpaceForFpConextFromPin);

    PIN_GetContextFPState(ctxt, fpContextFromPin);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[0] = 0xbaadf00d;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[1] = 0xbaadf00d;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[2] = 0xbaadf00d;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec32[3] = 0xbaadf00d;
    }
    PIN_SetContextFPState(ctxt, fpContextFromPin);

    // verify the xmm regs were set in the ctxt
    CHAR fpContextSpaceForFpConextFromPin2[FPSTATE_SIZE];
    FPSTATE *fpContextFromPin2 = reinterpret_cast<FPSTATE *>(fpContextSpaceForFpConextFromPin2);
    PIN_GetContextFPState(ctxt, fpContextFromPin2);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        if ((fpContextFromPin->fxsave_legacy._xmms[i]._vec64[0] != fpContextFromPin2->fxsave_legacy._xmms[i]._vec64[0]) ||
            (fpContextFromPin->fxsave_legacy._xmms[i]._vec64[1] != fpContextFromPin2->fxsave_legacy._xmms[i]._vec64[1]))
        {
            printf("TOOL ERROR2 at xmm[%d]  (%lx %lx %lx %lx) (%lx %lx %lx %lx)\n", i,
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[0],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[1],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[2],
                (unsigned long)fpContextFromPin->fxsave_legacy._xmms[i]._vec32[3],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[0],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[1],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[2],
                (unsigned long)fpContextFromPin2->fxsave_legacy._xmms[i]._vec32[3]);
            fflush (stdout);
            exit (-1);
        }
    }

    // now the thread should start running with the values from above in the xmm regs
}


void CheckAndSetFpContextXmmRegs (const CONTEXT *ctxtFrom, CONTEXT *ctxtTo)
{
    fprintf (stdout, "TOOL CheckAndSetFpContextXmmRegs\n");
    fflush (stdout);
    CHAR fpContextSpaceForFpConextFromPin[FPSTATE_SIZE];
    FPSTATE *fpContextFromPin = reinterpret_cast<FPSTATE *>(fpContextSpaceForFpConextFromPin);
    
    // the application set the each byte in the xmm regs in the state to be 0xa5 before the exception was caused
    PIN_GetContextFPState(ctxtFrom, fpContextFromPin);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        for (int j=0; j<16; j++)
        {
            if (fpContextFromPin->fxsave_legacy._xmms[i]._vec8[j] != 0xa5)
            {
                fprintf (stdout, "TOOL unexpected _xmm[%d]._vec8[%d] value %x\n", i, j,
                    (unsigned int)fpContextFromPin->fxsave_legacy._xmms[i]._vec8[j]);
                fflush (stdout);
                //exit (-1);
            }
        }
    }
    fprintf (stdout, "TOOL Checked ctxtFrom OK\n");
    fflush (stdout);


    // the tool now sets the each byte of the xmm regs in the ctxtTo to be 0x5a 
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        for (int j=0; j<16; j++)
        {
            fpContextFromPin->fxsave_legacy._xmms[i]._vec8[j] = 0x5a;
        }
    }
    PIN_SetContextFPState(ctxtTo, fpContextFromPin);
    
    // verify the setting worked
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        fpContextFromPin->fxsave_legacy._xmms[i]._vec64[0] = 0x0;
        fpContextFromPin->fxsave_legacy._xmms[i]._vec64[1] = 0x0;
    }
    PIN_GetContextFPState(ctxtTo, fpContextFromPin);
    for (int i=0; i<NUM_XMM_REGS; i++)
    {
        for (int j=0; j<16; j++)
        {
            if (fpContextFromPin->fxsave_legacy._xmms[i]._vec8[j] != 0x5a)
            {
                printf ("TOOL ERROR\n");
                fflush (stdout);
                exit (-1);
            }
        }
    }
    printf ("TOOL Checked ctxtTo OK\n");
    fflush (stdout);

    // application will verify that actual xmm registers contain 0x5a in each byte
}


// Special stack alignment ( n mod 16 ) at callee entry point, after return address has been pushed on the stack.
// n == 0 means no special alignment, e.g. regular void* alignment
// n == 16 means alignment on 16
// (reference document http://www.agner.org/optimize/calling_conventions.pdf)
#if defined(TARGET_IA32E)
LOCALVAR const INT32 StackEntryAlignment = 8;
#elif defined(TARGET_LINUX) || defined(TARGET_MAC) || defined(TARGET_BSD)
LOCALVAR const INT32 StackEntryAlignment = 12;
#else
LOCALVAR const INT32 StackEntryAlignment = 0;
#endif


INT32 GetStackAdjustmentForRedirectionToFunction(INT32 currentAlignment)
{

    
    INT32 adjustment = (currentAlignment - StackEntryAlignment) % 16;

    if (adjustment < 0)
    {
        // adjustment > -16
        adjustment = 16 + adjustment;
    }
    return adjustment;
}

// this function verifies that the xmm regs in the ctxtFrom are as they were set in the app just before the
// exception occurs. Then it sets the xmm regs in the ctxtTo to a different value, finally it causes the
// execution to continue in the application function DumpXmmRegsAtException
static void OnException(THREADID threadIndex, 
                  CONTEXT_CHANGE_REASON reason, 
                  const CONTEXT *ctxtFrom,
                  CONTEXT *ctxtTo,
                  INT32 info, 
                  VOID *v)
{
    if (CONTEXT_CHANGE_REASON_SIGRETURN == reason || CONTEXT_CHANGE_REASON_APC == reason
        || CONTEXT_CHANGE_REASON_CALLBACK == reason || CONTEXT_CHANGE_REASON_FATALSIGNAL == reason
        || ctxtTo == NULL)
    { // don't want to handle these
        return;
    }
    fprintf (stdout, "TOOL OnException callback\n");
    fflush (stdout);
    

    //PIN_SaveContext(ctxtFrom, ctxtTo);
    CheckAndSetFpContextXmmRegs(ctxtFrom, ctxtTo);
    
    
    
    // call the application function with the ctxtTo context
#ifdef TARGET_IA32E
    PIN_SetContextReg(ctxtTo, REG_RIP, dumpXmmRegsAtExceptionAddr);
    // take care of stack alignment since tool is redirecting execution flow to function
    ADDRINT curSp = PIN_GetContextReg(ctxtTo, REG_RSP);
    INT32 currentAlignment = curSp % 16;
    PIN_SetContextReg(ctxtTo, REG_RSP, curSp - GetStackAdjustmentForRedirectionToFunction(currentAlignment));
#else
    PIN_SetContextReg(ctxtTo, REG_EIP, dumpXmmRegsAtExceptionAddr);
#endif
}

int main(int argc, char **argv)
{
    PIN_Init(argc, argv);
    PIN_InitSymbols();

    //PIN_AddThreadStartFunction(OnThread, 0);
    IMG_AddInstrumentFunction(Image, 0);
    PIN_AddContextChangeFunction(OnException, 0);

    PIN_StartProgram();
    return 0;
}
