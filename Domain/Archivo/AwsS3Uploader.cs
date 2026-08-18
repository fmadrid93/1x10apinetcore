using Amazon;
using Amazon.S3;

namespace Domain.Archivo
{
    public class AwsS3Uploader : S3UploaderBase
    {
        public AwsS3Uploader() : base(
            bucketName: "datatrends012025",
            accessKey: "AKIA6JR6HAJHM4XQGYWZ",//Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
            secretKey: "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",//Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
            config: new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1
            })
        { }
    }
}
