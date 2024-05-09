## ToDo
- SendRequest要改成不論是否成功都要觸發Callback
- MySqlConnect同時併發Open()的話會報錯，待處理 (改為每次都 new MySqlConnection)
- 如果需要Lock有多個await的片段 可以用 SemaphoreSlim
- Server讀配置
- Repository
- 改用SqlSugar
- 重構RequestInfo
- ReceivedMessageInfo改為ByteBuffer
