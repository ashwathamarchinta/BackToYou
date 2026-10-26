# ─── Stage 1: Build ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Restore dependencies first (layer cache optimisation)
COPY BackToYou.csproj ./
RUN dotnet restore BackToYou.csproj

# Copy everything else and publish
COPY . ./
RUN dotnet publish BackToYou.csproj \
      --configuration Release \
      --output /app/publish \
      --no-restore

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create a non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Copy published application
COPY --from=build --chown=appuser:appuser /app/publish ./

# Prepare upload folders and ensure appuser ownership
RUN mkdir -p /app/wwwroot/uploads/items /app/wwwroot/uploads/proofs && \
    chown -R appuser:appuser /app

USER appuser

# Render injects PORT; default to 8080 if not set
ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "BackToYou.dll"]
