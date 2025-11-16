# ----- Giai đoạn 1: Build -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file .sln và các file .csproj để tối ưu cache layer
# Chỉ restore khi file project thay đổi
COPY CB-Gift.sln .
COPY CB-Gift/CB-Gift.csproj ./CB-Gift/
COPY CBGift.Tests/CBGift.Tests.csproj ./CBGift.Tests/
# (Nếu bạn có dự án .csproj khác, hãy COPY chúng ở đây)

# Restore dependencies cho toàn bộ solution
RUN dotnet restore "CB-Gift.sln"

# Sao chép toàn bộ source code
# Dấu "." này là thư mục gốc của context (C:\...\SEP490\CB-Gift)
COPY . .

# Build và publish dự án API chính
WORKDIR "/src/CB-Gift"
RUN dotnet publish "CB-Gift.csproj" -c Release -o /app/publish

# ----- Giai đoạn 2: Runtime -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "CB-Gift.dll"]