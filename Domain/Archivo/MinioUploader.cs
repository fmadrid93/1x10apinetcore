using Amazon;
using Amazon.S3;
using Domain.Archivo;

namespace Domain.Archvo
{
    public class MinioUploader : S3UploaderBase
    {
        public MinioUploader() : base(
            bucketName: "datatrends012025",
            accessKey: "fabricio",
            secretKey: "31032017",
            config: new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                ServiceURL = "http://minio:9000",
                //ServiceURL = "http://localhost:9000",
                ForcePathStyle = true
            })
        { }
    }
}
