﻿using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniCover.Utils
{
    public class DepsJsonUtils
    {
        public static void PatchDepsJson(string depsJsonFile, string hitServicesVersion)
        {
            var content = File.ReadAllText(depsJsonFile);
            var newContent = PatchDepsJsonContent(content, hitServicesVersion);
            File.WriteAllText(depsJsonFile, newContent);
        }

        public static void UnpatchDepsJson(string depsJsonFile)
        {
            var content = File.ReadAllText(depsJsonFile);
            var newContent = UnpatchDepsJsonContent(content);
            File.WriteAllText(depsJsonFile, newContent);
        }

        public static string PatchDepsJsonContent(string content, string hitServicesVersion)
        {
            var json = JsonConvert.DeserializeObject<JObject>(content);

            var targets = json["targets"] as JObject;

            if (targets != null)
            {
                foreach (var target in targets.PropertyValues().OfType<JObject>())
                {
                    target[$"MiniCover.HitServices/{hitServicesVersion}"] = new JObject
                    {
                        ["runtime"] = new JObject
                        {
                            ["MiniCover.HitServices.dll"] = new JObject()
                        }
                    };
                }
            }

            var libraries = json["libraries"] as JObject;
            if (libraries != null)
            {
                libraries[$"MiniCover.HitServices/{hitServicesVersion}"] = new JObject
                {
                    ["type"] = "project",
                    ["serviceable"] = false,
                    ["sha512"] = ""
                };
            }

            return JsonConvert.SerializeObject(json, Formatting.Indented);
        }

        public static string UnpatchDepsJsonContent(string content)
        {
            var json = JsonConvert.DeserializeObject<JObject>(content);

            var targets = json["targets"] as JObject;
            if (targets != null)
            {
                foreach (var target in targets.PropertyValues().OfType<JObject>())
                {
                    var miniCoverProperties = target.Properties()
                        .Where(p => p.Name.StartsWith("MiniCover.HitServices/"))
                        .ToArray();

                    foreach (var property in miniCoverProperties)
                        property.Remove();
                }
            }

            var libraries = json["libraries"] as JObject;
            if (libraries != null)
            {
                var miniCoverProperties = libraries.Properties()
                    .Where(p => p.Name.StartsWith("MiniCover.HitServices/"))
                    .ToArray();

                foreach (var property in miniCoverProperties)
                    property.Remove();
            }

            return JsonConvert.SerializeObject(json, Formatting.Indented);
        }

        public static DependencyContext LoadDependencyContext(string assemblyDirectory)
        {
            var depsJsonPath = Directory.GetFiles(assemblyDirectory, "*.deps.json", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (depsJsonPath != null)
            {
                using (var depsJsonStream = File.OpenRead(depsJsonPath))
                {
                    using (var reader = new DependencyContextJsonReader())
                    {
                        return reader.Read(depsJsonStream);
                    }
                }
            }

            return null;
        }

        public static List<string> GetAdditionalPaths(string runtimeConfigContent)
        {
            var additionalPaths = new List<string>();
            if (string.IsNullOrEmpty(runtimeConfigContent))
            {
                return additionalPaths;
            }

            var runtimeConfig = JsonConvert.DeserializeObject<JObject>(runtimeConfigContent);
            if (runtimeConfig == null)
            {
                return additionalPaths;
            }

            var additionalProbingPaths = runtimeConfig["runtimeOptions"]?["additionalProbingPaths"];
            if (additionalProbingPaths != null)
            {
                foreach (var path in additionalProbingPaths.Values<string>())
                {
                    if (!path.Contains("|"))
                    {
                        additionalPaths.Add(path);
                    }
                }
            }
            return additionalPaths;
        }
    }
}
