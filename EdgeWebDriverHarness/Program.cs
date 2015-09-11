﻿using EdgeWebDriverHarness.Models;
using OpenQA.Selenium.Edge;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace EdgeWebDriverHarness {

    class Program {

        static void Main(string[] args) {
            Console.Write("Release: ");
            var release = Console.ReadLine();

            Console.Write("Environment: ");
            var environment = Console.ReadLine();

            Console.Write("Cluster (optional):");
            var cluster = Console.ReadLine();

            Console.WriteLine("Starting Edge automation.");

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            if (!string.IsNullOrWhiteSpace(UserConfig.Instance.ErrorLog)) {
                Trace.Listeners.Add(new TextWriterTraceListener(UserConfig.Instance.ErrorLog));
            }

            UserConfig.Instance.Reload();

            var now = DateTime.Now;
            var yearWeek = DateTimeFormatInfo.CurrentInfo.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
            var releaseName = $"{now.ToString("yy")}.{yearWeek} {release}";
            var runDateAndHour = now.ToString("MM-dd-yyyy HH");

            try {
                var serverPath = Path.Combine(Environment.ExpandEnvironmentVariables($"{((Environment.Is64BitOperatingSystem) ? "%ProgramFiles(x86)%" : "%ProgramFiles%")}"), Properties.Settings.Default.WebDriverName);
                var options = new EdgeOptions { PageLoadStrategy = EdgePageLoadStrategy.Normal };

                using (var driver = new EdgeDriver(serverPath, options)) {
                    //This doesn't seem to work yet?
                    //driver.Manage().Window.Maximize();
                    //driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 15));
                    foreach (Site site in UserConfig.Instance.Sites.Where(s => string.IsNullOrWhiteSpace(cluster) || s.Cluster.Equals(cluster, StringComparison.OrdinalIgnoreCase))) {
                        foreach (Page page in UserConfig.Instance.Pages) {
                            try {
                                driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(30));
                                driver.Navigate().GoToUrl($"{site.BaseUrl}{page.Suffix}");
                                Thread.Sleep(5000);
                                var imagePath = Path.Combine(UserConfig.Instance.ScreenShotPath, releaseName, environment, runDateAndHour, "Desktop", $"{site.Name} {page.Name} edge.png");
                                Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                                var screenshot = driver.GetScreenshot();
                                screenshot.SaveAsFile(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                            } catch (Exception e) {
                                Trace.WriteLine(e.Message);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Trace.WriteLine(e.Message);
            }

            Console.WriteLine("That's all folks! Press enter to close this window.");
            Console.ReadLine();
        }
    }
}