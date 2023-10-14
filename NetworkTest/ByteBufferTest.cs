using Network;
using NUnit.Framework;

namespace NetworkTest;

[TestFixture]
public class ByteBufferTest
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(1234)]
    [TestCase(65535)]
    public void WriteUInt16_GiveNumbers_ReadInLittleEndian(int input)
    {
        // Arrange
        ByteBuffer buffer = new ByteBuffer();

        // Act
        buffer.WriteUInt16((ushort)input);

        // Assert
        var data = new byte[2];
        Array.Copy(buffer.RawData, buffer.ReadIndex, data, 0, buffer.Length);
        var read = (UInt16)((data[1] << 8) | data[0]);

        Assert.AreEqual(input, read);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(1234)]
    [TestCase(2545567)]
    public void WriteUInt32_GiveNumbers_ReadInLittleEndian(int input)
    {
        // Arrange
        ByteBuffer buffer = new ByteBuffer();

        // Act
        buffer.WriteUInt32((uint)input);

        // Assert
        var data = new byte[4];
        Array.Copy(buffer.RawData, buffer.ReadIndex, data, 0, buffer.Length);
        var read = (UInt32)((data[3] << 24) |
                            (data[2] << 16) |
                            (data[1] << 8) |
                            data[0]);

        Assert.AreEqual(input, read);
    }
}