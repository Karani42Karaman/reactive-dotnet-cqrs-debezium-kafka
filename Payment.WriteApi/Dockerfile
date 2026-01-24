FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Payment.WriteApi/Payment.WriteApi.csproj", "Payment.WriteApi/"]
RUN dotnet restore "Payment.WriteApi/Payment.WriteApi.csproj"
COPY . .
WORKDIR "/src/Payment.WriteApi"
RUN dotnet build "Payment.WriteApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.WriteApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.WriteApi.dll"]