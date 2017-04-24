using System;
using System.Runtime.InteropServices;

namespace DiscordMusicBot {
    internal class ConsoleHelper {

        internal static void Set() {
            //Disable Cursor Visibility
            Console.CursorVisible = false;

            //Window Close & any Close Event
            Handler += DisposeBot;
            SetConsoleCtrlHandler(Handler, true);

            //Ctrl + C | Ctrl + Break
            Console.CancelKeyPress += CancelKey;

            //Disable Quick Edit and Mouse Input
            DisableMouse();
        }

        private static void CancelKey(object sender, ConsoleCancelEventArgs e) {
            DisposeBot(CtrlType.CTRL_C_EVENT);
        }

        private static bool DisposeBot(CtrlType sig) {
            if (Program.Bot != null && !Program.Bot.IsDisposed)
                Program.Bot.Dispose();

            return false;
        }


        internal static void DisableMouse() {
            const uint ENABLE_QUICK_EDIT = 0x0040;
            const uint ENABLE_MOUSE_INPUT = 0x0010;

            IntPtr consoleHandle = GetConsoleWindow();

            if (!GetConsoleMode(consoleHandle, out uint consoleMode)) {
                // error
                return;
            }

            // Clear quick edit & Mouse input flags
            consoleMode &= ~ENABLE_QUICK_EDIT;
            consoleMode &= ~ENABLE_MOUSE_INPUT;

            if (!SetConsoleMode(consoleHandle, consoleMode)) {
                // error
            }
        }


        internal enum CtrlType {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        internal delegate bool EventHandler(CtrlType sig);
        internal static EventHandler Handler;


        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();
    }
}
