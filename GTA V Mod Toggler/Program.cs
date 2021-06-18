using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;

namespace GTA_V_Mod_Toggler {
    class Program {

        private enum LogMode {
            Default,
            Add,
            Fatal,
            Info,
            Debug,
            Error
        }

        private static void Log(LogMode mode, string format, params object[] objects) {
            string prefix;
            switch (mode) {
                case LogMode.Default:
#if DEBUG
                case LogMode.Info:
#endif
                    prefix = "[i]";
                    break;
                case LogMode.Add:
#if DEBUG
                case LogMode.Debug:
#endif
                    prefix = "[+] ->";
                    break;
#if DEBUG
                case LogMode.Error:
                    prefix = "[-] ->";
                    break;
#endif
                case LogMode.Fatal:
                    prefix = "[*]";
                    break;
                default:
                    return;
            }
            Console.WriteLine($"{prefix} {String.Format(format, objects)}");
        }

        private static void PrintHeader() {
            Console.Clear();
            const string header = "╔═╗╔╦╗╔═╗  ╦  ╦  ╔╦╗┌─┐┌┬┐  ╔╦╗┌─┐┌─┐┌─┐┬  ┌─┐┬─┐\n║ ╦ ║ ╠═╣  ╚╗╔╝  ║║║│ │ ││   ║ │ ││ ┬│ ┬│  ├┤ ├┬┘\n╚═╝ ╩ ╩ ╩   ╚╝   ╩ ╩└─┘─┴┘   ╩ └─┘└─┘└─┘┴─┘└─┘┴└─";
            Console.WriteLine($"{header}\n{new string('-', header.Split('\n').Last().Length)}\n\n");
        }

        private static string GetInstallPath() {
            Log(LogMode.Default, "Searching for GTA V installation...");
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            var names = key?.GetSubKeyNames() ?? new string[0];
            foreach (var name in names) {
                var productKey = key?.OpenSubKey(name);
                var publisher = Convert.ToString(productKey?.GetValue("Publisher"));
                var displayName = Convert.ToString(productKey?.GetValue("DisplayName"));
                if (!publisher.Equals("Rockstar Games", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (!displayName.Equals("Grand Theft Auto V", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                var installPath = Convert.ToString(productKey?.GetValue("InstallLocation"));
                Log(LogMode.Add, $"Installation found @ {installPath}.");
                return installPath;
            }
            return "";
        }

        private static readonly string[] Dlls = {
            "dinput8",
            "d3d11"
        };

        private static bool ToggleMods(string installPath) {
            Log(LogMode.Default, "Toggling mods...");
            var enabled = -1;
            foreach (var dll in Dlls) {
                var enabledDll = $@"{installPath}\{dll}.dll";
                var disabledDll = $"{enabledDll}.disabled";
                var enabledDllExists = File.Exists(enabledDll);
                var disabledDllExists = File.Exists(disabledDll);
                if (enabledDllExists && disabledDllExists) {
                    File.Delete(disabledDll);
                    disabledDllExists = false;
                }
                if (enabledDllExists || enabled == 1) {
                    enabled = 1;
                    File.Move(enabledDll, disabledDll);
                    Log(LogMode.Debug, $"Moving {enabledDll} -> {disabledDll}");
                } else if (disabledDllExists || enabled == 0) {
                    enabled = 0;
                    File.Move(disabledDll, enabledDll);
                    Log(LogMode.Debug, $"Moving {disabledDll} -> {enabledDll}");
                } else {
                    Log(LogMode.Error, $"{enabledDll}[.disabled] not found.");
                }
            }
            if (enabled == -1) {
                Log(LogMode.Fatal, "No mods found!");
                return false;
            }
            Log(LogMode.Add, $"{(enabled == 1 ? "Disabled" : "Enabled")} mods.");
            return true;
        }

        static void Main(string[] args) {
            PrintHeader();
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string installPath;
            if (File.Exists(baseDirectory + @"\GTA5.exe")) {
                installPath = baseDirectory;
            } else {
                installPath = GetInstallPath();
                if (installPath == "") {
                    Log(LogMode.Fatal, "GTA V install path not found. Please move this executable to the root of your GTA V installation directory.\nAlternatively, you can input the install path below:");
                    installPath = Console.ReadLine();
                }
                if (!File.Exists(installPath + @"\GTA5.exe")) {
                    Log(LogMode.Fatal, "Invalid install path: GTA V not found.");
                    Environment.ExitCode = -1;
                    goto Exit;
                }
            }
            if (!ToggleMods(installPath)) {
                Environment.ExitCode = -2;
            }

            Exit:
            for (var i = 3; i > 0; i--) {
                if (i == 3) {
                    var exitMessage = $"Exiting in {i}...";
                    Console.Write($"\n\n{new string('-', exitMessage.Length * 2)}\n{exitMessage}");
                } else {
                    Console.Write($" {i}...");
                }
                Thread.Sleep(1000);
            }
        }

    }
}
