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
WORKDIR /app
COPY --from=build /app/publish .

# Render nos asigna un puerto real en la variable de entorno PORT.
# Si no existe (por ejemplo corriendo local), usamos 10000 por defecto.
ENTRYPOINT ["sh", "-c", "dotnet EcommerceApp.dll --urls http://0.0.0.0:${PORT:-10000}"]