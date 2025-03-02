using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

class Program
{
    // Asynchronously retrieves the latest WinRAR download URL from the official website
    static async Task<string> GetLatestWinRARUrl()
    {
        string downloadPageUrl = "https://www.rarlab.com/download.htm";
        using (HttpClient client = new HttpClient())
        {
            string pageContent = await client.GetStringAsync(downloadPageUrl);

            // Search for the x64 version download link in the page content
            Match match = Regex.Match(pageContent, @"href=""(https://www\.rarlab\.com/rar/winrar-x64-[0-9]+\.exe)""");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                throw new Exception("Could not find the latest WinRAR download link!");
            }
        }
    }

    // Downloads and installs WinRAR silently
    static async Task InstallWinRAR()
    {
        try
        {
            string installerUrl = await GetLatestWinRARUrl();
            Console.WriteLine($"Downloading from: {installerUrl}");

            string installerPath = Path.Combine(Path.GetTempPath(), "winrar-installer.exe");

            // Download the WinRAR installer
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(installerUrl, installerPath);
            }

            Console.WriteLine("Installer downloaded, installing...");

            // Run the installer with silent mode and administrative privileges
            Process installerProcess = new Process();
            installerProcess.StartInfo.FileName = installerPath;
            installerProcess.StartInfo.Arguments = "/S";  // Silent installation
            installerProcess.StartInfo.UseShellExecute = true;
            installerProcess.StartInfo.Verb = "runas";  // Run as administrator
            installerProcess.Start();
            installerProcess.WaitForExit();

            // Delete the installer file after installation
            File.Delete(installerPath);
            Console.WriteLine("WinRAR has been successfully installed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error installing WinRAR: " + ex.Message);
        }
    }

    static async Task Main()
    {
        // Check if WinRAR is installed
        string winrarPath = FindWinRAR();
        if (string.IsNullOrEmpty(winrarPath))
        {
            await InstallWinRAR();
            Thread.Sleep(5000); // Wait a few seconds for installation to complete
            winrarPath = FindWinRAR();
        }

        // If still not found, exit
        if (string.IsNullOrEmpty(winrarPath))
        {
            Console.WriteLine("Failed to install WinRAR.");
            return;
        }

        Console.WriteLine("WinRAR found at: " + winrarPath);

        // Remove existing license key, write new key, and start WinRAR
        DeleteKey(winrarPath);
        WriteKey(winrarPath);
        StartWinRAR(winrarPath);

        Thread.Sleep(5000); // Wait a few seconds before closing WinRAR
        CloseWinRAR();
    }

    // Checks common installation paths for WinRAR
    static string FindWinRAR()
    {
        string[] possiblePaths = {
            @"C:\Program Files\WinRAR",
            @"C:\Program Files (x86)\WinRAR",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop\\apps\\winrar\\current")
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
                return path;
        }
        return "";
    }

    // Deletes the existing WinRAR license key if it exists
    static void DeleteKey(string winrarPath)
    {
        string keyPath = Path.Combine(winrarPath, "rarreg.key");
        if (File.Exists(keyPath))
        {
            File.Delete(keyPath);
            Console.WriteLine("License key deleted.");
        }
    }

    // Writes a new license key to the WinRAR directory
    static void WriteKey(string winrarPath)
    {
        string keyPath = Path.Combine(winrarPath, "rarreg.key");
        string keyContent = "" +
            "RAR registration data\n" +
            "HexCode\n" +
            "Single PC usage license\n" +
            "UID=4f7ec7a4a3abddc5c043\n" +
            "6412212250c0433ee2ffb690d43c9c4b506183532723bac916eda6\n" +
            "9cc8c2111d4381f9814060e38bb274f3ad73294c2171d322afe3af\n" +
            "57cb8adca2af8b4e7b9f4843a21877e42973bb2ad454801b2f0331\n" +
            "713a92ccc983bf6b6f20d120802682a68f607a56594a9fb22ca6f1\n" +
            "e27ad41eb6d55b0ee8798742ca58fc264879674c3c77e42973bb2a\n" +
            "d454801aa95c28813ad4cef4e00448b43379e1dd325b83bd609022\n" +
            "3ca5d1a41d61a97701fce62c994aa0dab365fc39026c1184221467\n" +
            "------------------------------------------------------\n";

        File.WriteAllText(keyPath, keyContent);
        Console.WriteLine("License key successfully written.");
    }

    // Starts WinRAR application
    static void StartWinRAR(string winrarPath)
    {
        string exePath = Path.Combine(winrarPath, "WinRAR.exe");
        if (File.Exists(exePath))
        {
            Process.Start(exePath);
        }
    }

    // Closes all running instances of WinRAR
    static void CloseWinRAR()
    {
        foreach (var process in Process.GetProcessesByName("WinRAR"))
        {
            process.Kill();
        }
    }
}
