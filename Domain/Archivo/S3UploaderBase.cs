using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Domain.Archivo
{
    public abstract class S3UploaderBase
    {
        public readonly string _bucketName;
        protected readonly IAmazonS3 _s3Client;

        protected S3UploaderBase(string bucketName, string accessKey, string secretKey, AmazonS3Config config)
        {
            _bucketName = bucketName;
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }

        public async Task<bool> SubirBytesAsync(byte[] archivo, string usuarioId, string nombreArchivo)
        {
            try
            {
                string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
                //string fecha = DateTime.Now.ToString("yyyy-MM-dd");
               // var tz = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz"); // Linux normalmente
              //  var fecha = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).ToString("yyyy-MM-dd");
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

                Console.WriteLine(response.HttpStatusCode == System.Net.HttpStatusCode.OK
                    ? $"✅ Archivo '{nombreEnBucket}' subido con éxito."
                    : $"❌ Falló la subida de '{nombreEnBucket}'. Status: {response.HttpStatusCode}");

                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al subir archivo: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]> ObtenerArchivoAsync(string server, string path, string nombreEnBucket)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = server,
                    Key = path + nombreEnBucket
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
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener archivo '{nombreEnBucket}': {ex.Message}");
                throw;
            }
        }
        //    public async Task<Tuple<Stream, string>> ObtenerArchivoAsync(string server, string path, string nombreEnBucket)
        //    {
        //        try
        //        {
        //            var request = new GetObjectRequest
        //            {
        //                BucketName = server,
        //                Key = path + nombreEnBucket
        //            };

        //            using var response = await _s3Client.GetObjectAsync(request);
        //            //    using var memoryStream = new MemoryStream();
        //            //  await response.ResponseStream.CopyToAsync(memoryStream);
        //            var contentType = response.Headers.ContentType;
        //            //Console.WriteLine($"📤 Archivo '{nombreEnBucket}' cargado desde S3.");
        //            //return memoryStream.ToArray();
        //            if (string.IsNullOrWhiteSpace(contentType))
        //                contentType = "application/octet-stream";

        //            // ✅ Esto ayuda muchísimo a reproducción/seek
        //            return new Tuple<Stream, string>(response.ResponseStream, contentType);
        //            //return new FileStreamResult(response.ResponseStream, contentType)
        //            //{
        //            //    EnableRangeProcessing = true
        //            //};
        //        }
        //        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        //        {
        //            Console.WriteLine($"❌ Archivo '{nombreEnBucket}' no encontrado en el bucket.");
        //            return null;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ Error al obtener archivo '{nombreEnBucket}': {ex.Message}");
        //            throw;
        //        }        
        //}
    }
}
