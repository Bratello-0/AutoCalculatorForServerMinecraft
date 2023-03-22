using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoCalculatorKaboom
{
    internal class Program
    {
        #region Библиотеки
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        #endregion

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        const int WH_KEYBOARD_LL = 13; // Номер глобального LowLevel-хука на клавиатуру
        const int WM_KEYDOWN = 0x100;  //Key up

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = HookCallback;

        private static IntPtr _hookID = IntPtr.Zero;


        static void Main(string[] args)
        {
            Console.WriteLine("start hook");
            OpenThreadReadFile();
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);         //в фоновый режим
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        private static void SetClipboard(string text)
        {
            Thread thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private static async Task ReadFileAsync()
        {
            string pathHead = "C:\\Users\\Геральт\\AppData\\Roaming\\Kaboom\\modpacks\\1.7.10\\modpacks\\skyhard\\logs\\";
            string pathDest = "latest1.log";
            string pathSource = "latest.log";
            while (true)
            {
                string textFromFile;
                string result = "";

                File.Delete(pathHead + pathDest);
                File.Copy(pathHead + pathSource, pathHead + pathDest);

                using (FileStream fstream = new FileStream(pathHead + pathDest, FileMode.Open))
                {
                    fstream.Seek(-1500, SeekOrigin.End);

                    byte[] output = new byte[1500];
                    await fstream.ReadAsync(output, 0, output.Length);
                    textFromFile = Encoding.Default.GetString(output); //чтение 
                }
                result = Multiplication(GetDigit(textFromFile));

                Console.WriteLine($"Текст из файла: {result}");
                SetClipboard(result);

                Thread.Sleep(5000);
            }
        }
        private static string Multiplication(string inputStr)
        {
            int result = -1;
            string[] temp = inputStr.Split(new char[] { '*' }).Select(dig => new string(dig.Where(t => char.IsDigit(t)).ToArray())).ToArray();
            foreach (string item in temp)
            {
                Console.WriteLine(item);
            }

            if (temp.Any() && temp.Length >= 2) 
            {
                result = int.Parse(temp[0]) * int.Parse(temp[1]);
            }
            return result.ToString();
        }
        private static string GetDigit(string inputStr)
        {
            string result = "";

            List<string> lineStr = inputStr
                        .Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Replace(" ", ""))
                        .ToList();

            lineStr.ForEach(sub =>
            {
                if (sub.Length >= 9)
                {
                    sub = sub.Substring(sub.Length - 7);
                }

                for (int i = 0; i < sub.Length; i++)
                {
                    if (sub[i] == '*')
                    {
                        if (char.IsDigit(sub[i + 1]))
                        {
                            result = sub;
                            return;
                        }
                        else { continue; }
                    }
                }
            });

            return result;
        }

        private static void OpenThreadReadFile()
        {
            Thread thread = new Thread(async () => await ReadFileAsync());
            thread.Start();
        }

        #region Функция хука
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN || code >= 0 && wParam == (IntPtr)260)
            {
                int vkCode = Marshal.ReadInt32(lParam); //Получить код клавиши
                if (vkCode == 187)
                {
                    Application.Exit();
                }
            }
            return CallNextHookEx(_hookID, code, wParam, lParam);
        }
        #endregion

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
    }
}