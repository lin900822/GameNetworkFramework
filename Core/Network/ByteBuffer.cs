namespace Core.Network;

public class ByteBuffer
{
    // Define
    private const int DEFAULT_SIZE = 1024 * 4; // 4kb

    // Properties
    public int Remain   => _capacity - _writeIndex;
    public int Length   => _writeIndex - _readIndex;
    public int Capacity => _capacity;

    public byte[] RawData => _rawData;
    public int ReadIndex => _readIndex;
    public int WriteIndex => _writeIndex;

    // Private Variables
    private byte[] _rawData;

    private int _readIndex;
    private int _writeIndex;

    private int _capacity = 0;
    private int _initSize = 0;

    // Methods
    public ByteBuffer(int size = DEFAULT_SIZE)
    {
        _rawData    = new byte[size];
        _capacity   = size;
        _initSize   = size;
        _readIndex  = 0;
        _writeIndex = 0;
    }

    public void SetReadIndex(int value) => _readIndex = value;
    public void SetWriteIndex(int value) => _writeIndex = value;

    // 擴充容量
    public void Resize(int size)
    {
        if (size < Length) return;
        if (size < _initSize) return;

        // 擴充為2的冪次 256 512 1024 2048...
        var newSize = 1;
        while (newSize < size)
        {
            newSize *= 2;
        }

        _capacity = newSize;
        var newData = new byte[_capacity];
        Array.Copy(_rawData, _readIndex, newData, 0, Length);
        _rawData = newData;

        _writeIndex = Length;
        _readIndex = 0;
    }

    // 檢查與複用byte空間
    public void CheckAndReuseCapacity()
    {
        if (Length < 8) ReuseCapacity();
    }

    // 複用byte空間
    private void ReuseCapacity()
    {
        if (Length > 0)
        {
            Array.Copy(_rawData, _readIndex, _rawData, 0, Length);
        }

        // 這裡順序要注意不能相反
        _writeIndex = Length;
        _readIndex = 0;
    }

    // 寫入資料
    public int Write(byte[] bytes, int offset, int count)
    {
        if (Remain < count) ReuseCapacity();
        if (Remain < count) Resize(Length + count);

        Array.Copy(bytes, offset, _rawData, _writeIndex, count);
        _writeIndex += count;
        return count;
    }

    // 寫入ushort
    public void WriteUInt16(ushort value)
    {
        if (Remain < 2) ReuseCapacity();
        if (Remain < 2) Resize(Length + 2);

        _rawData[_writeIndex] = (byte)(value & 0xFF);
        _rawData[_writeIndex + 1] = (byte)((value >> 8) & 0xFF);

        _writeIndex += 2;
    }

    // 寫入UInt32
    public void WriteUInt32(UInt32 value)
    {
        if (Remain < 4) ReuseCapacity();
        if (Remain < 4) Resize(Length + 4);

        _rawData[_writeIndex] = (byte)(value & 0xFF);
        _rawData[_writeIndex + 1] = (byte)((value >> 8) & 0xFF);
        _rawData[_writeIndex + 2] = (byte)((value >> 16) & 0xFF);
        _rawData[_writeIndex + 3] = (byte)((value >> 24) & 0xFF);

        _writeIndex += 4;
    }

    // 讀取資料
    public int Read(byte[] bytes, int offset, int count)
    {
        count = Math.Min(count, Length);
        Array.Copy(_rawData, _readIndex, bytes, offset, count);
        _readIndex += count;
        CheckAndReuseCapacity();
        return count;
    }

    // 檢查ushort
    public ushort CheckUInt16(int offset = 0)
    {
        if (Length < 2) return 0;
        // 以小端方式讀取Int16
        ushort readUInt16 = (ushort)((_rawData[_readIndex + 1 + offset] << 8) | _rawData[_readIndex + offset]);
        return readUInt16;
    }
    
    public uint CheckUInt32()
    {
        if (Length < 4) return 0;
        // 以小端方式讀取Int32
        uint readUInt32 = (uint)((_rawData[_readIndex + 3] << 24) |
                                 (_rawData[_readIndex + 2] << 16) |
                                 (_rawData[_readIndex + 1] << 8) |
                                 _rawData[_readIndex]);
        return readUInt32;
    }

    // 讀取ushort
    public ushort ReadUInt16()
    {
        if (Length < 2) return 0;
        // 以小端方式讀取Int16
        ushort readUInt16 = (ushort)((_rawData[_readIndex + 1] << 8) | _rawData[_readIndex]);
        _readIndex += 2;
        CheckAndReuseCapacity();
        return readUInt16;
    }

    // 讀取UInt32
    public uint ReadUInt32()
    {
        if (Length < 4) return 0;
        // 以小端方式讀取Int32
        uint readUInt32 = (uint)((_rawData[_readIndex + 3] << 24) |
                                 (_rawData[_readIndex + 2] << 16) |
                                 (_rawData[_readIndex + 1] << 8) |
                                 _rawData[_readIndex]);
        _readIndex += 4;
        CheckAndReuseCapacity();
        return readUInt32;
    }

    // 輸出緩衝區
    public override string ToString()
    {
        return BitConverter.ToString(_rawData, _readIndex, Length);
    }

    // Debug
    public string Debug()
    {
        return string.Format("_readIndex({0}) _writeIndex({1}) _data({2})",
            _readIndex,
            _writeIndex,
            BitConverter.ToString(_rawData, 0, _capacity)
        );
    }
}