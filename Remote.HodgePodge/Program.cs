using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Broadlink.RM;
using Neeo.Api;
using Neeo.Api.Devices;
using Neeo.Discovery;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Remote.HodgePodge;

//static class Program
//{
//    void Main()
//    {
//        String Password = "hello world!";
//        String Identity = "Slim Shady";

//        PgpKeyRingGenerator krgen = generateKeyRingGenerator(Identity, Password);

//        // Generate public key ring, dump to file.
//        PgpPublicKeyRing pkr = krgen.GeneratePublicKeyRing();
//        BufferedStream pubout = new BufferedStream(new FileStream(@"c:\temp\dummy.pkr", System.IO.FileMode.Create));
//        pkr.Encode(pubout);
//        pubout.Close();

//        // Generate private key, dump to file.
//        PgpSecretKeyRing skr = krgen.GenerateSecretKeyRing();
//        BufferedStream secout = new BufferedStream(new FileStream(@"c:\temp\dummy.skr", System.IO.FileMode.Create));
//        skr.Encode(secout);
//        secout.Close();

//    }

//    public static PgpKeyRingGenerator generateKeyRingGenerator(String identity, String password)
//    {

//        KeyRingParams keyRingParams = new KeyRingParams();
//        keyRingParams.Password = password;
//        keyRingParams.Identity = identity;
//        keyRingParams.PrivateKeyEncryptionAlgorithm = SymmetricKeyAlgorithmTag.Aes128;
//        keyRingParams.SymmetricAlgorithms = new SymmetricKeyAlgorithmTag[] {
//            SymmetricKeyAlgorithmTag.Aes256,
//            SymmetricKeyAlgorithmTag.Aes192,
//            SymmetricKeyAlgorithmTag.Aes128
//        };

//        keyRingParams.HashAlgorithms = new HashAlgorithmTag[] {
//            HashAlgorithmTag.Sha256,
//            HashAlgorithmTag.Sha1,
//            HashAlgorithmTag.Sha384,
//            HashAlgorithmTag.Sha512,
//            HashAlgorithmTag.Sha224,
//        };

//        IAsymmetricCipherKeyPairGenerator generator
//            = GeneratorUtilities.GetKeyPairGenerator("RSA");
//        generator.Init(keyRingParams.RsaParams);

//        /* Create the master (signing-only) key. */
//        PgpKeyPair masterKeyPair = new PgpKeyPair(
//            PublicKeyAlgorithmTag.RsaSign,
//            generator.GenerateKeyPair(),
//            DateTime.UtcNow);
//        Debug.WriteLine("Generated master key with ID "
//            + masterKeyPair.KeyId.ToString("X"));

//        PgpSignatureSubpacketGenerator masterSubpckGen
//            = new PgpSignatureSubpacketGenerator();
//        masterSubpckGen.SetKeyFlags(false, PgpKeyFlags.CanSign
//            | PgpKeyFlags.CanCertify);
//        masterSubpckGen.SetPreferredSymmetricAlgorithms(false,
//            (from a in keyRingParams.SymmetricAlgorithms
//             select (int)a).ToArray());
//        masterSubpckGen.SetPreferredHashAlgorithms(false,
//            (from a in keyRingParams.HashAlgorithms
//             select (int)a).ToArray());

//        /* Create a signing and encryption key for daily use. */
//        PgpKeyPair encKeyPair = new PgpKeyPair(
//            PublicKeyAlgorithmTag.RsaGeneral,
//            generator.GenerateKeyPair(),
//            DateTime.UtcNow);
//        Debug.WriteLine("Generated encryption key with ID "
//            + encKeyPair.KeyId.ToString("X"));

//        PgpSignatureSubpacketGenerator encSubpckGen = new PgpSignatureSubpacketGenerator();
//        encSubpckGen.SetKeyFlags(false, PgpKeyFlags.CanEncryptCommunications | PgpKeyFlags.CanEncryptStorage);

//        masterSubpckGen.SetPreferredSymmetricAlgorithms(false,
//            (from a in keyRingParams.SymmetricAlgorithms
//             select (int)a).ToArray());
//        masterSubpckGen.SetPreferredHashAlgorithms(false,
//            (from a in keyRingParams.HashAlgorithms
//             select (int)a).ToArray());

//        /* Create the key ring. */
//        PgpKeyRingGenerator keyRingGen = new PgpKeyRingGenerator(
//            PgpSignature.DefaultCertification,
//            masterKeyPair,
//            keyRingParams.Identity,
//            keyRingParams.PrivateKeyEncryptionAlgorithm.Value,
//            keyRingParams.GetPassword(),
//            true,
//            masterSubpckGen.Generate(),
//            null,
//            new SecureRandom());

//        /* Add encryption subkey. */
//        keyRingGen.AddSubKey(encKeyPair, encSubpckGen.Generate(), null);

//        return keyRingGen;

//    }

//    // Define other methods and classes here
//    class KeyRingParams
//    {

//        public SymmetricKeyAlgorithmTag? PrivateKeyEncryptionAlgorithm { get; set; }
//        public SymmetricKeyAlgorithmTag[] SymmetricAlgorithms { get; set; }
//        public HashAlgorithmTag[] HashAlgorithms { get; set; }
//        public RsaKeyGenerationParameters RsaParams { get; set; }
//        public string Identity { get; set; }
//        public string Password { get; set; }
//        //= EncryptionAlgorithm.NULL;

//        public char[] GetPassword()
//        {
//            return Password.ToCharArray();
//        }

//        public KeyRingParams()
//        {
//            //Org.BouncyCastle.Crypto.Tls.EncryptionAlgorithm
//            RsaParams = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 12);
//        }

//    }
//}

internal static class Program
{
    private static readonly Regex _ipAddressRegex = new(@"^\d+\.\d+\.\d+\.\d+$");

    private static async Task LearnCodes(RMDevice device)
    {
        string? fileName = Program.QueryFileName();
        if (fileName == null)
        {
            return;
        }
        Dictionary<string, string> dictionary = new();
        while (true)
        {
            if (Query("Command name?") is not string name)
            {
                break;
            }
            await device.BeginLearning();
            await device.WaitForAck();
            byte[] data = await device.WaitForData();
            dictionary[name] = Convert.ToHexString(data);
        }
        File.WriteAllText(fileName, JsonSerializer.Serialize(dictionary), Encoding.UTF8);
    }

    private static async Task Main()
    {
        var arg = Environment.GetCommandLineArgs().LastOrDefault()?.Trim();

        Brain? brain;
        if (arg != null && _ipAddressRegex.IsMatch(arg))
        {
            brain = new(IPAddress.Parse(arg.Trim()));
        }
        else
        {
            Console.WriteLine("Discovering brain...");
            brain = await BrainDiscovery.DiscoverAsync();
        }
        if (brain is null)
        {
            Console.Error.WriteLine("Brain not found.");
            return;
        }
        Console.WriteLine($"Brain found! {brain.IPAddress}");
        try
        {
            IDeviceBuilder builder = Device.CreateDevice("Smart TV", DeviceType.TV)
                .SetManufacturer("Amir")
                .AddButton("INPUT HDMI1")
                .AddCharacteristic(DeviceCharacteristic.AlwaysOn)
                .AddTextLabel("A", "Label A", true, async (id) => await Task.FromResult(id))
                .AddButtonHandler((deviceId, button) =>
                {
                    Console.WriteLine($"{deviceId}|{button}");
                    return Task.CompletedTask;
                })
                .RegisterSubscriptionFunction((x, y) =>
                {
                    Console.WriteLine("Sub");
                });
            Console.WriteLine("Starting server...");
            await brain.StartServerAsync(new[] { builder });
            Console.WriteLine("Server started. Press any key to quit...   ");
            Console.ReadKey(true);
        }
        finally
        {
            Console.WriteLine("Server stopping...   ");
            await brain.StopServerAsync();
        }
    }

    private static async Task MainRM()
    {
        using RMDevice? remote = await RMDiscovery.DiscoverDeviceAsync();
        if (remote is null)
        {
            return;
        }
        while (true)
        {
            Console.Write("Mode: (0 - Learn, 1 - Test, else quit): ");
            switch (Console.ReadLine())
            {
                case "0":
                    await LearnCodes(remote);
                    break;

                case "1":
                    await TestCodes(remote);
                    break;

                default:
                    return;
            }
        }
    }

    private static string? Query(string prompt, string quitCommand = "Done")
    {
        Console.Write($"{prompt} ({quitCommand} to end) ");
        return Console.ReadLine() is string text && !text.Equals(quitCommand, StringComparison.OrdinalIgnoreCase)
            ? text
            : null;
    }

    private static string? QueryFileName()
    {
        return Query("What is the device name?") is string name
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $"commands_{name}.json")
            : null;
    }

    private static async Task TestCodes(RMDevice remote)
    {
        if (QueryFileName() is not { } fileName)
        {
            return;
        }
        Dictionary<string, string> dictionary = new(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fileName, Encoding.UTF8))!, StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            if (Program.Query("Command name?") is not string name)
            {
                return;
            }
            if (!dictionary.TryGetValue(name, out string? text))
            {
                Console.Error.WriteLine($"Command {name} not found");
                continue;
            }
            await remote.SendData(Convert.FromHexString(text));
            await remote.WaitForAck();
        }
    }
}