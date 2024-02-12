## 封包定義

### 短封包:
| 總長度 2 Byte | MessageId 4 Byte | 資料內容 x Byte |

### 長封包:
| 總長度 4 Byte | MessageId 4 Byte | 資料內容 x Byte |

### Request:
| 總長度 4 Byte | RequestId 2 Byte | MessageId 4 Byte | 資料內容 x Byte |

### 說明:
- 長短封包由總長度的第一個Bit定義       1XXXXXXX XXXXXXXX
- 是否是Request由總長度的第二個Bit定義  X1XXXXXX XXXXXXXX
- MessageId: 0~65535
- RequestId: 0~65535 (系統自動生成)