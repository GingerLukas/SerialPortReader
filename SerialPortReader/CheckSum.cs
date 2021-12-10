namespace SerialPortReader;

public struct CheckSum
{
    public byte A { get; set; }
    public byte B { get; set; }

    public bool Equals(CheckSum other)
    {
        return A == other.A && B == other.B;
    }
}