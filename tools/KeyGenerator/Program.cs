using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// ============================================================
// System Sweep Professional — License Key Generator v2.0
// ============================================================
// Generates RSA 2048-bit signed license keys.
//
// Usage:
//   dotnet run -- gen-keys                    Generate RSA key pair
//   dotnet run -- generate <user> [<expiry>]  Create license for THIS machine
//   dotnet run -- create <fp> <user> <date>   Create license for specific HW
//   dotnet run -- batch <file.csv>            Batch generate from CSV
//   dotnet run -- machine-fp                  Show this machine's fingerprint
//   dotnet run -- embed                       Show public key for embedding
// ============================================================

string mode = args.Length > 0 ? args[0] : "help";

switch (mode)
{
    case "gen-keys":
        GenerateKeys();
        break;
    case "generate":
    {
        string user = args.Length > 1 ? args[1] : "User";
        string expiry = args.Length > 2 ? args[2] : DateTime.Now.AddYears(1).ToString("yyyy-MM-dd");
        string fp = GetMachineFingerprint();
        Console.WriteLine($"Machine: {fp}");
        CreateLicense(fp, user, expiry, LoadPrivateKey());
        break;
    }
    case "create":
        if (args.Length < 4) { Console.WriteLine("Usage: create <fp> <user> <yyyy-MM-dd>"); return; }
        CreateLicense(args[1], args[2], args[3], LoadPrivateKey());
        break;
    case "batch":
        if (args.Length < 2) { Console.WriteLine("Usage: batch <csv>"); return; }
        BatchGenerate(args[1], LoadPrivateKey());
        break;
    case "machine-fp":
        Console.WriteLine(GetMachineFingerprint());
        break;
    case "embed":
        EmbedPublicKey();
        break;
    default:
        Console.WriteLine("""
            System Sweep License Key Generator v2.0
            =========================================
            gen-keys              Generate new RSA key pair
            generate [user] [exp] Create license for THIS machine
            create <fp> <u> <d>   Create license for specific HW
            batch <csv>           Batch create licenses
            machine-fp            Show this machine's fingerprint
            embed                 Show public key for LicenseService.cs
            """);
        break;
}

static string GetMachineFingerprint()
{
    try
    {
        var parts = new List<string>();
        using var mc = new System.Management.ManagementClass("Win32_Processor");
        foreach (var item in mc.GetInstances()) { parts.Add(item["ProcessorId"]?.ToString() ?? ""); break; }
        using var mb = new System.Management.ManagementClass("Win32_BaseBoard");
        foreach (var item in mb.GetInstances()) { parts.Add(item["SerialNumber"]?.ToString() ?? ""); break; }
        var raw = string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
        if (string.IsNullOrEmpty(raw)) raw = Guid.NewGuid().ToString();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
    }
    catch { return Guid.NewGuid().ToString("N"); }
}

static void GenerateKeys()
{
    Console.WriteLine("Generating RSA 2048-bit key pair...");
    using var rsa = RSA.Create(2048);

    var privKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
    var pubKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());

    File.WriteAllText("private.key", privKey);
    Console.WriteLine("✅ Private key → private.key (KEEP SECRET!)");

    // Format for embedding
    var lines = Chunk(pubKey, 64);
    var csCode = "private static readonly byte[] RsaPublicKey = Convert.FromBase64String(\n    \"" +
        string.Join("\" +\n    \"", lines) + "\");";
    File.WriteAllText("public-key-cs.txt", csCode);
    Console.WriteLine("✅ Public key (C#) → public-key-cs.txt");
    Console.WriteLine();
    Console.WriteLine("=== Copy this into LicenseService.cs ===");
    Console.WriteLine(csCode);
}

static void CreateLicense(string machineFP, string user, string expiry, byte[] privKeyBytes)
{
    if (!DateTime.TryParse(expiry, out _))
    {
        Console.WriteLine($"❌ Invalid expiry date: {expiry}");
        return;
    }

    using var rsa = RSA.Create();
    rsa.ImportRSAPrivateKey(privKeyBytes, out _);

    var payload = $"{machineFP}|{user}|{expiry}";
    var payloadBytes = Encoding.UTF8.GetBytes(payload);
    var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    var licenseData = $"{Convert.ToBase64String(signature)}|{payload}";
    var licenseKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(licenseData));

    Console.WriteLine($"┌─────────────────────────────────────────────┐");
    Console.WriteLine($"│        SYSTEM SWEEP LICENSE KEY             │");
    Console.WriteLine($"├─────────────────────────────────────────────┤");
    Console.WriteLine($"│ {WrapText(licenseKey, 55)} │");
    Console.WriteLine($"├─────────────────────────────────────────────┤");
    Console.WriteLine($"│ User:    {user,-32} │");
    Console.WriteLine($"│ Machine: {machineFP[..16]}...{"",32} │");
    Console.WriteLine($"│ Expires: {expiry,-34} │");
    Console.WriteLine($"└─────────────────────────────────────────────┘");
    Console.WriteLine();
    Console.WriteLine($"License Key (copy this):");
    Console.WriteLine(licenseKey);
}

static byte[] LoadPrivateKey()
{
    if (!File.Exists("private.key"))
    {
        Console.Error.WriteLine("❌ private.key not found. Run 'gen-keys' first.");
        Environment.Exit(1);
        return null!;
    }
    return Convert.FromBase64String(File.ReadAllText("private.key").Trim());
}

static void BatchGenerate(string csvPath, byte[] privKey)
{
    if (!File.Exists(csvPath)) { Console.Error.WriteLine($"❌ File not found: {csvPath}"); return; }

    var lines = File.ReadAllLines(csvPath);
    var output = new List<string>();
    int ok = 0, fail = 0;

    foreach (var line in lines)
    {
        var parts = line.Split(',');
        if (parts.Length < 3 || string.IsNullOrWhiteSpace(parts[0]))
        { fail++; continue; }

        var fp = parts[0].Trim();
        var user = parts[1].Trim();
        var expiry = parts[2].Trim();

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privKey, out _);
        var payload = $"{fp}|{user}|{expiry}";
        var sig = rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var license = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Convert.ToBase64String(sig)}|{payload}"));
        output.Add(license);
        ok++;
    }

    File.WriteAllLines("batch-licenses.txt", output);
    Console.WriteLine($"✅ Generated {ok} licenses ({fail} skipped) → batch-licenses.txt");
}

static void EmbedPublicKey()
{
    if (!File.Exists("private.key"))
    {
        Console.WriteLine("No keys found. Run 'gen-keys' first.");
        return;
    }
    // Extract public key from private key
    using var rsa = RSA.Create();
    rsa.ImportRSAPrivateKey(Convert.FromBase64String(File.ReadAllText("private.key").Trim()), out _);
    var pubKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
    var lines = Chunk(pubKey, 64);
    var csCode = "private static readonly byte[] RsaPublicKey = Convert.FromBase64String(\n    \"" +
        string.Join("\" +\n    \"", lines) + "\");";
    Console.WriteLine(csCode);
}

static List<string> Chunk(string s, int size)
{
    var result = new List<string>();
    for (int i = 0; i < s.Length; i += size)
        result.Add(s.Substring(i, Math.Min(size, s.Length - i)));
    return result;
}

static string WrapText(string text, int maxLen)
{
    if (text.Length <= maxLen) return text;
    var result = new StringBuilder();
    int pos = 0;
    while (pos < text.Length)
    {
        int len = Math.Min(maxLen, text.Length - pos);
        if (pos > 0) result.AppendLine().Append("│ ");
        result.Append(text.Substring(pos, len));
        pos += len;
    }
    return result.ToString();
}
