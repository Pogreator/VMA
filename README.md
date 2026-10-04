# VMA Lang
Virtual machine assembly language  

## Overview
VMA (Virtual machine assembly language) is a language created to easily allow for simulation of custom architecture and logic components.

It works purely of 64 bit unsigned integers and allows for custom plugin and part creation. All code is translated to byte code, where the logicsim executes it. This language and simulator allow for simulating many different types of computers etc.

## Syntax
Example:
```
INPUTS(32) = 0 // Creates 32 64 bit inputs, each with starting value 0
OUTPUTS(16) = 0 // Creates 16 64 bit outputs, each with startng value 0

Input_a = INPUTS[0] // Gets input 0 and assigns it to Input_a

INPUTS[0] = 25 // Sets input 0 to 25

// Creates an adder, which stores results to result and overflow, with 64 bits
result, overflow = ADDER(64, Input_a, INPUTS[1])

// Creates an adder, which stores outputs to OUTPUTS[0] and OUTPUTS[1], 32 bits
ADDER(32, INPUTS[2], INPUTS[3], OUTPUTS[0], OUTPUTS[1])

// Creates a custom component with inputs a,b,c,d and outputs result0, result1, flag0, flag1
Component DualAdder(a, b, c, d) (reuslt0, result1, flag0, flag1) {
    ADDER(64, a, b, result0, flag0)
    ADDER(64, c, d, result1, flag1)
}
```