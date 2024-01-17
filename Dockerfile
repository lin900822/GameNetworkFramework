# 階段ㄧ：構建用於Build .net App的Image
# 使用官方的.NET SDK映像作為基礎映像
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
# 設定工作目錄
WORKDIR /app
# 複製所有專案文件和整個專案代碼
COPY . .
# 還原依賴項
RUN dotnet restore
# 執行 dotnet publish
RUN dotnet publish -c Release -o out

# 階段二：構建Runtime使用的Image,將上階段Build好的App拿來用,目的是讓Image更輕量
# 使用較小的運行時映像
FROM mcr.microsoft.com/dotnet/runtime:6.0
# 設定工作目錄
WORKDIR /app
# 從前一階段的構建環境中複製應用程序的輸出
COPY --from=build-env /app/out .
# 指定應用程序的入口點
ENTRYPOINT ["./ServerDemo"]
