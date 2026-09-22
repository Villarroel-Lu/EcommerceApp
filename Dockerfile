# ===== Etapa 1: compilar la app =====
# Usamos la imagen del SDK (pesada, tiene el compilador) solo para construir.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero solo el .csproj y restauramos paquetes NuGet.
# Truco de Docker: si no cambiaste dependencias, esta capa queda en
# caché y los siguientes builds son mucho más rápidos.
COPY *.csproj ./
RUN dotnet restore

# Ahora sí copiamos todo el código y compilamos en modo Release.
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ===== Etapa 2: imagen final =====
# Esta imagen es mucho más liviana: solo trae el runtime necesario
# para EJECUTAR la app, no el compilador completo.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# QuestPDF (los reportes en PDF) dibuja el texto usando fuentes reales del
# sistema operativo. La imagen de ASP.NET Core, por defecto, no trae NINGUNA
# fuente instalada: sin esto, generar un PDF en Render fallaría con un error
# (algo como "no se pudo dibujar el texto, no hay fuentes registradas").
# "fonts-dejavu-core" es una fuente libre con tildes y ñ (importante para
# textos en español); "libfontconfig1" es la librería que permite al sistema
# ENCONTRAR las fuentes instaladas.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Render nos asigna un puerto real en la variable de entorno PORT.
# Si no existe (por ejemplo corriendo local), usamos 10000 por defecto.
ENTRYPOINT ["sh", "-c", "dotnet EcommerceApp.dll --urls http://0.0.0.0:${PORT:-10000}"]