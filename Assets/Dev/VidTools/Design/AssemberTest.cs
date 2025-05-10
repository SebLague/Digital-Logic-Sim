using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DLS.Dev
{
	public class AssemberTest : MonoBehaviour
	{
		public bool printWithComments;

		void Start()
		{
			Instruction[] instructions = CreateProgram_CA();
			PrintInstructionMemory(instructions);

			UInt16[] controlUnitRom = ControlUnitROM();
			PrintControlUnitMemory(controlUnitRom);
		}

		UInt16[] ControlUnitROM()
		{
			// Control unit memory: 256 x uint16 (3bit aluOp, 3bit jumpOp, 3bit outputEnable, 3bit inputEnable, 4bits for other control flags)
			// (control unit is addressed by opcode from instruction memory)
			UInt16[] controlUnitROM = new UInt16[256];
			HashSet<OpCode> usedOpCodes = new();
			const AluOp AluDontCare = AluOp.Add;

			// ALU operations
			Set(OpCode.Add, Make(AluOp.Add, JumpOp.None, OutputEnable.Alu, InputEnable.RegA));
			Set(OpCode.AddConst, Make(AluOp.Add, JumpOp.None, OutputEnable.Alu, InputEnable.RegA, OtherControlFlags.AluUseConst));
			Set(OpCode.Subtract, Make(AluOp.Subtract, JumpOp.None, OutputEnable.Alu, InputEnable.RegA));
			Set(OpCode.SubtractConst, Make(AluOp.Subtract, JumpOp.None, OutputEnable.Alu, InputEnable.RegA, OtherControlFlags.AluUseConst));
			Set(OpCode.Compare, Make(AluOp.Subtract, JumpOp.None, OutputEnable.Alu, InputEnable.None));
			Set(OpCode.CompareConst, Make(AluOp.Subtract, JumpOp.None, OutputEnable.Alu, InputEnable.None, OtherControlFlags.AluUseConst));
			Set(OpCode.BitwiseAnd, Make(AluOp.And, JumpOp.None, OutputEnable.Alu, InputEnable.RegA));
			Set(OpCode.BitwiseAndConst, Make(AluOp.And, JumpOp.None, OutputEnable.Alu, InputEnable.RegA, OtherControlFlags.AluUseConst));
			Set(OpCode.BitwiseOr, Make(AluOp.Or, JumpOp.None, OutputEnable.Alu, InputEnable.RegA));
			Set(OpCode.BitwiseOrConst, Make(AluOp.Or, JumpOp.None, OutputEnable.Alu, InputEnable.RegA, OtherControlFlags.AluUseConst));
			Set(OpCode.LeftShift, Make(AluOp.LeftShift, JumpOp.None, OutputEnable.Alu, InputEnable.RegA));

			// Jumps
			Set(OpCode.Jump, Make(AluDontCare, JumpOp.Jump, OutputEnable.None, InputEnable.None));
			Set(OpCode.JumpIfZero, Make(AluDontCare, JumpOp.JumpZero, OutputEnable.None, InputEnable.None));
			Set(OpCode.JumpIfNotZero, Make(AluDontCare, JumpOp.JumpNotZero, OutputEnable.None, InputEnable.None));

			// Read/Write
			Set(OpCode.ARegSetConstant, Make(AluDontCare, JumpOp.None, OutputEnable.Const, InputEnable.RegA));
			Set(OpCode.RamToA, Make(AluDontCare, JumpOp.None, OutputEnable.Ram, InputEnable.RegA));
			Set(OpCode.BToA, Make(AluDontCare, JumpOp.None, OutputEnable.RegB, InputEnable.RegA));
			Set(OpCode.ConstToB, Make(AluDontCare, JumpOp.None, OutputEnable.Const, InputEnable.RegB));
			Set(OpCode.RamToB, Make(AluDontCare, JumpOp.None, OutputEnable.Ram, InputEnable.RegB));
			Set(OpCode.AToB, Make(AluDontCare, JumpOp.None, OutputEnable.RegA, InputEnable.RegB));
			Set(OpCode.AToRamAddress, Make(AluDontCare, JumpOp.None, OutputEnable.RegA, InputEnable.RamAddress));
			Set(OpCode.ConstToRamAddress, Make(AluDontCare, JumpOp.None, OutputEnable.Const, InputEnable.RamAddress));
			Set(OpCode.ConstToRam, Make(AluDontCare, JumpOp.None, OutputEnable.Const, InputEnable.Ram));
			Set(OpCode.AToRam, Make(AluDontCare, JumpOp.None, OutputEnable.RegA, InputEnable.Ram));
			Set(OpCode.BToRam, Make(AluDontCare, JumpOp.None, OutputEnable.RegB, InputEnable.Ram));
			Set(OpCode.AToDisplayReg, Make(AluDontCare, JumpOp.None, OutputEnable.RegA, InputEnable.DisplayReg));
			Set(OpCode.BToDisplayReg, Make(AluDontCare, JumpOp.None, OutputEnable.RegB, InputEnable.DisplayReg));
			Set(OpCode.DisplayWrite, Make(AluDontCare, JumpOp.None, OutputEnable.None, InputEnable.Display));

			// Other
			Set(OpCode.Halt, Make(AluDontCare, JumpOp.None, OutputEnable.None, InputEnable.None, OtherControlFlags.Halt));


			LogUnusedOpCodes();
			return controlUnitROM;

			void Set(OpCode op, UInt16 control)
			{
				controlUnitROM[(int)op] = control;
				usedOpCodes.Add(op);
			}


			UInt16 Make(AluOp aluOp, JumpOp jumpOp, OutputEnable outEnable, InputEnable inEnable, params OtherControlFlags[] otherFlags)
			{
				int otherFlagsInt = 0;
				foreach (OtherControlFlags flag in otherFlags)
				{
					otherFlagsInt |= 1 << (int)flag;
				}

				if ((int)aluOp >= 8 || (int)inEnable >= 8 || (int)outEnable >= 8 || (int)jumpOp >= 8 || otherFlagsInt >= 16) throw new Exception("Out of range");

				int packed = (int)aluOp | ((int)jumpOp << 3) | ((int)outEnable << 6) | ((int)inEnable << 9) | (otherFlagsInt << 12);
				return (UInt16)packed;
			}

			void LogUnusedOpCodes()
			{
				foreach (OpCode op in Enum.GetValues(typeof(OpCode)))
				{
					if (usedOpCodes.Contains(op)) continue;
					Debug.Log("Op code has no control entry: " + op);
				}
			}
		}

		Instruction[] CreateProgram_CA()
		{
			List<Instruction> instructions = new();

			const int displayWidth = 16;
			const int ruleStartAddr = displayWidth * 2;
			const int addrX = ruleStartAddr + 8;
			const int addrY = addrX + 1;

			// Init state
			string inputString = "1110011000111110";
			for (int i = 0; i < displayWidth; i++)
			{
				Do(OpCode.ConstToRamAddress, i);
				int v = inputString[i] == '0' ? 0 : 1;
				Do(OpCode.ConstToRam, v, $"Ram init test state {i} = {v}");
			}

			// Set rule
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 0);
			Do(OpCode.ConstToRam, 0, "Ram Store: rule 0 = 0");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 1);
			Do(OpCode.ConstToRam, 0, "Ram Store: rule 1 = 0");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 2);
			Do(OpCode.ConstToRam, 0, "Ram Store: rule 2 = 0");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 3);
			Do(OpCode.ConstToRam, 1, "Ram Store: rule 3 = 1");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 4);
			Do(OpCode.ConstToRam, 1, "Store rule 4 = 1");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 5);
			Do(OpCode.ConstToRam, 1, "Ram Store: rule 5 = 1");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 6);
			Do(OpCode.ConstToRam, 1, "Ram Store: rule 6 = 1");
			Do(OpCode.ConstToRamAddress, ruleStartAddr + 7);
			Do(OpCode.ConstToRam, 0, "Ram Store: rule 7 = 0");

			// Init x, y = 0
			Do(OpCode.ConstToRamAddress, addrX);
			Do(OpCode.ConstToRam, 0, "Ram Store: x = 0");
			Do(OpCode.ConstToRamAddress, addrY);
			Do(OpCode.ConstToRam, 0, "Ram Store: y = 0");

			// start loop x
			int mainLoopStart = instructions.Count;

			// state x
			Do(OpCode.ConstToRamAddress, addrX);
			Do(OpCode.RamToA, comment: "A = x");
			Do(OpCode.AToRamAddress);
			Do(OpCode.RamToB, comment: "B holds: state at x");

			// state x - 1
			Do(OpCode.SubtractConst, 1); // x-1
			Do(OpCode.BitwiseAndConst, 0b1111);
			Do(OpCode.AToRamAddress);
			Do(OpCode.RamToA, comment: "A holds state at x-1");
			Do(OpCode.LeftShift, comment: "LSHIFT");
			Do(OpCode.BitwiseOr, comment: "OR");
			Do(OpCode.LeftShift, comment: "LSHIFT");
			Do(OpCode.AToB, comment: "B holds: (stateX-1) << 2 | (stateX) << 1");

			// state x + 1
			Do(OpCode.ConstToRamAddress, addrX);
			Do(OpCode.RamToA);
			Do(OpCode.AddConst, 1);
			Do(OpCode.BitwiseAndConst, 0b1111);
			Do(OpCode.AToRamAddress);
			Do(OpCode.RamToA, comment: "A holds state at x+1");
			Do(OpCode.BitwiseOr, comment: "A holds rule index");

			// Get rule (new state)
			Do(OpCode.AddConst, ruleStartAddr);
			Do(OpCode.AToRamAddress);
			Do(OpCode.RamToB, comment: "B holds rule (new state)"); // B holds: rule (new state)

			// Save new state
			Do(OpCode.ConstToRamAddress, addrX);
			Do(OpCode.RamToA); // A holds: x
			Do(OpCode.AddConst, displayWidth);
			Do(OpCode.AToRamAddress); // address to store new state
			Do(OpCode.BToRam); // store new state
			Do(OpCode.BToDisplayReg);

			// Draw new state to pixel display at x,y
			Do(OpCode.ConstToRamAddress, addrY);
			Do(OpCode.RamToB);
			Do(OpCode.ConstToRamAddress, addrX);
			Do(OpCode.RamToA, comment: "A,B hold x,y");
			Do(OpCode.DisplayWrite, comment: "Write pixel"); // write pixel to display

			Do(OpCode.AddConst, 1, "Increment x"); // increment x
			Do(OpCode.BitwiseAndConst, 0b1111); // wrap x to 0
			Do(OpCode.AToRam); // store new x
			// if x == 0, increment y
			Do(OpCode.JumpIfNotZero, instructions.Count + 7 + 64, "Jump if not zero (dont increment y)"); // dont increment y
			Do(OpCode.BToA); // A holds: y
			Do(OpCode.AddConst, 1, "Increment y");
			Do(OpCode.ConstToRamAddress, addrY);
			Do(OpCode.AToRam);
			// .. if y == 16, end program
			Do(OpCode.CompareConst, 16);
			Do(OpCode.JumpIfZero, instructions.Count + 2 + 64, "Jump if zero (end progarm)");
			// Set curr state = new state
			for (int i = 0; i < displayWidth; i++)
			{
				Do(OpCode.ConstToRamAddress, i + displayWidth);
				Do(OpCode.RamToA, comment: $"A Load state new ({i}) from ram");
				Do(OpCode.ConstToRamAddress, i);
				Do(OpCode.AToRam, comment: "Ram set curr state to new state");
			}

			// .. end if
			// end if
			// end loop
			Do(OpCode.Jump, mainLoopStart, "Jump to loop start");


			Do(OpCode.Halt);

			return instructions.ToArray();

			void Do(OpCode op, int value = 0, string comment = "")
			{
				if (value > byte.MaxValue || value < sbyte.MinValue) throw new Exception("Value outside byte range: " + value);
				Instruction instruction = new(op, (byte)value, comment);
				instructions.Add(instruction);
			}
		}

		Instruction[] CreateTestProgam()
		{
			List<Instruction> instructions = new();

			// ALU test
			Do(OpCode.ARegSetConstant, 4);
			Do(OpCode.AddConst, 10); // 14
			Do(OpCode.AddConst, 1); // 5
			Do(OpCode.ConstToB, 7);
			Do(OpCode.Subtract); // 8
			Do(OpCode.LeftShift); // 16
			Do(OpCode.ConstToB, 14);
			Do(OpCode.BitwiseOr); // 30
			Do(OpCode.ConstToB, 0b111);
			Do(OpCode.BitwiseAnd); // 6
			// RAM test
			Do(OpCode.AToRamAddress);
			Do(OpCode.ConstToRam, 64); // store 64 at address 6
			Do(OpCode.ConstToRamAddress);
			Do(OpCode.AToRam); // store 6 at address 0
			Do(OpCode.RamToB); // load 6 into B
			Do(OpCode.ConstToRamAddress, 6);
			Do(OpCode.RamToA); // load 64 into A
			Do(OpCode.Add); // 70
			Do(OpCode.ConstToRamAddress);
			Do(OpCode.AToRam); // store 70 at address 0

			// Display test (draw diagonal line by incrementing x,y)
			Do(OpCode.ConstToRamAddress, 1);
			Do(OpCode.ConstToRam); // store zero at address 1 (x)
			Do(OpCode.ConstToRamAddress, 2);
			Do(OpCode.ConstToRam); // store zero at address 2 (y)
			Do(OpCode.ARegSetConstant, 1);
			Do(OpCode.AToDisplayReg);

			// Display loop start
			int loopstart = instructions.Count;
			Do(OpCode.ConstToRamAddress, 1); // load x,y into reg a and b
			Do(OpCode.RamToA);
			Do(OpCode.ConstToRamAddress, 2);
			Do(OpCode.RamToB);
			Do(OpCode.DisplayWrite); // set pixel
			Do(OpCode.ConstToB, 1);
			Do(OpCode.Add); // increment x
			Do(OpCode.ConstToRamAddress, 1);
			Do(OpCode.AToRam);
			Do(OpCode.ConstToRamAddress, 2);
			Do(OpCode.RamToA);
			Do(OpCode.Add); // increment y
			Do(OpCode.AToRam);

			Do(OpCode.ConstToB, 16); // test if y has reached 16, in which case exit the loop
			Do(OpCode.Compare);
			Do(OpCode.JumpIfZero, instructions.Count + 2); // exit loop
			Do(OpCode.Jump, loopstart);

			// Fetch previously stored 70 from ram 0
			Do(OpCode.ConstToRamAddress);
			Do(OpCode.RamToA);

			Do(OpCode.Halt); // Expected end state: RegA: 70, Display: diagonal line

			return instructions.ToArray();

			void Do(OpCode op, int value = 0)
			{
				if (value > byte.MaxValue || value < sbyte.MinValue) throw new Exception("Value outside byte range: " + value);
				Instruction instruction = new(op, (byte)value);
				instructions.Add(instruction);
			}
		}

		void PrintInstructionMemory(Instruction[] instructions)
		{
			StringBuilder sb = new();
			for (int i = 0; i < 256; i++)
			{
				ushort memVal = 0;
				string commentString = "";

				if (i < instructions.Length)
				{
					Instruction instruction = instructions[i];
					if (printWithComments) commentString = instruction.Comment;
					memVal = instruction.Pack();
				}

				string prep = printWithComments ? $"{i}: ".PadRight(4) : "";

				sb.AppendLine(prep + memVal + " " + commentString);
			}


			Debug.Log($"INSTRUCTION MEMORY: ({instructions.Length})");
			Debug.Log(sb.ToString());
		}

		void PrintControlUnitMemory(UInt16[] memory)
		{
			StringBuilder sb = new();

			foreach (UInt16 value in memory)
			{
				sb.AppendLine(value + "");
			}

			Debug.Log("CONTROL UNIT MEMORY:");
			Debug.Log(sb.ToString());
		}
		// Instruction memory: 256 x uint16 (uint8 opcode, uint8 const)

		enum OpCode
		{
			// ALU operations
			Add,
			AddConst,
			Subtract,
			SubtractConst,
			Compare,
			CompareConst,
			LeftShift,
			BitwiseAnd,
			BitwiseAndConst,
			BitwiseOr,
			BitwiseOrConst,

			// Jumps
			Jump,
			JumpIfZero,
			JumpIfNotZero,

			// Read/Write
			ARegSetConstant,
			RamToA,
			BToA,
			ConstToB,
			RamToB,
			AToB,
			AToRamAddress,
			ConstToRamAddress,
			ConstToRam,
			AToRam,
			BToRam,
			AToDisplayReg,
			BToDisplayReg,
			DisplayWrite,

			// Other
			Halt
		}


		enum AluOp // max 8 entries
		{
			Add,
			Subtract,
			And,
			Or,
			LeftShift,
			Unused0,
			Unused1,
			Unused2
		}

		enum JumpOp
		{
			None,
			Jump,
			JumpZero,
			JumpNotZero,
			Unused0,
			Unused1,
			Unused2,
			Unused3
		}

		enum OutputEnable // max 8 entries
		{
			None,
			Alu,
			RegA,
			RegB,
			Ram,
			Const,
			Unused0,
			Unused1
		}

		enum InputEnable // max 8 entries
		{
			None,
			RegA,
			RegB,
			RamAddress,
			Ram,
			DisplayReg,
			Display,
			Unused0
		}

		enum OtherControlFlags // max 4 entries
		{
			AluUseConst,
			Unused1,
			Unused2,
			Halt
		}

		struct Instruction
		{
			public readonly OpCode OpCode;
			public readonly byte ByteValue;
			public readonly string Comment;

			public Instruction(OpCode opCode, byte byteValue, string comment = "")
			{
				OpCode = opCode;
				ByteValue = byteValue;
				Comment = comment;
			}

			public ushort Pack()
			{
				int opInt = (int)OpCode;
				return (ushort)((opInt << 8) | ByteValue);
			}
		}
	}
}