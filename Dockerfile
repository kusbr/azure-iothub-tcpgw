FROM mcr.microsoft.com/dotnet/core/runtime:2.1-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src

COPY SocketIoT/*.csproj ./SocketIoT/
COPY SocketIoT.Bootstrapper/*.csproj ./SocketIoT.Bootstrapper/
COPY SocketIoT.Core.Contracts/*.csproj ./SocketIoT.Core.Contracts/
COPY SocketIoT.Core.Tcp/*.csproj ./SocketIoT.Core.Tcp/
COPY SocketIoT.IoTHubProvider/*.csproj ./SocketIoT.IoTHubProvider/
COPY SocketIoT.Tenancy/*.csproj ./SocketIoT.Tenancy/

RUN dotnet restore "SocketIoT/SocketIoT.csproj"

COPY . .
WORKDIR "/src/SocketIoT"
RUN dotnet build "SocketIoT.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SocketIoT.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

COPY SocketIoT/*.json /app

EXPOSE 12000/tcp
EXPOSE 12001/tcp

ENTRYPOINT ["dotnet", "SocketIoT.dll"]
