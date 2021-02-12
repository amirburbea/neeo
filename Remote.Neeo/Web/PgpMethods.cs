using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;

namespace Remote.Neeo.Web
{
    internal static class PgpMethods
    {
        public static Stream Decrypt(Stream inputStream, PgpPrivateKey privateKey)
        {
            using Stream decoderStream = PgpUtilities.GetDecoderStream(inputStream);
            PgpObjectFactory decoderFactory = new(decoderStream);
            PgpEncryptedDataList dataList = decoderFactory.NextPgpObject() as PgpEncryptedDataList ?? (PgpEncryptedDataList)decoderFactory.NextPgpObject();
            PgpPublicKeyEncryptedData data = dataList.GetEncryptedDataObjects().Cast<PgpPublicKeyEncryptedData>().First();
            using Stream dataStream = data.GetDataStream(privateKey);
            PgpObjectFactory dataFactory = new(dataStream);
            if (dataFactory.NextPgpObject() is not PgpCompressedData compressedData)
            {
                throw new Exception();
            }
            using Stream compressedStream = compressedData.GetDataStream();
            PgpObjectFactory factory = new(compressedStream);
            PgpLiteralData literal = factory.NextPgpObject() as PgpLiteralData ?? (PgpLiteralData)factory.NextPgpObject();
            MemoryStream output = new();
            using (Stream input = literal.GetInputStream())
            {
                Streams.PipeAll(input, output);
            }
            output.Seek(0L, SeekOrigin.Begin);
            return output;
        }

        public static (PgpPrivateKey, PgpPublicKey) GenerateKeys()
        {
            RsaKeyPairGenerator kpg = new();
            kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new(), 768, 8));
            AsymmetricCipherKeyPair pair = kpg.GenerateKeyPair();
            SecureRandom random = new();
            byte[] bytes = new byte[64];
            random.NextBytes(bytes);
            char[] passphrase = Encoding.ASCII.GetChars(bytes);
            PgpSecretKey secretKey = new(
                PgpSignature.DefaultCertification,
                PublicKeyAlgorithmTag.RsaGeneral,
                pair.Public,
                pair.Private,
                DateTime.Now,
                JsonSerializer.Serialize(new { Name = "NEEO-SDK", Email = "neeo-sdk@neeo.com" }, new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                SymmetricKeyAlgorithmTag.Aes256,
                passphrase,
                default,
                default,
                random
            );
            return (secretKey.ExtractPrivateKey(passphrase), secretKey.PublicKey);
        }

        public static byte[] GetKeyBytes(Action<Stream> encode)
        {
            using MemoryStream output = new();
            using (ArmoredOutputStream target = new ArmoredOutputStream(output))
            {
                encode(target);
            }
            return output.ToArray();
        }

        /*
        public static PgpPublicKey GetNeeoKey()
        {
            HttpClient c = new HttpClient();
            var q = c.GetStringAsync("http://localhost:6336/secure/pubkey").Result;
            var pubKey = new PgpPublicKeyRing(new ArmoredInputStream(new MemoryStream(Encoding.ASCII.GetBytes(JsonSerializer.Deserialize<Dictionary<string, string>>(q)!["publickey"])))).GetPublicKey();
            var p = (RsaKeyParameters)pubKey.GetKey();
            // throw new Exception();
            return pubKey;
        }

        private static PgpPrivateKey GetPrivateKey(string privateKeyPath)
        {
            using Stream keyIn = File.OpenRead(privateKeyPath);
            using Stream inputStream = Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.GetDecoderStream(keyIn);
            PgpSecretKeyRingBundle secretKeyRingBundle = new PgpSecretKeyRingBundle(inputStream);

            foreach (PgpSecretKeyRing kRing in secretKeyRingBundle.GetKeyRings())
            {
                foreach (PgpSecretKey secretKey in kRing.GetSecretKeys())
                {
                    PgpPrivateKey privKey = secretKey.ExtractPrivateKey("1234567890".ToCharArray());

                    if (privKey.Key is ElGamalPrivateKeyParameters)
                    //Org.BouncyCastle.Crypto.Parameters.ElGamalPrivateKeyParameters
                    {
                        return privKey;
                    }
                }
            }
            throw new Exception();
        }*/
    }
}
