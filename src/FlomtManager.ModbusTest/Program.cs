using FlomtManager.Modbus;
using System.Text;

namespace FlomtManager.ModbusTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var count = (ushort)125;
            var modbusProtocol = new ModbusProtocolTcp("185.147.58.54", 5000);
            modbusProtocol.Open();
            for (int i = 0; i < 5;  i++)
            {
                var start = (ushort)(i * 250);
                var bytes = modbusProtocol.ReadRegistersBytes(1, start, count, CancellationToken.None);
                Console.WriteLine("{0} + {1}", start, count);
                Console.WriteLine(string.Join(" ", bytes.Select(x => x.ToString("x2")) ?? []));
                Console.WriteLine(Encoding.ASCII.GetString(bytes));
            }
            modbusProtocol.Close();
        }
    }
}
