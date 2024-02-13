## ToDo

#### 必做
- 短時間內送太多封包的Session要踢掉(50%)
- MySqlConnect同時併發Open()的話會報錯，待處理
- Server讀配置
- Repository
- ReceivedMessageInfo的Message從byte[]改成ByteBuffer

#### 有空再做
- 重構NetworkSession的SessionObject
- 重構Response, RequestInfo, ReceivedMessageInfo這些struct