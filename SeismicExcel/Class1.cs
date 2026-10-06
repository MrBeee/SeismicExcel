using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using SharpProj;
using System;
using System.Reflection;

// See : https://excel-dna.net/docs/guides-basic/Intellisense/

namespace SeismicExcel
{
    public class IntelliSenseAddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            IntelliSenseServer.Install();
        }

        public void AutoClose()
        {
            IntelliSenseServer.Uninstall();
        }
    }

    public class Class1
    {
        [ExcelFunction(Name = "SE.lib.Version", Description = "Returns assembly version, file version and informational version of the add-in")]
        public static object AddInVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version assemblyVersion = assembly.GetName().Version;

            AssemblyFileVersionAttribute fileVersionAttribute =
                (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(
                    assembly,
                    typeof(AssemblyFileVersionAttribute));

            AssemblyInformationalVersionAttribute informationalVersionAttribute =
                (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    assembly,
                    typeof(AssemblyInformationalVersionAttribute));

            string fileVersion = fileVersionAttribute == null ? string.Empty : fileVersionAttribute.Version;
            string informationalVersion = informationalVersionAttribute == null ? string.Empty : informationalVersionAttribute.InformationalVersion;

            return new object[,]
            {
                { "AssemblyVersion", assemblyVersion == null ? string.Empty : assemblyVersion.ToString() },
                { "FileVersion", fileVersion },
                { "InformationalVersion", informationalVersion }
            };
        }

        /// <summary>
        /// Returns version of Operating System and Excel
        /// </summary>
        /// <returns>Name and version number of the current operating system</returns>
        /// <example>
        /// "Windows (64-bit) NT :.00" with Win10(64-bit) and Excel 2016(16.0.6326.1010, 64-bit)
        /// </example>
        [ExcelFunction(Name = "SE.lib.OperatingSystem", Description = "Returns version of Operating System and Excel")]
        public static string OperatingSystem()
        {
            dynamic xlApp = ExcelDnaUtil.Application;
            string strOS = xlApp.OperatingSystem;
            return strOS;
        }

        /// <summary>
        /// A simple Excel-DNA function that returns a greeting message.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [ExcelFunction(Name = "SE.tst.SayHello", Description = "Test Excel-DNA library")]
        public static string SayHello(
            [ExcelArgument(Name = "name", Description = "The name to greet")] string name)
        {
            return $"Hello {name}!";
        }

        /// <summary>
        /// Verification that SharpProj is initialized correctly and can be used for coordinate transformations.
        /// </summary>
        [ExcelFunction(Name = "SE.tst.TestProj", Description = "Test SharpProj initialization")]
        public static string TestProj()
        {
            using (var ctx = new ProjContext())
            {
                return $"SharpProj initialized successfully (Version {ctx.Version})";
            }
        }

        [ExcelFunction(Name = "SE.tst.TestDefaultValues", Description = "Example with Excel-friendly optional arguments")]
        public static object TestDefaultValues(
            [ExcelArgument(Name = "x", Description = "Required value")] double x,
            [ExcelArgument(Name = "scale", Description = "Optional scale")] object scale,
            [ExcelArgument(Name = "label", Description = "Optional label")] object label)
        {
            double actualScale = scale is ExcelMissing || scale is ExcelEmpty ? 1.0 : Convert.ToDouble(scale);
            string actualLabel = label is ExcelMissing || label is ExcelEmpty ? "Default" : label.ToString();

            return $"{actualLabel}: {x * actualScale}";
        }

    }
}