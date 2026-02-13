namespace MotorDebugStudio.Services;

public static class Crc16Ccitt
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        const ushort poly = 0x1021;
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ poly : crc << 1);
            }
        }

        return crc;
    }
}
