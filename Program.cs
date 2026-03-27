using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using Mono.Unix.Native;

class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    extern static bool DestroyIcon(IntPtr handle);
    static NotifyIcon? cpuIcon, gpuIcon, ramIcon;
    static System.Windows.Forms.Timer? timer;
    static HardwareManager? manager;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        manager = new HardwareManager();

        ContextMenuStrip contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Ext PerfMonitor", null, (s,e ) => Application.Exit());

        cpuIcon = new NotifyIcon() {Visible = true, ContextMenuStrip = contextMenu};
        gpuIcon = new NotifyIcon() {Visible = true, ContextMenuStrip = contextMenu};
        ramIcon = new NotifyIcon() {Visible = true, ContextMenuStrip = contextMenu};

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 1000;
        timer.Tick += Timer_Tick;
        timer.Start();
        
        Application.Run();

        cpuIcon.Dispose();
        gpuIcon.Dispose();
        ramIcon.Dispose();
        manager.Dispose();
    }

    private static void Timer_Tick(Object? sender, EventArgs e)
    {
        if(manager == null) return;
        manager.UpdateAll();

        var hardwares = manager.GetHardwares().ToList();
        
        var cpu = hardwares.FirstOrDefault(s=>s.Name == "CPU") as CPU;
        var gpu = hardwares.FirstOrDefault(s=>s.Name == "GPU") as GPU;
        var mem = hardwares.FirstOrDefault(s=>s.Name == "MEM") as Memory;

        if (cpu != null && cpuIcon != null)
        {
            UpdateIcon(cpuIcon, cpu.Usage.ToString(), Brushes.Red, cpu.GetInfo());
        }
        if(gpu != null && gpuIcon != null)
        {
            UpdateIcon(gpuIcon, gpu.Usage.ToString(),Brushes.DeepSkyBlue, gpu.GetInfo());
        }
        if(mem != null && ramIcon != null)
        {
            UpdateIcon(ramIcon, mem.Usage.ToString(), Brushes.LimeGreen, mem.GetInfo());
        }
    }

    private static void UpdateIcon(NotifyIcon targetIcon, string text, Brush textBrush, string tooltipText)
    {
        using (Bitmap bitmap = new Bitmap(16, 16))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);

            using(Font font = new Font("Tahoma", 8, FontStyle.Bold))
            {
                g.DrawString(text, font, textBrush, -2,2);
            }
        
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);

            targetIcon.Icon = icon;
            targetIcon.Text = tooltipText;

            DestroyIcon(hIcon);
            icon.Dispose();
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

        var loadSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");
        var tempSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Package"));

        if(loadSensor != null)
        {
            Usage = (int)loadSensor.Value.GetValueOrDefault();
        }
        if(tempSensor != null)
        {
            Temperature = tempSensor.Value.GetValueOrDefault();
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
       
        var loadSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
        if(loadSensor != null)
        {
            Usage = (int)loadSensor.Value.GetValueOrDefault();
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

    public IEnumerable<Hardware> GetHardwares()
    {
        return hardwares;
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
        
        var loadSensor = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
        if(loadSensor != null)
        {
            Usage = (int)loadSensor.Value.GetValueOrDefault();
        }
    }

    public override string GetInfo()
    {
        return $"{Name}: {Usage}%";
    }
}