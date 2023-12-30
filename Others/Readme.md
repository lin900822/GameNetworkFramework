## 封包定義
| 總長度 2 Byte | MessageId 4 Byte | StateCode 4 Byte | 資料內容 x Byte |

- 封包最大長度: 65535 Byte
- MessageId: 0 ~ 4,294,967,295
- StateCode: 0 ~ 4,294,967,295
- 通訊分為兩種
  - 單向的Message
  - 雙向的Request與Response

### Message
- Client送Message:
  - MessageId: 訊息Id 0 ~ int.MaxValue
  - StateCode: 無

- Server送Message:
  - MessageId: 訊息Id 0 ~ int.MaxValue
  - StateCode: 狀態碼 0 ~ int.MaxValue

### Request & Response
- Client送Request:
  - MessageId: 訊息Id 0 ~ int.MaxValue
  - StateCode: 請求流水號 (int.MaxValue + 1) ~ uint.MaxValue

- Server回Response:
  - MessageId: Client發上來的請求流水號 (int.MaxValue + 1) ~ uint.MaxValue
  - StateCode: 狀態碼 0 ~ int.MaxValue