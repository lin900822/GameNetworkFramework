namespace Network.TCP;

public class MessageUtils
{
    /// <summary>
    /// | 總長度 2 Byte | MessageId 2 Byte | 資料內容 x Byte |
    /// </summary>
    public static bool TryParse(ByteBuffer byteBuffer, out UInt16 outMessageId, out byte[] outMessage)
    {
        outMessageId = 0;
        outMessage = null;
        
        // 連表示總長度的 2 Byte都沒收到
        if (byteBuffer.Length <= 2) return false;
        
        // 資料不完整
        var totalLength = byteBuffer.CheckUInt16();
        if (byteBuffer.Length < totalLength) return false;

        // 資料完整，開始解析
        totalLength = byteBuffer.ReadUInt16();
        outMessageId = byteBuffer.ReadUInt16();

        var bodyLength = totalLength - 2 - 2;

        outMessage = new byte[bodyLength];
        byteBuffer.Read(outMessage, 0, bodyLength);

        return true;
    }

    public static void SetMessage(ByteBuffer byteBuffer, UInt16 messageId, byte[] message)
    {
        var totalLength = message.Length + 2 + 2;
        byteBuffer.WriteUInt16((ushort)totalLength);
        byteBuffer.WriteUInt16(messageId);
        byteBuffer.Write(message, 0, message.Length);
    }
}