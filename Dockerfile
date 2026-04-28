# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY global.json ./
COPY src/BallouBot.Core/BallouBot.Core.csproj src/BallouBot.Core/
COPY src/BallouBot.Data/BallouBot.Data.csproj src/BallouBot.Data/
COPY src/BallouBot.Host/BallouBot.Host.csproj src/BallouBot.Host/
COPY src/modules/BallouBot.Modules.Welcome/BallouBot.Modules.Welcome.csproj src/modules/BallouBot.Modules.Welcome/
COPY src/modules/BallouBot.Modules.RandomRichard/BallouBot.Modules.RandomRichard.csproj src/modules/BallouBot.Modules.RandomRichard/
COPY src/modules/BallouBot.Modules.Gif/BallouBot.Modules.Gif.csproj src/modules/BallouBot.Modules.Gif/
COPY src/modules/BallouBot.Modules.FactCheck/BallouBot.Modules.FactCheck.csproj src/modules/BallouBot.Modules.FactCheck/
COPY src/modules/BallouBot.Modules.Help/BallouBot.Modules.Help.csproj src/modules/BallouBot.Modules.Help/
COPY src/modules/BallouBot.Modules.ErrorNotify/BallouBot.Modules.ErrorNotify.csproj src/modules/BallouBot.Modules.ErrorNotify/

# Restore dependencies (Host project pulls in all src dependencies)
RUN dotnet restore src/BallouBot.Host/BallouBot.Host.csproj

# Copy everything else and build
COPY src/ src/
RUN dotnet publish src/BallouBot.Host/BallouBot.Host.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs

# Default environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/balloubot.db"

ENTRYPOINT ["dotnet", "BallouBot.Host.dll"]
