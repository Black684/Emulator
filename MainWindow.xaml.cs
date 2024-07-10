using NModbus;
using NModbus.Device;
using NModbus.Serial;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Emulator
{
    public partial class MainWindow : Window
    {
        private readonly SerialPortProvider serialPortProvider = new SerialPortProvider();
        private readonly Sensor sensor = new Sensor();

        public MainWindow()
        {
            InitializeComponent();
            serialPortProvider.PortsNamesChanged.StartWith(serialPortProvider.PortNames).Subscribe(UpdatePortNames);
            ConnectButton.Click += ConnectButton_Click;
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
            slave = modbusFactory.CreateSlave(1);
            network.AddSlave(slave);
            slave = new ModbusSlave(
        }
        public void Connect(string portname) 
        {
            serialPort.PortName = portname;
            serialPort.Parity = Parity.Even;
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
