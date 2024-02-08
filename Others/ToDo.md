## ToDo

#### 必做
- MySqlConnect同時併發Open()的話會報錯，待處理
- ClientBase HeartBeat 斷線重連
- ServerBase短時間內收到太多同個Session的封包要踢掉

#### 有空再做
- 一個Frame處理多個Message, TimeOut Request
- 重構NetworkSession的SessionObject
- 重構Response, RequestInfo, ReceivedMessageInfo這些struct