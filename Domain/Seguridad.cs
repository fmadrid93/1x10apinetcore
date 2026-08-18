using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Domain
{
    public class Seguridad
    {
        public string GeneraClaveSHA1(string txt)
        {
            Byte[] data = System.Text.Encoding.Unicode.GetBytes(txt);
            Byte[] result;

            SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
            result = sha.ComputeHash(data);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length - 1; i++)
            {
                if (result[i] < 16)
                {
                    sb.Append("0");
                }
                else
                {
                    sb.Append(result[i].ToString("x"));
                }
            }
            return sb.ToString();
        }

    }
}
