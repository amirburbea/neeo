using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using Moq.Protected;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Xunit;

namespace Neeo.Sdk.Tests.Devices.Features;


// TODO: Implement registration feature tests.
/*
public class RegistrationFeatureTests
{
    [Fact]
    public void GetPublicKey()
    {
        var keyPair = PgpKeyPairGenerator.CreatePgpKeys(
            nameof(RegistrationFeatureTests),
            () => new[] { byte.MinValue },
            new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            () => new ZeroRandom()
        );
        PgpPublicKeyResponse response = new(keyPair);
        throw new(response.PublicKey);
    }

    private class ZeroRandom : SecureRandom
    {
        public readonly Dictionary<string, int> _times = new();




        public override void NextBytes(byte[] buf)
        {
            this.NextBytes(buf, 0, buf.Length);
        }

        public override void NextBytes(byte[] buf, int off, int len)
        {
            int times = _times.GetValueOrDefault("NextBytes");
            for (; off < len; off++)
            {
                buf[off]++;
            }
            _times["NextBytes"] = times + 1;

            PgpUtilities.GetDecoderStream(inputStream);
            PgpPublicKeyRingBundle pgpPub = new PgpPublicKeyRingBundle(inputStream);


            PgpPublicKey key = new(PublicKeyAlgorithmTag.RsaGeneral,
            //for (; off < len; off++)
            //{
            //    buf[off] = (byte)off;
            //}

            // base.NextBytes(buf, off, len);


            IAsymmetricCipherKeyPairGenerator generator
    = GeneratorUtilities.GetKeyPairGenerator("RSA");
            generator.Init(keyRingParams.RsaParams);

           
        }




        public override int Next()
        {
            return this.Next(0, int.MaxValue);
        }

        public override double NextDouble()
        {
            string key = MethodBase.GetCurrentMethod()!.ToString()!;
            int times = _times.GetValueOrDefault(key);
            return _times[key] = times + 1;
        }

        public override int Next(int maxValue)
        {
            return this.Next(int.MinValue, maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            string key = MethodBase.GetCurrentMethod()!.ToString()!;
            int times = _times.GetValueOrDefault(key);
            return Math.Min(Math.Max(_times[key] = times + 1, minValue), maxValue);
        }

        public override long NextInt64()
        {
            return this.NextInt64(long.MaxValue);
        }

        public override long NextInt64(long maxValue)
        {
            return this.NextInt64(long.MinValue, maxValue);
        }

        public override long NextInt64(long minValue, long maxValue)
        {
            return maxValue;
        }

        public override int NextInt()
        {
            return this.Next();
        }

        public override long NextLong()
        {
            return this.NextInt64();
        }

        public override float NextSingle()
        {
            return float.MaxValue;
        }

        protected override double Sample()
        {
            return double.MaxValue;
        }
    }

    private static SecureRandom CreateSecureRandom()
    {
        int count = 0;
        Mock<SecureRandom> mock = new(MockBehavior.Strict);
        mock.Setup(random => random.SetSeed(It.IsAny<byte[]>()));
        mock.Setup(random => random.NextBytes(It.IsAny<byte[]>()));
        mock.Setup(random => random.Next()).Returns(() => (int)Math.Pow(count++, 2));

        return mock.Object;
    }

    private interface SecureRandomProtectedMethods
    {
        void SetSeed(byte[] seed);
    }
}
*/