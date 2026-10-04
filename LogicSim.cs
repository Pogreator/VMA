using System.Runtime.InteropServices;

public class LogicSim
{
	public ulong[] ByteCode { get; set; } = Array.Empty<ulong>();
	public ulong ExecutionPosition { get; set; } = 0;

	public List<ulong> InputId { get; set; } = new List<ulong>();
	public List<ulong> OutputId { get; set; } = new List<ulong>();
	public Dictionary<ulong, ulong> IOStates { get; set; } = new Dictionary<ulong, ulong>();

	public enum TYPES
	{
		INPUT, // 0 = id | 1 = init input number
		OUTPUT, // 0 = id | 1 = init output number
		NOT, // 0 = id | 1 = bit_width | 2 = a | 3 = output
		AND, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		NAND, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		OR, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		NOR, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		XOR, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		XNOR, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = output
		ADDER, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = outputId | 5 = carryOutputID
		SUBTRACTOR, // 0 = id | 1 = bit_width | 2 = a | 3 = b | 4 = outputId | 5 = carryOutputID
        BITSHIFT, // 0 - id | 1 = bit_width | 2 = direction (left = 0, right = > 0) | 3 = a | 4 = b | 5 = outputId | 6 = over/under flow output
		MULTIPLEXER, // 0 - id | 1 = bit_width | 2 = sel | 3 = a | 4 = b | 5 = output
	}

	public static ulong[] ReadBin(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        
        long fileLength = fs.Length;
        int elementCount = (int)(fileLength / 8); 
        
        ulong[] result = new ulong[elementCount];
        Span<byte> byteSpan = MemoryMarshal.Cast<ulong, byte>(result);
        
        fs.ReadExactly(byteSpan);
        
        return result;
    }

	public ulong BitWidthToMask(int bit_width)
	{
		return (bit_width >= 64) ? ulong.MaxValue : (1UL << bit_width) - 1;
	}
	
	public void DecodeType(TYPES Type)
	{
		switch (Type)
		{
			case TYPES.INPUT:
				{
					ulong id = ByteCode[ExecutionPosition];
					ulong value = ByteCode[ExecutionPosition+1];

					if (!InputId.Contains(id)) 
					{
						InputId.Add(id);
					}
					IOStates[id] = value;
					ExecutionPosition+=2;
					break;
				}
			
			case TYPES.OUTPUT:
				{
					ulong id = ByteCode[ExecutionPosition];
					ulong value = ByteCode[ExecutionPosition+1];

					if (!OutputId.Contains(id)) 
					{
						OutputId.Add(id);
					}
					IOStates[id] = value;
					ExecutionPosition+=2;
					break;
				}

			case TYPES.NOT:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong output = ByteCode[ExecutionPosition+3];

					IOStates[output] = ~a & mask;

					ExecutionPosition+=4;
					break;
				}
			
			case TYPES.AND:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = a & b & mask;

					ExecutionPosition+=5;
					break;
				}
			
			case TYPES.NAND:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = ~(a & b) & mask;

					ExecutionPosition+=5;
					break;
				}
			
			case TYPES.OR:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = (a | b) & mask;
					
					ExecutionPosition+=5;
					break;
				}
			
			case TYPES.NOR:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = ~(a | b) & mask;

					ExecutionPosition+=5;
					break;
				}
			
			case TYPES.XOR:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = (a ^ b) & mask;

					ExecutionPosition+=5;
					break;
				}
			
			case TYPES.XNOR:
				{
					ulong mask = BitWidthToMask((int)ByteCode[ExecutionPosition+1]);
					ulong a = IOStates[ByteCode[ExecutionPosition+2]];
					ulong b = IOStates[ByteCode[ExecutionPosition+3]];
					ulong output = ByteCode[ExecutionPosition+4];

					IOStates[output] = ~(a ^ b) & mask;

					ExecutionPosition+=5;
					break;
				}

			case TYPES.ADDER:
				{
					int bit_width = (int)ByteCode[ExecutionPosition + 1];
					ulong mask = BitWidthToMask(bit_width);
					
					ulong a = IOStates[ByteCode[ExecutionPosition + 2]] & mask;
					ulong b = IOStates[ByteCode[ExecutionPosition + 3]] & mask;

					ulong rawSum = a + b;
					ulong maskSum = rawSum & mask;

					bool hasOverflow = (bit_width >= 64) ? rawSum < a : (rawSum & (mask+1)) > 0;
					ulong carry = hasOverflow ? 1UL : 0UL;

					IOStates[ByteCode[ExecutionPosition+4]] = maskSum;
					IOStates[ByteCode[ExecutionPosition+5]] = carry;

					ExecutionPosition+=6;
					break;
				}

			case TYPES.SUBTRACTOR:
				{
					int bit_width = (int)ByteCode[ExecutionPosition + 1];
					ulong mask = BitWidthToMask(bit_width);
					
					ulong a = IOStates[ByteCode[ExecutionPosition + 2]] & mask;
					ulong b = IOStates[ByteCode[ExecutionPosition + 3]] & mask;

					ulong negativeB = ((b ^ mask) + 1) & mask;

					ulong rawDiff = a + negativeB;
					ulong maskDiff = rawDiff & mask;

					ulong borrow = (a < b) ? 1UL : 0UL;

					IOStates[ByteCode[ExecutionPosition+4]] = maskDiff;
					IOStates[ByteCode[ExecutionPosition+5]] = borrow;

					ExecutionPosition+=6;
					break;
				}
            
            case TYPES.BITSHIFT:
                {
                    int bit_width = (int)ByteCode[ExecutionPosition + 1];
					ulong mask = BitWidthToMask(bit_width);
                    
                    int direction = (IOStates[ByteCode[ExecutionPosition + 2]] > 0) ? 0 : 1;
                    ulong a = IOStates[ByteCode[ExecutionPosition + 3]] & mask;
                    int b = (int)IOStates[ByteCode[ExecutionPosition + 4]];

                    ulong shift = 0;
                    ulong flag = 0;


                    if (b == 0)
                    {
                        shift = a;
                        flag = 0;
                    }
                    else if (direction == 0)
                    {
                        shift = (a << b) & mask;
                        if (((shift >> b) & mask) != a)
                        {
                            flag = 1;
                        }
                    }
                    else
                    {
                        shift = a >> b;
                        ulong underflowMask = (1UL << b) - 1UL;
                        if ((a & underflowMask) != 0)
                        {
                            flag = 1;
                        }
                    }

                    IOStates[ByteCode[ExecutionPosition+5]] = shift;
					IOStates[ByteCode[ExecutionPosition+6]] = flag;
                    ExecutionPosition+=7;
                    break;
                }
			
			case TYPES.MULTIPLEXER:
                {
                    int bit_width = (int)ByteCode[ExecutionPosition + 1];
                    ulong mask = BitWidthToMask(bit_width);
                    
                    ulong select = IOStates[ByteCode[ExecutionPosition + 2]];
                    ulong a      = IOStates[ByteCode[ExecutionPosition + 3]] & mask;
                    ulong b      = IOStates[ByteCode[ExecutionPosition + 4]] & mask;
                    
                    ulong outputValue = (select > 0) ? b : a;

                    IOStates[ByteCode[ExecutionPosition + 5]] = outputValue;
                    
                    ExecutionPosition += 6;
                    break;
                }
		}
	}

	public void StepByteCode()
	{
		ulong Type = ByteCode[ExecutionPosition];
		ExecutionPosition++;
		DecodeType((TYPES)Type);
	}

	public void ExecuteByteCode()
	{
		ExecutionPosition = 0;
		while (ExecutionPosition < (ulong)ByteCode.Length)
		{
		   StepByteCode(); 
		}
	}
}
