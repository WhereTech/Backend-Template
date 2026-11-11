FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["BackendTemplate/BackendTemplate.csproj", "BackendTemplate/"]
RUN dotnet restore "BackendTemplate/BackendTemplate.csproj"

COPY . .
WORKDIR "/src/BackendTemplate"

RUN dotnet publish "BackendTemplate.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# 设定时区，统一为GMT+8
RUN sed -i 's#https\?://dl-cdn.alpinelinux.org/alpine#https://mirrors.tuna.tsinghua.edu.cn/alpine#g' /etc/apk/repositories && \
    apk add --no-cache icu-libs tzdata &&  \
    cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime &&  \
    echo "Asia/Shanghai" > /etc/timezone

ENV ASPNETCORE_ENVIRONMENT=Release

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BackendTemplate.dll", "--configFile", "/etc/backend-template/appsettings.json"]
