using System;
using System.IO;
using System.Threading;

class Program
{
    static void Main(string[] args)
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
        bool isInputBin = false;

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
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Unknown argument flag '{args[i]}' ignored.");
                    Console.ResetColor();
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
            LogicSim simulator = new LogicSim();
            simulator.ByteCode = bytecodeArray;

            if (singleTestLoop)
            {
                Console.WriteLine("Running simulation: Single Test Execution Pass (--test)...");
                simulator.ExecuteByteCode();
                PrintRegisterDump(simulator);
            }
            else if (constantLoop)
            {
                Console.WriteLine("Running simulation: Constant Evaluation Loop Mode (--run)...");
                Console.WriteLine("Press Ctrl+C to abort the hardware execution pipeline manually.");
                Console.WriteLine("------------------------------------------------");
                
                ulong totalCycles = 0;
                while (true)
                {
                    simulator.ExecuteByteCode();
                    totalCycles++;
                    
                    if (totalCycles % 10000 == 0)
                    {
                        Console.WriteLine($"[Heartbeat] Cycle tick iteration count: {totalCycles}");
                    }
                    
                    Thread.Sleep(1);
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

    private static void PrintRegisterDump(LogicSim simulator)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[✓] Simulation Run Complete. Finalized Finished Register Dump:");
        Console.ResetColor();
        Console.WriteLine("------------------------------------------------");
        foreach (var id in simulator.InputId)
        {
            var state = simulator.IOStates[id];
            Console.WriteLine($">  Input Address ID: [{id}] -> Decoded State Value: {state} (0x{state:X})");
        }
        foreach (var id in simulator.OutputId)
        {
            var state = simulator.IOStates[id];
            Console.WriteLine($">  Ouptut Address ID: [{id}] -> Decoded State Value: {state} (0x{state:X})");
        }
        Console.WriteLine("------------------------------------------------");
    }

    private static void PrintUsageGuide()
    {
        string exeName = AppDomain.CurrentDomain.FriendlyName;
        Console.WriteLine("=====================================================================");
        Console.WriteLine("                  VMA Bytecode Compiler & Simulator CLI              ");
        Console.WriteLine("=====================================================================");
        Console.WriteLine("Usage Instructions:");
        Console.WriteLine($"  dotnet run -- --input <file.vma> [--output <file.bin>] [--test | --run]");
        Console.WriteLine($"  OR: ./{exeName} -i <file.vma> [-o <file.bin>] [-t | -r]\n");
        Console.WriteLine("Argument Flags:");
        Console.WriteLine("  -i, --input <file>   (Required) Path to human-readable text code file.");
        Console.WriteLine("  -o, --output <file>  (Optional) Target destination for output binary layout.");
        Console.WriteLine("                       If omitted, no binary payload file is written out.");
        Console.WriteLine("  -t, --test           (Optional) Run simulation exactly once (Default).");
        Console.WriteLine("  -r, --run            (Optional) Constant loop evaluation script indefinitely.");
        Console.WriteLine("=====================================================================");
    }
}