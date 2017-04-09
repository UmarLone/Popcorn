using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using Popcorn.GifLoader.Properties;

[assembly: AssemblyTitle("Popcorn.GifLoader")]
[assembly: AssemblyDescription("Popcorn Windows native app made in .NET/WPF")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Popcorn")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("a985ebe7-753b-4d73-b363-4b63d87f98b7")]

[assembly: AssemblyVersion(VersionInfo.VersionString)]
[assembly: AssemblyFileVersion(VersionInfo.VersionString)]
[assembly: AssemblyInformationalVersion(VersionInfo.VersionString)]

[assembly: InternalsVisibleTo("Popcorn.GifLoader.Demo")]

namespace Popcorn.GifLoader.Properties
{
    class VersionInfo
    {
        /// <summary>
        /// Single place to define version
        /// </summary>
        public const string VersionString = "1.4.14";
    }
}