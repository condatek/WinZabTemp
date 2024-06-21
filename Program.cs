using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

public class Program
{
    public static void Main(string[] args)
    {
        if (Environment.UserInteractive)
        {
            Console.WriteLine("Do you want to install this program as a Windows service? (y/n)");
            string input = Console.ReadLine();

            if (input?.ToLower() == "y")
            {
                // Install the service
                ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                Console.WriteLine("Service installed. Please start the service manually from the Services management console.");
            }
            else
            {
                // Run as console application
                RunAsConsole();
            }
        }
        else
        {
            // Run as Windows service
            ServiceBase.Run(new TemperatureMonitorService());
        }
    }

    private static void RunAsConsole()
    {
        TemperatureMonitorService service = new TemperatureMonitorService();
        service.StartService();
        Console.WriteLine("Press any key to stop the program...");
        Console.ReadKey();
        service.Stop();
    }
}
