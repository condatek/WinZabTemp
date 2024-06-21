using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

public class TemperatureMonitorService : ServiceBase
{
    private Computer computer;
    private const string TemperatureFilePath = @"C:\OpenHardwareMonitor\temperature.txt";
    private Timer _timer;

    public void StartService()
    {
        OnStart(null);
    }

    protected override void OnStart(string[] args)
    {
        InitializeComputer();

        // Check if temperature.txt exists, else create it
        try
        {
            if (!File.Exists(TemperatureFilePath))
            {
                File.Create(TemperatureFilePath).Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating temperature file: {ex.Message}");
        }

        _timer = new Timer(MonitorTemperature, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void InitializeComputer()
    {
        computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };

        computer.Open();
    }

    private void MonitorTemperature(object state)
    {
        try
        {
            var sensorVisitor = new SensorVisitor(HandleSensorEvent);
            sensorVisitor.VisitComputer(computer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MonitorTemperature: {ex.Message}");
        }
    }

    private void HandleSensorEvent(ISensor sensor)
    {
        try
        {
            // Filter only CPU temperature sensors
            // CPU Package = Intel - Core (Tctl/Tdie) = AMD - To verify
            if (sensor.SensorType == SensorType.Temperature &&
                (sensor.Name.Contains("CPU Package") || sensor.Name.Contains("Core (Tctl/Tdie)")))
            {
                float temperature = sensor.Value.GetValueOrDefault();
                float roundedTemperature = (float)Math.Round(temperature, 1);
                string temperatureInfo = $"{sensor.Name}: {roundedTemperature}°C";

                Console.WriteLine(temperatureInfo);
                Task.Run(() => WriteTemperatureToFileAsync(roundedTemperature));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling sensor event: {ex.Message}");
            // Optionally handle or log the exception
        }
    }


    private async Task WriteTemperatureToFileAsync(float temperature)
    {
        try
        {
            float roundedTemperature = (float)Math.Round(temperature, 1);
            using (StreamWriter writer = new StreamWriter(TemperatureFilePath, false))
            {
                await writer.WriteLineAsync($"{roundedTemperature}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing temperature file: {ex.Message}");
        }
    }

    protected override void OnStop()
    {
        _timer?.Change(Timeout.Infinite, 0);
        computer.Close();
    }
}

public class SensorVisitor
{
    private readonly Action<ISensor> _sensorAction;

    public SensorVisitor(Action<ISensor> sensorAction)
    {
        _sensorAction = sensorAction;
    }

    public void VisitComputer(IComputer computer)
    {
        computer.Accept(new Visitor(_sensorAction));
    }

    private class Visitor : IVisitor
    {
        private readonly Action<ISensor> _sensorAction;

        public Visitor(Action<ISensor> sensorAction)
        {
            _sensorAction = sensorAction;
        }

        public void VisitComputer(IComputer computer)
        {
            foreach (var hardware in computer.Hardware)
            {
                hardware.Accept(this);
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                _sensorAction(sensor);
            }

            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
