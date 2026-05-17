using System.Text.Json;
using System.Text.Json.Nodes;

namespace MITMDomainFronting.Windows;

internal static class ConfigBuilder
{
    public static void BuildRuntimeConfig()
    {
        AppPaths.EnsureDirectories();

        if (!File.Exists(AppPaths.BundledConfigPath))
        {
            throw new FileNotFoundException(
                "MITM-DomainFronting.json is missing. Run scripts\\Prepare-Assets.ps1 first.",
                AppPaths.BundledConfigPath);
        }

        var json = File.ReadAllText(AppPaths.BundledConfigPath);
        var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("Config JSON is empty.");

        PatchCertificatePaths(root);

        File.WriteAllText(
            AppPaths.RuntimeConfigPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void PatchCertificatePaths(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("certificateFile"))
            {
                obj["certificateFile"] = AppPaths.CertificatePath;
            }
            if (obj.ContainsKey("keyFile"))
            {
                obj["keyFile"] = AppPaths.KeyPath;
            }

            foreach (var child in obj.Select(pair => pair.Value).OfType<JsonNode>().ToList())
            {
                PatchCertificatePaths(child);
            }
            return;
        }

        if (node is JsonArray array)
        {
            foreach (var child in array.OfType<JsonNode>().ToList())
            {
                PatchCertificatePaths(child);
            }
        }
    }
}

