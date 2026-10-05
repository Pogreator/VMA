# VMA Lang
Virtual machine assembly language  

## Overview
VMA (Virtual machine assembly language) is a language created to easily allow for simulation of custom architecture and logic components.

It works purely of 64 bit unsigned integers and allows for custom plugin and part creation. All code is translated to byte code, where the logicsim executes it. This language and simulator allow for simulating many different types of computers etc.

## Examples
Examples can be found in the repo in the examples folder. Look at examples/source for the code, examples/bin is the compiled bytecode

## Syntax
VMA uses a syntax similar to python mixed with C#. This provides easy to read and write code.

Comments are written with `//` like C#  
A new line is needed for every variable and call.  
Variables are handled like python, where no type is declared. The only data type is unsigned long (64 bit).

Built-ins which output numbers can directly allocate to variables. Eg `value, carry = ADDER(64, a, b)`.

## Built-ins

- `INPUTS<(), []>` — Allows creating inputs and setting or getting their values.
  - `INPUTS(<value>)` — Creates `<value>` total inputs.
  - `INPUTS[<value>]` — References the input with the specified ID.

- `OUTPUTS<(), []>` — Allows creating outputs and setting or getting their values.
  - `OUTPUTS(<value>)` — Creates `<value>` total outputs.
  - `OUTPUTS[<value>]` — References the output with the specified ID.

- `NODES<(), []>` — Allows creating internal scratchpad elements and setting or getting their values.
  - `NODES(<value>)` — Creates `<value>` total internal nodes.
  - `NODES[<value>]` — References the internal node with the specified ID.

- `COMPONENT <name> (<inputs>) (<outputs>) {<code>}` — Defines a reusable component macro with designed inputs, outputs

- `=` — Traditional assignment syntax used for raw numeric literals or variable copies.
  - `value = 5` — Assigns the numeric value `5`.
  - `OUTPUTS[0] = value` — Copies the value of `value` to output `0`.
  - Custom text labels without brackets automatically create a new internal `NODE`.

- `NOT(<bit_width>, <a>, <out>)` — Performs a bitwise NOT on `a` and writes the result to `out`.

- `AND(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise AND on `a` and `b` and writes the result to `out`.

- `NAND(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise NAND on `a` and `b` and writes the result to `out`.

- `OR(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise OR on `a` and `b` and writes the result to `out`.

- `NOR(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise NOR on `a` and `b` and writes the result to `out`.

- `XOR(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise XOR on `a` and `b` and writes the result to `out`.

- `XNOR(<bit_width>, <a>, <b>, <out>)` — Performs a bitwise XNOR on `a` and `b` and writes the result to `out`.

- `ADDER(<bit_width>, <a>, <b>, <sum>, <carry>)` — Adds `a` and `b`, writing the result to `sum` and the overflow flag to `carry`.

- `SUBTRACTOR(<bit_width>, <a>, <b>, <diff>, <borrow>)` — Subtracts `b` from `a`, writing the result to `diff` and the borrow flag to `borrow`.

- `BITSHIFT(<bit_width>, <dir>, <a>, <amt>, <out>, <flag>)` — Shifts `a` by `amt`.
  - `LEFT (0)` — Shifts left.
  - `RIGHT (1)` — Shifts right.
  - Writes the result to `out` and sets `flag` on overflow.

- `MULTIPLEXER(<bit_width>, <sel>, <a>, <b>, <out>)` — Selects between `a` and `b`.
  - `sel = 0` — Outputs `a`.
  - `sel = 1` — Outputs `b`.
