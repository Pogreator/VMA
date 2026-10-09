using System;
using System.IO;
using System.Threading;

class Program
{
    private static LogicSim simulator = new LogicSim();

    private static bool isSimulationRunning = true;
    private static readonly object pipelineLock = new object();

    static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
        {
            PrintUsageGuide();
            return;
        }

        string inputFilePath = null;
        string outputBinaryPath = null;
        bool constantLoop = false;
        bool singleTestLoop = false;
        bool consoleControl = false;
        bool isInputBin = false;
        bool validation = true;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length) inputFilePath = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length) outputBinaryPath = args[++i];
                    break;
                case "-r":
                case "--run":
                    constantLoop = true;
                    break;
                case "-t":
                case "--test":
                    singleTestLoop = true;
                    break;
                case "-c":
                case "--console":
                    consoleControl = true;
                    break;
                case "--ignore-errors":
                    validation = false;
                    break;
                case "--version":
                    Console.WriteLine("VMA 0.1.4");
                    return;
                default:
                    if (File.Exists(args[i]) && inputFilePath == null) inputFilePath = args[i];
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Unknown argument '{args[i]}' ignored.");
                        Console.ResetColor();
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(inputFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Missing required input file path (-i / --input <filename.vma / filename.bin>).");
            Console.ResetColor();
            PrintUsageGuide();
            return;
        }

        if (!File.Exists(inputFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: The source file '{inputFilePath}' does not exist.");
            Console.ResetColor();
            return;
        }

        isInputBin = Path.GetExtension(inputFilePath).Equals(".bin", StringComparison.OrdinalIgnoreCase);

        if (!constantLoop && !singleTestLoop)
        {
            singleTestLoop = true; 
        }

        try
        {
            ulong[] bytecodeArray;
            if (!isInputBin)
            {
                Console.WriteLine($"> Reading source script: {inputFilePath}...");
                string sourceCode = File.ReadAllText(inputFilePath);

                Console.WriteLine("> Compiling layout to 64-bit virtual machine bytecode...");
                VMALang compiler = new VMALang();
                compiler._validation = validation;
                bytecodeArray = compiler.Compile(sourceCode);

                Console.WriteLine("------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compilation Successful!");
                Console.ResetColor();
                Console.WriteLine($"Total 64-bit Words Generated: {bytecodeArray.Length}");
                Console.WriteLine("------------------------------------------------");
            }
            else
            {
                Console.WriteLine($"> Reading input bytecode: {inputFilePath}...");
                bytecodeArray = LogicSim.ReadBin(inputFilePath);
                Console.WriteLine("------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Loading Successful!");
                Console.ResetColor();
                Console.WriteLine($"Total 64-bit Words Generated: {bytecodeArray.Length}");
                Console.WriteLine("------------------------------------------------");
            }

            if (!string.IsNullOrEmpty(outputBinaryPath))
            {
                Console.WriteLine($" Packaging system data to target: {outputBinaryPath}...");
                VMALang.SaveBin(outputBinaryPath, bytecodeArray);
            }
            else if (!isInputBin)
            {
                Console.WriteLine(" No output path specified; skipping binary file generation.");
            }

            Console.WriteLine("> Initializing custom LogicSim Engine loop environment...");
            simulator.ByteCode = bytecodeArray;

            if (singleTestLoop)
            {
                Console.WriteLine("Running simulation: Single Test Execution Pass (--test)...");
                simulator.ExecuteByteCode();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[✓] Simulation Run Complete. Input and Output dump:");
                Console.ResetColor();
                PrintRegisterDump(simulator);
            }
            else if (constantLoop)
            {
                Console.WriteLine("Running simulation: Constant Evaluation Loop Mode (--run)...");
                Console.WriteLine("Press Ctrl+C to abort");
                Console.WriteLine("------------------------------------------------");
                if (consoleControl)
                {
                    Console.WriteLine("Terminal Control Interfacing Active.");
                    Console.WriteLine("Commands: 'dump' (read state), 'set <id> <val>' (write input), 'exit'");
                    Console.WriteLine("------------------------------------------------");

                    _ = Task.Run(() => RunSimulationPipeline(consoleControl));

                    while (isSimulationRunning)
                    {
                        Console.Write("\n> ");
                        string input = await Console.In.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(input)) continue;

                        string[] tokens = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string command = tokens[0].ToLower();

                        switch (command)
                        {
                            case "dump":
                                lock (pipelineLock)
                                {
                                    PrintRegisterDump(simulator);
                                }
                                break;

                            case "set":
                                if (tokens.Length >= 3 && int.TryParse(tokens[1], out int id) && ulong.TryParse(tokens[2], out ulong val))
                                {
                                    lock (pipelineLock)
                                    {
                                        if (simulator.IOStates.ContainsKey((ulong)id))
                                        {
                                            simulator.IOStates[(ulong)id] = val;
                                            Console.WriteLine($"[Control] Successfully mutated Address ID [{(ulong)id}] to State: {simulator.IOStates[(ulong)id]}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[Control Error] Address ID [{(ulong)id}] not registered in current simulation space.");
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("[Control Error] Invalid syntax. Use: set <address_id> <value>");
                                }
                                break;

                            case "exit":
                            case "quit":
                                isSimulationRunning = false;
                                Environment.Exit(0);
                                break;

                            default:
                                Console.WriteLine("[Control Error] Command not recognized. Use 'dump', 'set', or 'exit'.");
                                break;
                        }
                    }
                }
                else
                {
                    RunSimulationPipeline(consoleControl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[!] Compiler Fatal Exception triggered during parse pipeline:\n{ex.Message}");
            Console.ResetColor();
        }
    }

    private static void RunSimulationPipeline(bool showPrompt)
    {
        ulong totalCycles = 0;
        while (true)
        {
            simulator.ExecuteByteCode();
            totalCycles++;
            Thread.Sleep(1);
        }
    }

    private static void PrintRegisterDump(LogicSim simulator)
    {
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine($"> Entry Byte: {simulator.entryByte}");
        foreach (var id in simulator.InputId)
        {
            var state = simulator.IOStates[id];
            Console.WriteLine($">  Input Address ID: [{id}] -> Decoded State Value: {state} | {(long)state} | 0x{state:X}");
        }
        foreach (var id in simulator.OutputId)
        {
            var state = simulator.IOStates[id];
            Console.WriteLine($">  Ouptut Address ID: [{id}] -> Decoded State Value: {state} | {(long)state} | 0x{state:X}");
        }
        Console.WriteLine("------------------------------------------------");
    }

    private static void PrintUsageGuide()
    {
        string exeName = AppDomain.CurrentDomain.FriendlyName;
        Console.WriteLine($"usage: {exeName} [OPTIONS] [file.vma]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("-i, --input <file>  : specify the input source file containing VMA code");
        Console.WriteLine("-o, --output <file> : write compiled binary to the specified file");
        Console.WriteLine("                      if omitted, no binary file is written");
        Console.WriteLine("-t, --test          : run the simulation exactly once (default)");
        Console.WriteLine("-r, --run           : run the simulation continuously until interrupted");
        Console.WriteLine("-c, --console       : allows control of the simulation through console");
        Console.WriteLine("-h, --help          : display this help message and exit");
        Console.WriteLine("--version           : get current program version");
        Console.WriteLine("--ignore-errors     : compile without validation");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("file.vma            : optional path to the VMA source file");
        Console.WriteLine("                      may be specified directly or with -i / --input");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"  {exeName} program.vma");
        Console.WriteLine($"  {exeName} -i program.vma");
        Console.WriteLine($"  {exeName} program.vma -o program.bin");
        Console.WriteLine($"  {exeName} -i program.vma -t");
        Console.WriteLine($"  {exeName} program.vma -r -c");
    }
}