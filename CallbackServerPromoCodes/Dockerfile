FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CallbackServerPromoCodes/CallbackServerPromoCodes.csproj", "CallbackServerPromoCodes/"]
RUN dotnet restore "CallbackServerPromoCodes/CallbackServerPromoCodes.csproj"
COPY . .
WORKDIR "/src/CallbackServerPromoCodes"
RUN dotnet build "CallbackServerPromoCodes.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CallbackServerPromoCodes.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CallbackServerPromoCodes.dll"]
