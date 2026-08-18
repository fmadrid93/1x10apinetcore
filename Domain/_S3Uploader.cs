using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon;

namespace Domain
{
    public class _S3Uploader
    {
        public readonly string _bucketName = "datatrends012025";
        private readonly string _accessKey = "fabricio";  // o el de AWS
        private readonly string _secretKey = "31032017";  // o el de AWS
        private readonly string _serviceURL = "http://127.0.0.1:9000"; // Cambia a null para AWS
        private readonly RegionEndpoint _region = RegionEndpoint.USEast1;

        private IAmazonS3 _s3Client;

        public _S3Uploader()
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = _region,
                ForcePathStyle = true, // Requerido por MinIO
                ServiceURL = _serviceURL, // Quita esta línea para usar AWS S3
            };

            _s3Client = new AmazonS3Client(_accessKey, _secretKey, config);
        }
        /// <summary>
        /// Sube un archivo de bytes a MinIO/S3 organizándolo por fecha y usuario
        /// </summary>
        public async Task<bool> SubirBytesAsync(byte[] archivo, string usuarioId, string nombreArchivo)
        {
            try
            {
                // Construir la estructura: grabaciones/yyyy-MM-dd/usuarioId/nombreArchivo
                string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
           //     string nombreArchivo = $"{nombreArchivoBase}_{DateTime.UtcNow:HHmmss}.mp3";
                string nombreEnBucket = $"grabaciones/{fecha}/{usuarioId}/{nombreArchivo}";

                using var stream = new MemoryStream(archivo);

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = nombreEnBucket,
                    InputStream = stream,
                    ContentType = "audio/mpeg",
                    AutoCloseStream = true
                };

                var response = await _s3Client.PutObjectAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"✅ Archivo '{nombreEnBucket}' subido con éxito. ETag: {response.ETag}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Falló la subida de '{nombreEnBucket}'. Status: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"❌ AmazonS3Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                return false;
            }
        }



        public async Task DescargarArchivoAsync(string nombreEnBucket, string destinoLocal)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = nombreEnBucket
            };

            using var response = await _s3Client.GetObjectAsync(request);
            await response.WriteResponseStreamToFileAsync(destinoLocal, false, default);
            Console.WriteLine($"📥 Archivo '{nombreEnBucket}' descargado a '{destinoLocal}'.");
        }
        // ✅ Obtener archivo como byte[]
        public async Task<byte[]> ObtenerArchivoAsync(string server, string path, string nombreEnBucket)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = server,
                    Key = path+ nombreEnBucket
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);

                Console.WriteLine($"📤 Archivo '{nombreEnBucket}' cargado desde S3.");
                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"❌ Archivo '{nombreEnBucket}' no encontrado en el bucket.");
                return null; // o throw si prefieres propagar la excepción
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener archivo '{nombreEnBucket}': {ex.Message}");
                throw;
            }
        }

    }
}
