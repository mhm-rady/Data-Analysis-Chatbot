FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY SsasMcpServer.Core/*.csproj ./SsasMcpServer.Core/
COPY SsasMcpServer.Models/*.csproj ./SsasMcpServer.Models/
COPY SsasMcpServer.Services/*.csproj ./SsasMcpServer.Services/
COPY SsasMcpServer.Tools/*.csproj ./SsasMcpServer.Tools/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build
WORKDIR /src/SsasMcpServer.Core
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "SsasMcpServer.Core.dll"]