    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    public class VMALang
    {
        private ulong _nextId = 0;
        
        private readonly Dictionary<String, ulong> _symbolTable = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ulong> _bytecode = new();

        private readonly Dictionary<string, ComponentDef> _components = new(StringComparer.OrdinalIgnoreCase);

        private class ComponentDef
        {
            public string Name;
            public List<string> Inputs = new();
            public List<string> Outputs = new();
            public List<string> BodyLines = new();
        }
        
        private ulong GetOrCreateID(string smybol)
        {
            smybol = smybol.Trim();
            if (TryParseNumericLiteral(smybol, out ulong literalValue, out string uniqueKey))
            {
                // Cache the literal under a unique token key string (e.g., "__lit_32") 
                if (!_symbolTable.TryGetValue(uniqueKey, out ulong litId))
                {
                    litId = _nextId++;
                    _symbolTable[uniqueKey] = litId;
                    
                    _bytecode.AddRange(new[] { 3UL, litId, literalValue });
                }
                return litId;
            }

            if (!_symbolTable.TryGetValue(smybol, out ulong id))
            {
                id = _nextId++;
                _symbolTable[smybol] = id;

                if (!smybol.Contains("["))
                {
                    _bytecode.AddRange(new[] { 3UL, id, 0UL }); 
                }
            }

            return id;
        }

        private bool TryParseNumericLiteral(string input, out ulong val, out string uniqueKey)
        {
            val = 0;
            uniqueKey = null;
            input = input.Trim();
            if (string.IsNullOrEmpty(input)) return false;

            try
            {
                bool parsed = false;
                
                // Hex parsing
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    val = Convert.ToUInt64(input[2..], 16);
                    parsed = true;
                }
                // Binary parsing
                else if (input.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                {
                    val = Convert.ToUInt64(input[2..], 2);
                    parsed = true;
                }
                // Base-10 Integer parsing
                else
                {
                    parsed = ulong.TryParse(input, out val);
                }

                if (parsed)
                {
                    uniqueKey = $"__lit_{val}__";
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public ulong[] Compile(string sourceCode)
        {
            _bytecode.Clear();
            _symbolTable.Clear();
            _components.Clear();
            _nextId = 0;

            string[] lines = sourceCode.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            List<string> cleanLines = new List<string>();

            foreach (var origLine in lines)
            {
                string line = origLine.Split("//")[0].Trim();
                if (!string.IsNullOrWhiteSpace(line)) cleanLines.Add(line);
            }

            List<string> setupLines = new List<string>();
            List<string> logicLines = new List<string>();
            bool containsEntryPoint = false;

            // Pass 1: Extract components and separate setups from logic
            for (int i = 0; i < cleanLines.Count; i++)
            {
                string line = cleanLines[i];

                var compMatch = Regex.Match(line, @"^Component\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(([^)]*)\)\s*\(([^)]*)\)\s*\{");
                if (compMatch.Success)
                {
                    var def = new ComponentDef { Name = compMatch.Groups[1].Value };
                    
                    foreach (var inp in compMatch.Groups[2].Value.Split(',')) if (!string.IsNullOrWhiteSpace(inp)) def.Inputs.Add(inp.Trim());
                    foreach (var outp in compMatch.Groups[3].Value.Split(',')) if (!string.IsNullOrWhiteSpace(outp)) def.Outputs.Add(outp.Trim());

                    i++; // Move past header
                    while (i < cleanLines.Count && cleanLines[i].Trim() != "}")
                    {
                        def.BodyLines.Add(cleanLines[i].Trim());
                        i++;
                    }
                    _components[def.Name] = def;
                    continue;
                }

                var arrayDefMatch = Regex.Match(line, @"^(INPUTS|OUTPUTS|NODES)\s*\(\s*(\d+)\s*\)\s*=\s*([0-9a-fA-FxXbB]+)$", RegexOptions.IgnoreCase);
                if (arrayDefMatch.Success)
                {
                    setupLines.Add(line);
                    continue;
                }

                var entryDefMatch = Regex.Match(line, @"\bENTRY\b", RegexOptions.IgnoreCase);
                if (entryDefMatch.Success)
                {
                    containsEntryPoint = true;
                }

                var aliasMatch = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*((?:INPUTS|OUTPUTS|NODES)\[\s*\d+\s*\])", RegexOptions.IgnoreCase);
                if (aliasMatch.Success)
                {
                    string varName = aliasMatch.Groups[1].Value;
                    string arrayTarget = aliasMatch.Groups[2].Value;
                    _symbolTable[varName] = GetOrCreateID(arrayTarget);
                    continue; 
                }

                logicLines.Add(line);
            }

            // Pass 2: Initialisation configurations (sequential header generation)
            foreach (var line in setupLines)
            {
                var arrayMatch = Regex.Match(line, @"^(INPUTS|OUTPUTS|NODES)\s*\(\s*(\d+)\s*\)\s*=\s*([0-9a-fA-FxXbB]+)", RegexOptions.IgnoreCase);
                if (arrayMatch.Success)
                {
                    string arrayName = arrayMatch.Groups[1].Value.ToUpper();
                    int size = int.Parse(arrayMatch.Groups[2].Value);
                    
                    ulong initVal = 0;
                    TryParseNumericLiteral(arrayMatch.Groups[3].Value, out initVal, out _);
                    
                    // Maps directly to simulator engine opcodes
                    ulong opcode = arrayName switch {
                        "INPUTS"  => 1UL,
                        "OUTPUTS" => 2UL,
                        _         => 3UL //  TYPES.NODE Opcode 2
                    };

                    for (int j = 0; j < size; j++)
                    {
                        ulong varId = GetOrCreateID($"{arrayName}[{j}]");
                        _bytecode.AddRange(new[] { opcode, varId, initVal });
                    }
                }
            }

            if (containsEntryPoint == false) _bytecode.Add(0UL);

            // Pass 3: Process Execution and Operations logic
            foreach (var line in logicLines)
            {
                ParseInstruction(line, new Dictionary<string, string>());
            }

            return _bytecode.ToArray();
        }

        private void ParseInstruction(string line, Dictionary<string, string> macroScope)
        {
            string leftHand = "";
            string rightHand = line;

            // 1. Handle traditional Python-style splits: out0, out1 = Function(args)
            if (line.Contains("="))
            {
                string[] parts = line.Split('=');
                leftHand = parts[0].Trim();
                rightHand = parts[1].Trim();
            }

            // 2. Intercept direct runtime value changes FIRST (e.g., INPUTS[0] = 25)
            if (!string.IsNullOrEmpty(leftHand) && !rightHand.Contains("("))
            {
                string resolvedLeft = ResolveMacroSymbol(leftHand, macroScope);
                string resolvedRight = ResolveMacroSymbol(rightHand, macroScope);

                ulong targetId = GetOrCreateID(resolvedLeft);

                if (TryParseNumericLiteral(resolvedRight, out ulong literalValue, out _))
                {
                    _bytecode.AddRange(new[] { 3UL, targetId, literalValue });
                    return;
                }
                else
                {
                    ulong sourceId = GetOrCreateID(resolvedRight);
                    _bytecode.AddRange(new[] { 4UL, _nextId++, targetId, sourceId });
                    return;
                }
            }

            // 3. Robustly handle hardware-style trailing outputs: Function(...) (out0, out1)
            List<string> leftHandTargets = new List<string>();
            
            // This regex ensures it matches trailing parenthesis outputs even if they contain brackets []
            var hardwareOutputsMatch = Regex.Match(rightHand, @"^([a-zA-Z_][a-zA-Z0-9_]*\s*\(.*\))\s*\(([^)]+)\)$");
            if (hardwareOutputsMatch.Success)
            {
                rightHand = hardwareOutputsMatch.Groups[1].Value.Trim(); // Strip the trailing outputs group
                string rawOutputs = hardwareOutputsMatch.Groups[2].Value; // "OUTPUTS[0], OUTPUTS[1], ..."
                
                // Clean split by commas that preserves array bracket integrity
                var matches = Regex.Matches(rawOutputs, @"([^,\[\]]+(?:\[\d+\])?)");
                foreach (Match match in matches)
                {
                    leftHandTargets.Add(ResolveMacroSymbol(match.Value.Trim(), macroScope));
                }
            }
            else if (!string.IsNullOrEmpty(leftHand))
            {
                // Fallback: Populate from traditional left-hand side assignment if present
                foreach (var target in leftHand.Split(',')) 
                {
                    leftHandTargets.Add(ResolveMacroSymbol(target.Trim(), macroScope));
                }
            }

            // 4. Function call signature parsing
            var funcMatch = Regex.Match(rightHand, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.*)\)$");
            if (!funcMatch.Success) return;

            string funcName = funcMatch.Groups[1].Value;
            
            // Clean split for function arguments to keep brackets [] intact
            List<string> parsedArgs = new List<string>();
            var argMatches = Regex.Matches(funcMatch.Groups[2].Value, @"([^,\[\]]+(?:\[\d+\])?)");
            foreach (Match match in argMatches)
            {
                parsedArgs.Add(ResolveMacroSymbol(match.Value.Trim(), macroScope));
            }
            string[] args = parsedArgs.ToArray();

            // 5. Macro expansion processing
            if (_components.TryGetValue(funcName, out var compDef))
            {
                var instanceScope = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Bind inputs
                for (int i = 0; i < compDef.Inputs.Count && i < args.Length; i++)
                {
                    instanceScope[compDef.Inputs[i]] = args[i];
                }

                // Bind outputs (Unified from both notation layouts seamlessly!)
                if (leftHandTargets.Count > 0)
                {
                    for (int i = 0; i < compDef.Outputs.Count && i < leftHandTargets.Count; i++)
                    {
                        instanceScope[compDef.Outputs[i]] = leftHandTargets[i];
                    }
                }
                else 
                {
                    int argOffset = compDef.Inputs.Count;
                    for (int i = 0; i < compDef.Outputs.Count && (argOffset + i) < args.Length; i++)
                    {
                        instanceScope[compDef.Outputs[i]] = args[argOffset + i];
                    }
                }

                // Ensure any internal unmapped component parameters generate a unique local space namespace instance tag
                string uniquePrefix = $"_m{_nextId++}_";
                foreach (var innerOutput in compDef.Outputs)
                {
                    if (!instanceScope.ContainsKey(innerOutput))
                    {
                        instanceScope[innerOutput] = uniquePrefix + innerOutput;
                    }
                }

                foreach (var bodyLine in compDef.BodyLines)
                {
                    ParseInstruction(bodyLine, instanceScope);
                }
                return;
            }

            // 6. Core Instructions
            string upperFunc = funcName.ToUpper();

            // Find entry point of program
            if (upperFunc == "ENTRY") _bytecode.Add(0UL);
            
            // Complete Logic Gates Processing Suite (Opcodes 2 to 8)
            else if (upperFunc is "NOT" or "AND" or "NAND" or "OR" or "NOR" or "XOR" or "XNOR")
            {
                ulong opcode = upperFunc switch {
                    "NOT"  => 5UL, "AND" => 6UL, "NAND" => 7UL,
                    "OR"   => 8UL, "NOR" => 9UL, "XOR"  => 10UL,
                    _      => 11UL
                };

                if (args.Length < 2) return; 

                ulong bitWidth = ulong.Parse(args[0]);
                ulong aId = GetOrCreateID(args[1]);
                ulong outId = leftHandTargets.Count > 0 ? GetOrCreateID(leftHandTargets[0]) : (args.Length > 2 ? GetOrCreateID(args[2]) : 0UL);

                if (upperFunc == "NOT")
                {
                    _bytecode.AddRange(new[] { opcode, _nextId++, bitWidth, aId, outId });
                }
                else 
                {
                    if (args.Length < 3) return; 
                    ulong bId = GetOrCreateID(args[2]);
                    outId = leftHandTargets.Count > 0 ? GetOrCreateID(leftHandTargets[0]) : (args.Length > 3 ? GetOrCreateID(args[3]) : 0UL);
                    _bytecode.AddRange(new[] { opcode, _nextId++, bitWidth, aId, bId, outId });
                }
            }
            else if (upperFunc == "ADDER" || upperFunc == "SUBTRACTOR")
            {
                if (args.Length < 3) return; 

                ulong opcode = (upperFunc == "ADDER") ? 12UL : 13UL;
                ulong bitWidth = ulong.Parse(args[0]);
                ulong aId = GetOrCreateID(args[1]);
                ulong bId = GetOrCreateID(args[2]);
                ulong outId, flagId;

                if (leftHandTargets.Count > 0)
                {
                    outId = GetOrCreateID(leftHandTargets[0]);
                    flagId = leftHandTargets.Count > 1 ? GetOrCreateID(leftHandTargets[1]) : GetOrCreateID($"_flag_{_nextId}");
                }
                else
                {
                    outId = args.Length > 3 ? GetOrCreateID(args[3]) : 0UL;
                    flagId = args.Length > 4 ? GetOrCreateID(args[4]) : 0UL;
                }

                _bytecode.AddRange(new[] { opcode, _nextId++, bitWidth, aId, bId, outId, flagId });
            }
            else if (upperFunc == "BITSHIFT")
            {
                if (args.Length < 4) return; 

                ulong opcode = 14UL;
                ulong bitWidth = ulong.Parse(args[0]);
                ulong direction = args[1].Equals("LEFT", StringComparison.OrdinalIgnoreCase) ? 0UL : 
                                args[1].Equals("RIGHT", StringComparison.OrdinalIgnoreCase) ? 1UL : GetOrCreateID(args[1]);
                ulong aId = GetOrCreateID(args[2]);
                ulong bId = GetOrCreateID(args[3]);
                ulong outId, flagId;

                if (leftHandTargets.Count > 0)
                {
                    outId = GetOrCreateID(leftHandTargets[0]);
                    flagId = leftHandTargets.Count > 1 ? GetOrCreateID(leftHandTargets[1]) : GetOrCreateID($"_flag_{_nextId}");
                }
                else
                {
                    outId = args.Length > 4 ? GetOrCreateID(args[4]) : 0UL;
                    flagId = args.Length > 5 ? GetOrCreateID(args[5]) : 0UL;
                }

                _bytecode.AddRange(new[] { opcode, _nextId++, bitWidth, direction, aId, bId, outId, flagId });
            }
            else if (upperFunc == "MULTIPLEXER")
            {
                if (args.Length < 4) return; 

                ulong opcode = 15UL;
                ulong bitWidth = ulong.Parse(args[0]);
                ulong select = GetOrCreateID(args[1]);
                ulong aId = GetOrCreateID(args[2]);
                ulong bId = GetOrCreateID(args[3]);
                ulong outId;

                if (leftHandTargets.Count > 0)
                {
                    outId = GetOrCreateID(leftHandTargets[0]);
                }
                else
                {
                    outId = args.Length > 4 ? GetOrCreateID(args[4]) : 0UL;
                }

                _bytecode.AddRange(new[] { opcode, _nextId++, bitWidth, select, aId, bId, outId });
            }
        }

        private string ResolveMacroSymbol(string symbol, Dictionary<string, string> scope)
        {
            if (scope.TryGetValue(symbol, out var outerSymbol)) return outerSymbol;
            return symbol;
        }

        public static void SaveBin(string filePath, ulong[] bytecode)
        {
            ReadOnlySpan<byte> byteSpan = MemoryMarshal.Cast<ulong, byte>(bytecode);
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.Write(byteSpan);
        }
    }