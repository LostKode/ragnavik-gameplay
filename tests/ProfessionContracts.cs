using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class ProfessionContracts
{
    internal static void Verify(string professions, string game)
    {
        Check(professions, "PreventExperience", "Prefix", 2, SignatureTypeCode.Boolean);
        Check(professions, "Helper", "getActiveProfessions", 0, null);
        Check(professions, "Professions", "fromSkill", 1, null);
        Check(professions, "Skill_Element", "UpdateImageDisplay", 1, SignatureTypeCode.Void);
        Check(game, "Skills", "RaiseSkill", 2, SignatureTypeCode.Void);
        Check(game, "Player", "GetSkills", 0, null);
        Console.WriteLine("PASS actual Professions and game method contracts");
    }

    internal static void VerifyTooltip(string managed)
    {
        foreach (var file in Directory.EnumerateFiles(managed, "*.dll"))
        {
            using var pe = new PEReader(File.OpenRead(file));
            if (!pe.HasMetadata) continue;
            var m = pe.GetMetadataReader();
            foreach (var handle in m.TypeDefinitions)
            {
                var type = m.GetTypeDefinition(handle);
                if (m.GetString(type.Name) != "UITooltip") continue;
                var fields = type.GetFields().Select(m.GetFieldDefinition).Select(f => m.GetString(f.Name)).ToArray();
                if (!fields.Contains("m_topic") || !fields.Contains("m_text")) throw new Exception("Tooltip fields changed");
                Console.WriteLine($"PASS tooltip fields: {Path.GetFileName(file)} {m.GetString(type.Namespace)}.UITooltip");
                return;
            }
        }
        throw new Exception("UITooltip missing");
    }

    private static void Check(string path, string typeName, string methodName, int count, SignatureTypeCode? returns)
    {
        using var pe = new PEReader(File.OpenRead(path));
        var m = pe.GetMetadataReader();
        var methods = m.TypeDefinitions.Select(m.GetTypeDefinition)
            .Where(t => m.GetString(t.Name) == typeName)
            .SelectMany(t => t.GetMethods()).Select(m.GetMethodDefinition)
            .Where(method => m.GetString(method.Name) == methodName).ToArray();
        if (methods.Length != 1) throw new Exception($"{typeName}.{methodName}: expected exactly one method");
        var reader = m.GetBlobReader(methods[0].Signature);
        var header = reader.ReadSignatureHeader();
        if (header.IsGeneric) reader.ReadCompressedInteger();
        if (reader.ReadCompressedInteger() != count) throw new Exception($"{typeName}.{methodName}: parameter count changed");
        if (returns != null && reader.ReadSignatureTypeCode() != returns) throw new Exception($"{typeName}.{methodName}: return type changed");
    }
}
