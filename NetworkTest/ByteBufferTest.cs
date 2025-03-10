﻿using System.Text;
using NUnit.Framework;
using Shared.Network;

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
        var read = (ushort)((data[1] << 8) | data[0]);

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

    [Test]
    public void Resize_WhenDataBiggerThenSize_Resize()
    {
        // Arrange
        ByteBuffer buffer = new ByteBuffer(4);

        // Act
        var bigData = new byte[1024];
        buffer.Write(bigData, 0, bigData.Length);

        // Assert
        Assert.AreEqual(buffer.RawData.Length, 1024);
    }

    [Test]
    public void ReadToAnotherByteBuffer()
    {
        // Arrange
        ByteBuffer buffer1 = new ByteBuffer();
        ByteBuffer buffer2 = new ByteBuffer();

        var string1 = "ReadByteBufferTest";
        var data1   = Encoding.UTF8.GetBytes(string1); 
        buffer1.Write(data1, 0, data1.Length);
        buffer2.Write(data1, 0, data1.Length);

        // Act
        buffer1.Read(buffer2, buffer1.Length);

        // Assert
        byte[] data2 = new byte[buffer2.Length];
        buffer2.Read(data2, 0, buffer2.Length);
        var string2 = Encoding.UTF8.GetString(data2);

        Assert.AreEqual($"{string1}{string1}", string2);
    }
}