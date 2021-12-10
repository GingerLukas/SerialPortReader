namespace SerialPortReader;

public class CustomPacket
{
    public PacketHeader Header { get; set; }
    public byte[] Data { get; set; }
}