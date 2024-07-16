using NModbus;
using NModbus.Data;
using NModbus.Device;
using NModbus.Serial;
using Sensors;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;

namespace Emulator
{
    public partial class MainWindow : Window
    {
        private readonly SerialPortProvider serialPortProvider = new SerialPortProvider();
        private readonly Sensor sensor = new Sensor();

        public MainWindow()
        {
            InitializeComponent();
            Dimensions.ItemsSource = Enum.GetValues(typeof(Dimension));
            Dimensions.SelectedIndex = 2;
            serialPortProvider.PortsNamesChanged.StartWith(serialPortProvider.PortNames).Subscribe(UpdatePortNames);
            ConnectButton.Click += ConnectButton_Click;
            PressureButton.Click += PressureButton_Click;
            DimensionButton.Click += DimensionButton_Click;
        }

        private void DimensionButton_Click(object sender, RoutedEventArgs e)
        {
            sensor.SetDimension((Dimension)Dimensions.SelectedItem);
        }

        private void PressureButton_Click(object sender, RoutedEventArgs e)
        {
            var str = Pressure.Text.Replace(".", ",");
            if (!float.TryParse(str, out var pressure))
            {
                MessageBox.Show("Требуется значение типа float.");
            }
            else
            {
                sensor.SetPressure(pressure);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            sensor.Connect((string)Connect.SelectedItem);
        }

        private void UpdatePortNames(IEnumerable<string> portNames)
        {
            Dispatcher.Invoke(() => Connect.ItemsSource = portNames);
        }
    }

    public class Sensor : IDisposable
    {
        private readonly SerialPort serialPort = new SerialPort();
        private static readonly ModbusFactory modbusFactory = new ModbusFactory();
        private readonly IModbusSlaveNetwork network;
        private readonly IModbusSlave slave;

        public Sensor()
        {
            network = modbusFactory.CreateRtuSlaveNetwork(serialPort);
            slave = new ModbusSlave(1, new SlaveDataStore(), new IModbusFunctionService[] { new ReadHoldingRegistersService(), new WriteMultipleRegistersService() });
            network.AddSlave(slave);
        }
        public void Connect(string portname) 
        {
            serialPort.PortName = portname;
            serialPort.Parity = System.IO.Ports.Parity.Even;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();
            slave.DataStore.HoldingRegisters.WritePoints(0x20, new ushort[] {0x1100});
            Task.Run(async () => await network.ListenAsync());
        }

        public void Dispose()
        {
            network.Dispose();
            serialPort.Dispose();
        }

        public void SetPressure(float pressure)
        {
            ushort reg27 = 0;
            ushort reg28 = 0;
            var bytes = BitConverter.GetBytes(pressure);
            reg27 = ByteManipulater.ChangeMSB(reg27, bytes[3]);
            reg27 = ByteManipulater.ChangeLSB(reg27, bytes[2]);
            reg28 = ByteManipulater.ChangeMSB(reg28, bytes[1]);
            reg28 = ByteManipulater.ChangeLSB(reg28, bytes[0]);
            slave.DataStore.HoldingRegisters.WritePoints(0x27, new ushort[] {reg27, reg28});
        }

        public void SetDimension(Dimension dimension)
        {
            var points = slave.DataStore.HoldingRegisters.ReadPoints(0x01, 1);
            var byteValue = DimensionConverter.Default.Convert(dimension);
            var regValue = ByteManipulater.ChangeLSB(points[0], byteValue);
            slave.DataStore.HoldingRegisters.WritePoints(0x01, new ushort[] { regValue });
        }
    }

    public class SerialPortProvider
    {
        public IEnumerable<string> PortNames 
        { 
            get
            {
                lock(locker)
                {
                    return portNames;
                }
            }
            set
            {
                lock (locker)
                {
                    portNames = value.ToArray();                    
                }
            }
        }

        public IObservable<IEnumerable<string>> PortsNamesChanged => portsNamesChanged.AsObservable();
        private string[] portNames = Array.Empty<string>();
        private readonly object locker = new object();
        private readonly Subject<IEnumerable<string>> portsNamesChanged = new Subject<IEnumerable<string>>();


        public SerialPortProvider()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    Update();
                    await Task.Delay(500);
                }
            });
        }

        private void Update()
        {
            string[] ports = SerialPort.GetPortNames().OrderBy(x => x).Where(x => x.Contains("COM")).ToArray();
            var raiseEvent = AreNotEqual(ports);
            PortNames = ports;
            if (raiseEvent)
            {
                portsNamesChanged.OnNext(ports);
            }
        }

        private bool AreNotEqual(string[] ports)
        {
            if (ports.Length != portNames.Count())
            {
                return true;
            }
            for (int i = 0; i < ports.Length; i++)
            {
                if (portNames[i] != ports[i])
                {
                    return true;
                }
            }
            return false;//Коллекции равны
        }

    }
}
