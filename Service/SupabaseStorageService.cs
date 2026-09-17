using System.Net.Http.Headers;

namespace EcommerceApp.Services
{
    // Un solo servicio reutilizable para subir CUALQUIER archivo
    // (fotos de mascotas, fotos de carnet, etc.) a Supabase Storage.
    // Se inyecta en los controladores que lo necesiten.
    public class SupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;

        public SupabaseStorageService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _supabaseUrl = configuration["Supabase:Url"]
                ?? throw new InvalidOperationException("Falta Supabase:Url en appsettings.json");
            _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? throw new InvalidOperationException("Falta Supabase:ServiceRoleKey en appsettings.json");
        }

        // Sube un archivo (IFormFile) a un bucket de Supabase y devuelve
        // la URL pública para guardar en la base de datos.
        // bucket = "mascotas" o "carnets"
        // carpeta = subcarpeta dentro del bucket, para organizar (ej: "fotos")
        public async Task<string> SubirArchivoAsync(IFormFile archivo, string bucket, string carpeta)
        {
            // Nombre único (GUID) para que dos archivos nunca se sobrescriban
            var nombreArchivo = $"{carpeta}/{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            var urlSubida = $"{_supabaseUrl}/storage/v1/object/{bucket}/{nombreArchivo}";

            await using var stream = archivo.OpenReadStream();
            using var contenido = new StreamContent(stream);
            contenido.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrEmpty(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, urlSubida) { Content = contenido };
            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");

            var respuesta = await _httpClient.SendAsync(request);

            if (!respuesta.IsSuccessStatusCode)
            {
                var detalle = await respuesta.Content.ReadAsStringAsync();
                throw new Exception($"Error al subir archivo a Supabase Storage: {respuesta.StatusCode} - {detalle}");
            }

            // Esta es la URL pública, funciona porque marcamos el bucket
            // como "Public" al crearlo en el paso 1.
            return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{nombreArchivo}";
        }
    }
}