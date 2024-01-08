using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;

namespace FAQDemo
{
    class Program {
        # region process Flags & Handlers
        internal struct Flags  {
            public const int PROCESS_VM_OPERATION = 0x0008;
            public const int PROCESS_VM_READ = 0x0010;
            public const int PROCESS_VM_WRITE = 0x0020;
            public const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        }
        static IntPtr processHandler;
        static Process fixProcess;
        #endregion
        #region external DLLs import
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
        #endregion
        #region internal stuff
        static readonly Dictionary<string, int> fixVersions = new Dictionary<string, int>();
        static int baseAddress;
        internal struct LogLevel {
            public const byte Critical = 0;
            public const byte Error = 1;
            public const byte Warning = 2;
            public const byte Info = 3;
            public const byte Debug = 4;
        }
        const uint versionMajor = 1;
        const uint versionMinor = 0;
        const uint versionBuild = 1;
        const uint versionRevision = 1;
        #endregion
        #region parameters
        static string fixVersion = null;
        static int demoLenght = 720;
        static byte logLevel = LogLevel.Critical;
        static bool readOnly = false;
        #endregion

        static void Main(string[] args) {
            parseArguments(args);

            if (fixVersion == null) {
                if (logLevel >= LogLevel.Critical) Console.WriteLine("No Fix version specified. (To specify a version, run the .exe with the parameter fixVersion, ex: 'FAQDemo.exe /fixVersion:68', where 68 is a entry in the file 'FixVersions.xml' associed with a offset)");
                Environment.Exit(-1);
            }

            if (logLevel >= LogLevel.Debug) {
                Console.WriteLine("========== Parameters ==========");
                Console.WriteLine(" - Fix version specified (Unverified): " + fixVersion + ".");
                if (!readOnly) Console.WriteLine(" - Demo Lenght: " + demoLenght + " minutes.");
                Console.WriteLine(" - Log Level: " + logLevel + ".");
                Console.WriteLine("================================");
            }

            if (logLevel >= LogLevel.Info) Console.WriteLine("Reading the file '" + AppDomain.CurrentDomain.BaseDirectory + "FixVersions.xml" + "' for versions offsets.");
            readVersionsXML(AppDomain.CurrentDomain.BaseDirectory + "FixVersions.xml");

            if (!isVersionMapped(fixVersion)) {
                if (logLevel >= LogLevel.Critical) Console.WriteLine("Fix version unmapped, aborting.");
                Environment.Exit(-1);
            }

            if (logLevel >= LogLevel.Debug) Console.WriteLine("Getting Fix process...");
            fixProcess = Process.GetProcessesByName("Fix")[0];

            if (logLevel >= LogLevel.Debug) Console.WriteLine("Getting Fix base address...");
            foreach (ProcessModule ProcMod in fixProcess.Modules) if (ProcMod.ModuleName == "fix.exe") baseAddress = (int)ProcMod.BaseAddress;

            if (logLevel >= LogLevel.Debug) Console.WriteLine("Getting Fix process Handler...");
            processHandler = OpenProcess(Flags.PROCESS_VM_OPERATION | Flags.PROCESS_VM_READ | Flags.PROCESS_VM_WRITE, false, fixProcess.Id);

            if (readOnly) {
                if (logLevel >= LogLevel.Info) Console.Write("Current demo lenght: ");
                Console.Write(readDemoLenght());
                if (logLevel >= LogLevel.Info) Console.WriteLine(" minutes.");
                Environment.Exit(-1);
            }

            if (logLevel >= LogLevel.Info) Console.WriteLine("Current demo lenght: " + readDemoLenght() + " minutes.");

            if (logLevel >= LogLevel.Info) Console.WriteLine("Writing new demo lenght...");
            writeDemoLenght(demoLenght);

            if (logLevel >= LogLevel.Debug) Console.WriteLine(readDemoLenght() == demoLenght ? "Success." : "Fail.");
        }

        static void parseArguments(string[] args) {
            foreach (string arg in args) {
                switch (arg.Split(':')[0]) {
                    case "/fixVersion": fixVersion = arg.Split(':')[1]; break;
                    case "/demoLenght": demoLenght = Convert.ToInt32(arg.Split(':')[1]);  break;
                    case "/logLevel": logLevel = Convert.ToByte(arg.Split(':')[1]); break;
                    case "/readOnly": readOnly = true; break;
                    case "/getVersion": printVersion(); Environment.Exit(-1); break;
                    case "/NoGame": Console.WriteLine("NoLife"); Environment.Exit(-1); break;
                    case "/help": printHelp(); Environment.Exit(-1); break;
                    default: if (logLevel >= LogLevel.Error) Console.WriteLine("Unrecognized parameter: " + arg); break;
                }
            }
        }

        static void printHelp() {
            Console.WriteLine("The intended use for thi executable is to be inserted as an autostart entry inside the.SCU file of a project in development fase, to make the demo last significatly longer.");
            Console.WriteLine("");
            Console.WriteLine("How to use:");
            Console.WriteLine("FAQ.exe /fixVersion:X [/demoLenght:(0-32767)] [/logLevel:(0-4)] [/readOnly] [/getVersion] [/help]");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("FAQ.exe / fixVersion:65");
            Console.WriteLine("FAQ.exe / fixVersion:65 / demoLenght:32767");
            Console.WriteLine("FAQ.exe / fixVersion:65 / logLevel:4");
            Console.WriteLine("FAQ.exe / fixVersion:65 / demoLenght:234 / logLevel:2");
            Console.WriteLine("");
            Console.WriteLine("fixVersion:");
            Console.WriteLine("The parameter fixVersion, specify the iFix's version of which the user intends to modify the demo lenght.");
            Console.WriteLine("It is necessary to find in the file 'FixVersions.xml', found in the same directory as the.exe, the right offset in memory.");
            Console.WriteLine("When using CheatEngine to modify the demo lenght, the address of the signed two bytes which represents the left minutes of the demo(120 at the start by default), is specified with a format of fix.exe + X;");
            Console.WriteLine("The 'X' is the offset of the memory area, and is expressed in Hexadecimal, while in the.xml is expressed in decimal, therefore, a conversion in needded when adding new versions.");
            Console.WriteLine("");
            Console.WriteLine("demoLenght:");
            Console.WriteLine("It represend the value at which the memory address will be set at;");
            Console.WriteLine("By default it is 720, if it is not passed as a parameter.");
            Console.WriteLine("Negative numbers are possible, but non - sense.");
            Console.WriteLine("32767 is the max number.");
            Console.WriteLine("");
            Console.WriteLine("logLevel:");
            Console.WriteLine("It range from 0 to 4 and represents the level of details the executable will comunicate to the user what is going on.");
            Console.WriteLine("0: Critical");
            Console.WriteLine("1: Error");
            Console.WriteLine("2: Warning");
            Console.WriteLine("3: Info");
            Console.WriteLine("4: Debug");
            Console.WriteLine("");
            Console.WriteLine("readOnly:");
            Console.WriteLine("Will print the current demoLenght stored in the memory address, with no additional decoration, if the logLevel is below Info.");
            Console.WriteLine("Usefull to automate some procedure or diagnosis.");
            Console.WriteLine("note: this parameter will ONLY print the remaining demo time, even if provided, it will prevent the write of a new demoLenght.");
            Console.WriteLine("");
            Console.WriteLine("getVersion:");
            Console.WriteLine("Will ONLY print the version of the EXE.");
            Console.WriteLine("By: Ex_FST; Based on an idea and with the contribution of Shylix12");
        }

        static void printVersion() {
            Console.WriteLine(string.Format("{0}.{1}-build{2}.rev{3}",
                versionMajor,
                versionMinor,
                versionBuild,
                versionRevision));
        }

        static bool isVersionMapped(string fixVersion)
        {
            return fixVersions.ContainsKey(fixVersion);
        }

        static int getAddress(string fixVersion)
        {
            return baseAddress + fixVersions[fixVersion];
        }

        static short readDemoLenght()
        {
            int bytesRead = 0;
            byte[] buffer_R = new byte[2];

            ReadProcessMemory((int)processHandler, getAddress(fixVersion), buffer_R, buffer_R.Length, ref bytesRead);

            return BitConverter.ToInt16(buffer_R, 0);
        }

        static void writeDemoLenght(int demoLenght)
        {
            int bytesWritten = 0;
            byte[] buffer_W = BitConverter.GetBytes(demoLenght);

            WriteProcessMemory((int)processHandler, getAddress(fixVersion), buffer_W, buffer_W.Length, ref bytesWritten);
        }

        static void readVersionsXML(string filePath) {
            XmlDocument document = new XmlDocument();
            document.Load(filePath);

            foreach (XmlNode node in document.DocumentElement.ChildNodes) {
                fixVersions.Add(
                    node.Attributes["code"].Value,
                    Convert.ToInt32(node.Attributes["offset"].Value));
            }

        }
    }
}