using System.Reflection;
using System.Text.RegularExpressions;
using Prism.Mvvm;

namespace PlaywrightForSdWebUi.Utils;

public class AppVersionInfo : BindableBase
{
    public AppVersionInfo()
    {
        var projectName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
        ProjectName = Regex.Replace(projectName, "([a-z])([A-Z])", "$1 $2");
    }

    public string Title => GetAppNameWithVersion();

    /// <summary>
    ///     If this property is not null, its value will be used as the version string in GetAppNameWithVersion.
    ///     This is intended for testing purposes only.
    /// </summary>
    public string CustomVersion { get; set; }

    private string ProjectName { get; }

    public string GetAppNameWithVersion()
    {
        if (!string.IsNullOrWhiteSpace(CustomVersion))
        {
            return $"{ProjectName} ver:{CustomVersion}";
        }

        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return !string.IsNullOrWhiteSpace(infoVersion)
            ? $"{ProjectName} ver:{infoVersion}"
            : $"{ProjectName} (version unknown)";
    }
}