using System;
using System.Runtime.InteropServices;

namespace UnityRawInput
{
    public static class Win32API
    {
        public delegate int HookProc (int code, IntPtr wParam, IntPtr lParam);

        [DllImport("User32")]
        public static extern IntPtr SetWindowsHookEx (HookType code, HookProc func, IntPtr hInstance, int threadID);
        [DllImport("User32")]
        public static extern int UnhookWindowsHookEx (IntPtr hhook);
        [DllImport("User32")]
        public static extern int CallNextHookEx (IntPtr hhook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("Kernel32")]
        public static extern uint GetCurrentThreadId ();
        [DllImport("Kernel32")]
        public static extern IntPtr GetModuleHandle (string lpModuleName);
    }
}
