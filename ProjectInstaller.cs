using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;

[RunInstaller(true)]
public class ProjectInstaller : Installer
{
    private ServiceProcessInstaller processInstaller;
    private ServiceInstaller serviceInstaller;

    public ProjectInstaller()
    {
        processInstaller = new ServiceProcessInstaller
        {
            Account = ServiceAccount.LocalSystem
        };

        serviceInstaller = new ServiceInstaller
        {
            ServiceName = "WinZabTemp",
            DisplayName = "WinZabTemp",
            Description = "Zabbix Temperature Service by Oliver Lee Chachou",
            StartType = ServiceStartMode.Automatic
        };

        Installers.Add(processInstaller);
        Installers.Add(serviceInstaller);
    }

    public override void Install(IDictionary savedState)
    {
        base.Install(savedState);

        // Display confirmation message using MessageBox
        if (Context.Parameters.ContainsKey("assemblypath"))
        {
            string assemblyPath = Context.Parameters["assemblypath"];
            string serviceName = Path.GetFileNameWithoutExtension(assemblyPath);

            // Start the service after installation
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }

            // Show message box with confirmation
            ShowConfirmationMessage(serviceName);
        }
    }

    private void ShowConfirmationMessage(string serviceName)
    {
        // Display a MessageBox with the service installation confirmation
        MessageBox.Show($"Service '{serviceName}' installed and started successfully.",
                        "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
