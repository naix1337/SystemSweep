using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// ============================================================
// System Sweep License Key Generator
// ============================================================
// Usage:
//   1. Generate keys:   dotnet run -- gen-keys
//   2. Create license:  dotnet run -- create <machineFP> <user> <expiry>
//   3. Batch:           dotnet run -- batch <file.csv>
// ============================================================

string mode = args.Length > 0 ? args[0] : "help";

switch (mode)
{
    case "gen-keys":
        GenerateKeys();
        break;
    case "create":
        if (args.Length < 4) { Console.WriteLine("Usage: create <machineFP> <user> <yyyy-MM-dd>"); return; }
        CreateLicense(args[1], args[2], args[3], LoadPrivateKey());
        break;
    case "batch":
        if (args.Length < 2) { Console.WriteLine("Usage: batch <csv-file>"); return; }
        BatchGenerate(args[1], LoadPrivateKey());
        break;
    case "help":
    default:
        Console.WriteLine("""
            System Sweep License Key Generator
            ====================================
            gen-keys              Generate new RSA key pair
            create <fp> <u> <d>   Create license for machine FP, user, expiry date
            batch <file.csv>      Batch create from CSV file
            """);
        break;
}

static void GenerateKeys()
{
    Console.WriteLine("Generating RSA 2048-bit key pair...");
    using var rsa = RSA.Create(2048);

    var pubKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
    var privKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

    // Save private key
    File.WriteAllText("private.key", privKey);
    Console.WriteLine("Private key saved: private.key");

    // Save public key in C# format for embedding
    var pubKeyLines = Chunk(pubKey, 64);
    var csFormat = "private static readonly byte[] RsaPublicKey = Convert.FromBase64String(\n    \"" +
        string.Join("\" +\n    \"", pubKeyLines) + "\");";
    File.WriteAllText("public.key.cs", csFormat);
    Console.WriteLine("Public key (C#): public.key.cs");
    Console.WriteLine();
    Console.WriteLine("=== Embed this in LicenseService.cs ===");
    Console.WriteLine(csFormat);
}

static string CreateLicense(string machineFP, string user, string expiry, byte[] privKeyBytes)
{
    using var rsa = RSA.Create();
    rsa.ImportRSAPrivateKey(privKeyBytes, out _);

    var payload = $"{machineFP}|{user}|{expiry}";
    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var licenseData = $"{Convert.ToBase64String(signature)}|{payload}";
    var licenseKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(licenseData));

    Console.WriteLine($"License: {licenseKey}");
    Console.WriteLine($"For:     {user} | {machineFP[..16]}... | Expires: {expiry}");
    return licenseKey;
}

static byte[] LoadPrivateKey()
{
    if (!File.Exists("private.key"))
    {
        Console.Error.WriteLine("Error: private.key not found. Run 'gen-keys' first.");
        Environment.Exit(1);
        return null!;
    }
    return Convert.FromBase64String(File.ReadAllText("private.key").Trim());
}

static void BatchGenerate(string csvPath, byte[] privKey)
{
    if (!File.Exists(csvPath)) { Console.Error.WriteLine($"File not found: {csvPath}"); return; }
    var lines = File.ReadAllLines(csvPath);
    var output = new List<string>();

    foreach (var line in lines)
    {
        var parts = line.Split(',');
        if (parts.Length < 3) continue;
        var license = CreateLicense(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), privKey);
        output.Add($"{parts[0]},{parts[1]},{parts[2]},{license}");
    }

    File.WriteAllLines("licenses.csv", output);
    Console.WriteLine($"Generated {output.Count} licenses → licenses.csv");
}

static List<string> Chunk(string s, int size)
{
    var result = new List<string>();
    for (int i = 0; i < s.Length; i += size)
        result.Add(s.Substring(i, Math.Min(size, s.Length - i)));
    return result;
}
