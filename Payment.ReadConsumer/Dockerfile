FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Payment.ReadConsumer/Payment.ReadConsumer.csproj", "Payment.ReadConsumer/"]
RUN dotnet restore "Payment.ReadConsumer/Payment.ReadConsumer.csproj"
COPY . .
WORKDIR "/src/Payment.ReadConsumer"
RUN dotnet build "Payment.ReadConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.ReadConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.ReadConsumer.dll"]