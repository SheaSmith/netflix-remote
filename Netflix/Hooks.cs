using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Netflix
{
    class Hooks
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        public static LowLevelKeyboardProc keyboardProc = KeyboardCallback;
        public static IntPtr keyboardHookId = IntPtr.Zero;

        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        public static IntPtr KeyboardCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {

                if ((Keys)vkCode == Keys.Down)
                {
                    if (!Program.animating)
                        Program.Select(Program.MoveDown());

                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Up)
                {
                    if (!Program.animating)
                        Program.Select(Program.MoveUp());

                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Left)
                {
                    if (!Program.animating)
                        Program.Select(Program.MoveLeft(Program.currentHyperlink));

                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Right)
                {
                    if (!Program.animating)
                        Program.Select(Program.MoveRight(Program.currentHyperlink));

                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Enter)
                {
                    if (!Program.animating)
                        Program.Click();

                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Back && (Program.automation.FocusedElement() == null || !Program.automation.FocusedElement().Patterns.TextEdit.IsSupported))
                {
                    Keyboard.Type(VirtualKeyShort.ESCAPE);
                    return (IntPtr)(-1);
                }

                if ((Keys)vkCode == Keys.Escape)
                {
                    Program.Clear();
                    Thread.Sleep(1000);
                    Program.Reset();
                }

                Program.UpdateLinks();
            }

            if ((Keys)vkCode == Keys.Down || (Keys)vkCode == Keys.Up)
            {
                return (IntPtr)(-1);
            }

            return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
