# ===================================================================
# Dockerfile — RadioAficionado.Servicio
# Multi-stage build: SDK para compilar, ASP.NET runtime para ejecutar
# ===================================================================

# Etapa 1: Compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copiar archivos de proyecto para aprovechar cache de capas en restore
COPY RadioAficionado.slnx .
COPY src/RadioAficionado.Dominio/RadioAficionado.Dominio.csproj src/RadioAficionado.Dominio/
COPY src/RadioAficionado.Aplicacion/RadioAficionado.Aplicacion.csproj src/RadioAficionado.Aplicacion/
COPY src/RadioAficionado.Compartido/RadioAficionado.Compartido.csproj src/RadioAficionado.Compartido/
COPY src/RadioAficionado.Infraestructura/RadioAficionado.Infraestructura.csproj src/RadioAficionado.Infraestructura/
COPY src/RadioAficionado.Infraestructura.Sqlite/RadioAficionado.Infraestructura.Sqlite.csproj src/RadioAficionado.Infraestructura.Sqlite/
COPY src/RadioAficionado.Infraestructura.Postgres/RadioAficionado.Infraestructura.Postgres.csproj src/RadioAficionado.Infraestructura.Postgres/
COPY src/RadioAficionado.Nativo.Dsp/RadioAficionado.Nativo.Dsp.csproj src/RadioAficionado.Nativo.Dsp/
COPY src/RadioAficionado.Nativo.ModosDigitales/RadioAficionado.Nativo.ModosDigitales.csproj src/RadioAficionado.Nativo.ModosDigitales/
COPY src/RadioAficionado.Nativo.Rotador/RadioAficionado.Nativo.Rotador.csproj src/RadioAficionado.Nativo.Rotador/
COPY src/RadioAficionado.Nativo.Audio/RadioAficionado.Nativo.Audio.csproj src/RadioAficionado.Nativo.Audio/
COPY src/RadioAficionado.Nativo.Rig/RadioAficionado.Nativo.Rig.csproj src/RadioAficionado.Nativo.Rig/
COPY src/RadioAficionado.Nativo.Sdr/RadioAficionado.Nativo.Sdr.csproj src/RadioAficionado.Nativo.Sdr/
COPY src/RadioAficionado.IA/RadioAficionado.IA.csproj src/RadioAficionado.IA/
COPY src/RadioAficionado.Servicio/RadioAficionado.Servicio.csproj src/RadioAficionado.Servicio/

# Restaurar dependencias (capa cacheada si los csproj no cambian)
RUN dotnet restore src/RadioAficionado.Servicio/RadioAficionado.Servicio.csproj

# Copiar todo el codigo fuente
COPY src/ src/

# Publicar en modo Release
RUN dotnet publish src/RadioAficionado.Servicio/RadioAficionado.Servicio.csproj \
    -c Release -o /app/publish --no-restore

# Etapa 2: Imagen final ligera
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 5200

ENV ASPNETCORE_URLS=http://+:5200
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Ejecutar como usuario no-root (UID predefinido en la imagen aspnet) para reducir
# el impacto de una eventual ejecucion remota de codigo.
USER $APP_UID

ENTRYPOINT ["dotnet", "RadioAficionado.Servicio.dll"]
