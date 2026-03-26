using LibreHardwareMonitor.Hardware;
class Program
{
    static void Main()
    {
        using var manager = new HardwareManager();
        while (true)    
        {
            manager.UpdateAll();
            manager.PrintAll();
            Thread.Sleep(1000);
            Console.Clear();
        }
    }
}

abstract class Hardware
{
    public string? Name{get;protected set;}
    public abstract void Update();
    public abstract string GetInfo();

}

class CPU : Hardware
{
    private IHardware cpu;
    public int Usage{get; private set;}
    public float Temperature{get; private set;}

    public CPU(Computer computer)
    {
        Name = "CPU";

        cpu = computer.Hardware
            .First(h => h.HardwareType == HardwareType.Cpu);
    }

    public override void Update()
    {
        cpu.Update();

        foreach(var sub in cpu.SubHardware)
        {
            sub.Update();
        }

        foreach(var sensor in cpu.Sensors)
        {
            if(sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total"))
            {
                Usage = (int)sensor.Value.GetValueOrDefault();
            }

            if(sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Package"))
            {
                Temperature = sensor.Value.GetValueOrDefault();
            }
        }
    }

    public override string GetInfo()
    {
        return $"{Name}: {Usage}% - {Temperature:F1}";
    }
}

class GPU : Hardware
{
    private IHardware gpu;

    public GPU(Computer computer)
    {
        Name = "GPU";

        gpu = computer.Hardware
            .FirstOrDefault(h=> h.HardwareType == HardwareType.GpuNvidia)
            ?? throw new Exception("Khong tim thay GPU");
    }
    public int Usage{get; private set;}

    public override void Update()
    {
        gpu.Update();
        foreach(var sensor in gpu.Sensors)
        {
            if(sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total"))
            {
                Usage = (int)sensor.Value.GetValueOrDefault();
            }
            
        }
    }

    public override string GetInfo()
    {
        return $"{Name}: {Usage}%";
    }
}

class HardwareManager : IDisposable
{
    public void Dispose()
    {
        computer.Close();
    }
    private Computer computer;
    private List<Hardware> hardwares;

    public HardwareManager()
    {
        computer = new Computer()
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
        };

        computer.Open();
        hardwares = new List<Hardware>()
        {
            new CPU(computer),
            new GPU(computer),
            new Memory(computer)
        };
    }
    public void UpdateAll()
    {
        foreach(var hardware in hardwares)
        {
            hardware.Update();
        }   
    }

    public void PrintAll()
    {
        foreach(var hw in hardwares)
        {
            Console.WriteLine(hw.GetInfo());
        }
    }
}

class Memory : Hardware
{
    private IHardware mem;

    public int Usage{get; private set;}

    public Memory(Computer computer)
    {
        Name = "MEM";

        mem = computer.Hardware
            .First(h => h.HardwareType == HardwareType.Memory);
    }

    public override void Update()
    {
        mem.Update();
        foreach(var sensor in mem.Sensors)
        {
            if(sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total"))
            {
                Usage = (int)sensor.Value.GetValueOrDefault();
            }
        }
    }

    public override string GetInfo()
    {
        return $"{Name}: {Usage}%";
    }
}