FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Payment.ReadApi/Payment.ReadApi.csproj", "Payment.ReadApi/"]
RUN dotnet restore "Payment.ReadApi/Payment.ReadApi.csproj"
COPY . .
WORKDIR "/src/Payment.ReadApi"
RUN dotnet build "Payment.ReadApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.ReadApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.ReadApi.dll"]