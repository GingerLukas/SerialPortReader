using System.IO.Ports;
using System.Runtime.InteropServices;

namespace SerialPortReader;

public class SerialPortCustom
{
    private const byte START_OF_PACKET = 0xAA;
    private const int HEADER_LENGTH = 14;
    private const int MAX_PACKET_SIZE = ushort.MaxValue + 14 + 2; //data + header + data_checksum
    private readonly SerialPort _serialPort;
    private byte[] _buffer = new byte[MAX_PACKET_SIZE * 2];

    public SerialPortCustom(SerialPort serialPort)
    {
        Stream stream = serialPort.BaseStream;
        _serialPort = serialPort;
    }

    private async Task<CustomPacket> ReadPacket()
    {
        int packetStart = await FindHeader();
        PacketHeader header = ExtractHeader(packetStart);
        CustomPacket packet = new CustomPacket() { Header = header };
        Array.Copy(_buffer, packet.Data, header.Length);
        return packet;
    }

    private async void ReadData(ushort length)
    {
        ushort readBytes = 0;
        while (readBytes < length)
        {
            WaitForData();

            _buffer[readBytes++] = (byte)_serialPort.ReadByte();
        }
    }

    private PacketHeader ExtractHeader(int index)
    {
        PacketHeader header = new PacketHeader();
        header.Flags = _buffer[index + 1];
        header.Src = BitConverter.ToUInt32(_buffer, index + 2);
        header.Dst = BitConverter.ToUInt32(_buffer, index + 6);
        header.Length = BitConverter.ToUInt16(_buffer, index + 10);
        return header;
    }

    private async Task<int> FindHeader()
    {
        WaitForValue(START_OF_PACKET);
        int readBytes = 0;
        _buffer[readBytes++] = START_OF_PACKET;
        int prevStartIndex = 0;
        List<int> possibleStarts = new List<int>() { 0 }; //initial position

        int length = HEADER_LENGTH;
        while (readBytes < length)
        {
            WaitForData();

            byte value = _buffer[(readBytes++)] = (byte)_serialPort.ReadByte();
            if (value == START_OF_PACKET)
            {
                possibleStarts.Add(prevStartIndex);
                int offset = readBytes - prevStartIndex;
                length += offset; //ensure to read whole possible header
                prevStartIndex = readBytes;
            }
        }

        int? correctStart = GetCorrectHeader(possibleStarts);

        if (correctStart == null)
        {
            throw new Exception("Didn't find valid frame header");
        }


        return correctStart.Value;
    }

    private int? GetCorrectHeader(List<int> possibleStarts)
    {
        foreach (int start in CollectionsMarshal.AsSpan(possibleStarts))
        {
            CheckSum sum = CheckSum(start + 1, HEADER_LENGTH - 3);
            CheckSum correctSum = new CheckSum()
            {
                A = _buffer[start + HEADER_LENGTH - 2],
                B = _buffer[start + HEADER_LENGTH - 2 + 1]
            };
            if (sum.Equals(correctSum))
            {
                return start;
            }
        }

        return null;
    }

    private CheckSum CheckSum(int start, int length)
    {
        byte A = 0xFF;
        byte B = 0;
        short sum = 0;
        for (int i = 0; i < length; i++)
        {
            A = (byte)((A + _buffer[i + start]) % 0x100);
            B = (byte)((B + A) % 0x100);
        }

        return new CheckSum() { A = A, B = B };
    }

    private async void WaitForValue(byte value)
    {
        while (true)
        {
            WaitForData();
            byte b = (byte)_serialPort.ReadByte();
            if (b == value)
            {
                return;
            }
        }
    }

    private async void WaitForData()
    {
        while (_serialPort.BytesToRead == 0)
        {
            await Task.Delay(TimeSpan.FromTicks(1));
        }
    }
}