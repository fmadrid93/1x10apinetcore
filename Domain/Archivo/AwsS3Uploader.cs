using Amazon;
using Amazon.S3;

namespace Domain.Archivo
{
    public class AwsS3Uploader : S3UploaderBase
    {
        public AwsS3Uploader() : base(
            bucketName: "",
            accessKey: "",//Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
            secretKey: "",//Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
            config: new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1
            })
        { }
    }
}
