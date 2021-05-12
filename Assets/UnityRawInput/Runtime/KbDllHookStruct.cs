using System;
using System.Runtime.InteropServices;

namespace UnityRawInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public KBDLLHOOKSTRUCTFlags flags;
        public uint time;
        public UIntPtr dwExtraInfo;

        public static KBDLLHOOKSTRUCT CreateFromPtr (IntPtr ptr)
        {
            return (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(ptr, typeof(KBDLLHOOKSTRUCT));
        }
    }

    [Flags]
    public enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }
}
