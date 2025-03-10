// using System.Net.Sockets;
//
// namespace Core.Network;
//
// /// <summary>
// /// 封裝 NetworkListener NetworkConnector 共用方法
// /// </summary>
// [Obsolete]
// public abstract class NetworkBase
// {
//     public Action<NetworkCommunicator> OnConnected;
//
//     public Action<ReceivedMessageInfo> OnReceivedMessage;
//
//     public Action<Socket> OnClosed;
//
//     protected ByteBufferPool _byteBufferPool;
//
//     public NetworkBase()
//     {
//         _byteBufferPool = new ByteBufferPool();
//     }
//
//     #region - Receive -
//     
//     protected abstract void OnReceive(object sender, SocketAsyncEventArgs args);
//     
//     protected void ReceiveAsync(SocketAsyncEventArgs args)
//     {
//         try
//         {
//             var socket = args.AcceptSocket;
//             if (socket == null)
//             {
//                 Logger.Log.Error("Receive Failed, client socket is null");
//                 return;
//             }
//             if (!socket.ReceiveAsync(args))
//             {
//                 OnReceive(this, args);
//             }
//         }
//         catch (Exception e)
//         {
//             Logger.Log.Error(e.ToString());
//         }
//     }
//     
//     protected bool ReadDataToBuffer(SocketAsyncEventArgs args, ByteBuffer readBuffer)
//     {
//         var receiveCount = args.BytesTransferred;
//         var isNotSuccess = args.SocketError != SocketError.Success;
//
//         if (receiveCount <= 0 || isNotSuccess)
//         {
//             return false;
//         }
//
//         readBuffer.Write(args.Buffer, args.Offset, receiveCount);
//
//         return true;
//     }
//     
//     #endregion
//
//     #region - Send -
//
//     protected abstract void OnSend(object sender, SocketAsyncEventArgs args);
//     
//     private void SendAsync(SocketAsyncEventArgs args)
//     {
//         try
//         {
//             var targetSocket = args.AcceptSocket;
//             if (targetSocket == null)
//             {
//                 Logger.Log.Error("Send Failed, client socket is null");
//                 return;
//             }
//             if (!targetSocket.SendAsync(args))
//             {
//                 OnSend(this, args);
//             }
//         }
//         catch (Exception e)
//         {
//             Logger.Log.Error(e.ToString());
//         }
//     }
//     
//     protected void AddMessageToSendQueue(uint messageId, uint stateCode, byte[] message, Queue<ByteBuffer> sendQueue, SocketAsyncEventArgs args)
//     {
//         // 打包資料
//         var byteBuffer = _byteBufferPool.Rent(2 + 2 + 4 + message.Length);
//         PackMessage(byteBuffer, messageId, stateCode, message);
//
//         // 透過 SendQueue處理發送不完整問題
//         int count = 0;
//         lock (sendQueue)
//         {
//             sendQueue.Enqueue(byteBuffer);
//             count = sendQueue.Count;
//         }
//
//         // 當 SendQueue只有 1個時發送
//         // SendQueue.Count > 1時, 在 OnSend()裡面會持續發送, 直到發送完
//         if (count == 1)
//         {
//             var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
//             args.SetBuffer(args.Offset, copyCount);
//             Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, copyCount);
//             SendAsync(args);
//         }
//     }
//     
//     protected void CheckSendQueue(SocketAsyncEventArgs args, Queue<ByteBuffer> sendQueue)
//     {
//         var count = args.BytesTransferred;
//
//         ByteBuffer byteBuffer;
//         lock (sendQueue)
//         {
//             byteBuffer = sendQueue.First();
//         }
//
//         byteBuffer.SetReadIndex(byteBuffer.ReadIndex + count);
//         
//         // 完整發送完一個ByteBuffer的資料
//         if (byteBuffer.Length <= 0)
//         {
//             ByteBuffer dequeueBuffer;
//             lock (sendQueue)
//             {
//                 dequeueBuffer = sendQueue.Dequeue();
//                 if (sendQueue.Count >= 1)
//                 {
//                     byteBuffer = sendQueue.First();
//                 }
//                 else
//                 {
//                     byteBuffer = null;
//                 }
//             }
//             _byteBufferPool.Return(dequeueBuffer);
//         }
//
//         if (byteBuffer != null)
//         {
//             // SendQueue還有資料，繼續發送
//             var copyCount = Math.Min(byteBuffer.Length, NetworkConfig.BufferSize);
//             args.SetBuffer(args.Offset, copyCount);
//             Array.Copy(byteBuffer.RawData, byteBuffer.ReadIndex, args.Buffer, args.Offset, copyCount);
//             SendAsync(args);
//         }
//     }
//
//     #endregion
//
//     #region - Message -
//
//     /// <summary>
//     /// | 總長度 2 Byte | MessageId 4 Byte | State Code 4 Byte | 資料內容 x Byte |
//     /// </summary>
//     protected static bool TryUnpackMessage(ByteBuffer byteBuffer, out ReceivedMessageInfo receivedMessageInfo)
//     {
//         receivedMessageInfo = new ReceivedMessageInfo();
//
//         // 連表示總長度的 2 Byte都沒收到
//         if (byteBuffer.Length <= 2) return false;
//         
//         // 資料不完整
//         var totalLength = byteBuffer.CheckUInt16();
//         if (byteBuffer.Length < totalLength) return false;
//
//         // 資料完整，開始解析
//         totalLength = byteBuffer.ReadUInt16();
//         var bodyLength = totalLength - 2 - 4 - 4;
//         
//         receivedMessageInfo.MessageLength = bodyLength;
//         receivedMessageInfo.MessageId     = byteBuffer.ReadUInt16();
//         //receivedMessageInfo.StateCode     = byteBuffer.ReadUInt32();
//         receivedMessageInfo.Allocate(totalLength);
//         byteBuffer.Read(receivedMessageInfo.Message, 0, bodyLength);
//
//         return true;
//     }
//
//     private static void PackMessage(ByteBuffer byteBuffer, uint messageId, uint stateCode, byte[] message)
//     {
//         var totalLength = message.Length + 2 + 4 + 4;
//         byteBuffer.WriteUInt16((ushort)totalLength);
//         byteBuffer.WriteUInt32(messageId);
//         byteBuffer.WriteUInt32(stateCode);
//         byteBuffer.Write(message, 0, message.Length);
//     }
//
//     #endregion
// }