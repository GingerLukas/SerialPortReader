namespace SerialPortReader;

public struct PacketHeader
{
    public byte Flags { get; set; }
    public UInt32 Src { get; set; }
    public UInt32 Dst { get; set; }
    public UInt16 Length { get; set; }
}