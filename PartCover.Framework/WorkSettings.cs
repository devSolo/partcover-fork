using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace PartCover.Framework
{
    public class SettingsException : Exception
    {
        public SettingsException(string message) : base(message)
        {
        }
    }

    public class WorkSettings
    {
        #region ArgumentOption

        private class ArgumentOption
        {
            #region Delegates

            public delegate void ActivateHandler(WorkSettings settings);

            public delegate void OptionHandler(WorkSettings settings, string value);

            #endregion

            public readonly ActivateHandler activator;
            public readonly OptionHandler handler;
            public readonly string key;
            public string arguments;
            public string description;
            public bool optional;

            public ArgumentOption(string key, OptionHandler handler)
                : this(key, handler, null, true, string.Empty, string.Empty)
            {
            }

            public ArgumentOption(string key, ActivateHandler activator)
                : this(key, null, activator, true, string.Empty, string.Empty)
            {
            }

            public ArgumentOption(string key, OptionHandler handler, ActivateHandler activator, bool optional,
                                  string arguments, string description)
            {
                this.key = key;
                this.handler = handler;
                this.activator = activator;
                this.optional = optional;
                this.arguments = arguments;
                this.description = description;
            }
        }

        #endregion ArgumentOption

        private static readonly ArgumentOption[] Options =
            {
                new ArgumentOption("--target", ReadTarget),
                new ArgumentOption("--version", ReadVersion),
                new ArgumentOption("--help", ReadHelp),
                new ArgumentOption("--target-work-dir", ReadTargetWorkDir),
                new ArgumentOption("--generate", ReadGenerateSettingsFile),
                new ArgumentOption("--log", ReadLogLevel),
                new ArgumentOption("--target-args", ReadTargetArgs),
                new ArgumentOption("--include", ReadInclude),
                new ArgumentOption("--exclude", ReadExclude),
                new ArgumentOption("--output", ReadOutput),
                new ArgumentOption("--reportFormat", ReadReportFormat),
                new ArgumentOption("--settings", ReadSettingsFile),
            };

        private ArgumentOption currentOption;

        #region settings readers

        private static void ReadTarget(WorkSettings settings, string value)
        {
            if (!File.Exists(value))
                throw new SettingsException("Cannot find target (" + value + ")");
            settings.targetPath = Path.GetFullPath(value);
        }

        private static void ReadGenerateSettingsFile(WorkSettings settings, string value)
        {
            settings.generateSettingsFileName = value;
        }

        private static void ReadHelp(WorkSettings settings)
        {
            settings.printLongHelp = true;
        }

        private static void ReadVersion(WorkSettings settings)
        {
            settings.printVersion = true;
        }

        private static void ReadTargetWorkDir(WorkSettings settings, string value)
        {
            if (!Directory.Exists(value))
                throw new SettingsException("Cannot find target working dir (" + value + ")");
            settings.targetWorkingDir = Path.GetFullPath(value);
        }

        private static void ReadSettingsFile(WorkSettings settings, string value)
        {
            if (!File.Exists(value))
                throw new SettingsException("Cannot find settings file (" + value + ")");
            settings.settingsFile = value;
        }

        private static void ReadOutput(WorkSettings settings, string value)
        {
            if (value.Length > 0) settings.outputFile = value;
        }

        private static void ReadReportFormat(WorkSettings settings, string value)
        {
            if (value.Length > 0) settings.reportFormat = value;
        }

        private static void ReadExclude(WorkSettings settings, string value)
        {
            if (value.Length > 0) settings.excludeItems.Add(value);
        }

        private static void ReadInclude(WorkSettings settings, string value)
        {
            if (value.Length > 0) settings.includeItems.Add(value);
        }

        private static void ReadTargetArgs(WorkSettings settings, string value)
        {
            settings.targetArgs = value;
        }

        private static void ReadLogLevel(WorkSettings settings, string value)
        {
            try
            {
                settings.logLevel = Int32.Parse(value, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new SettingsException("Wrong value for --log (" + ex.Message + ")");
            }
        }

        #endregion

        #region Parse Args

        private bool printLongHelp;
        private bool printVersion;

        public bool InitializeFromCommandLine(string[] args)
        {
            currentOption = null;

            foreach (string arg in args)
            {
                ArgumentOption nextOption = GetOptionHandler(arg);
                if (nextOption != null)
                {
                    currentOption = nextOption;

                    if (currentOption.activator != null)
                        currentOption.activator(this);

                    continue;
                }

                if (currentOption == null)
                {
                    PrintShortUsage(true);
                    return false;
                }

                if (currentOption.handler != null)
                {
                    currentOption.handler(this, arg);
                }
                else
                {
                    throw new SettingsException("Unexpected argument for option '" + currentOption.key + "'");
                }
            }

            if (settingsFile != null)
            {
                ReadSettingsFile();
            }
            else if (generateSettingsFileName != null)
            {
                GenerateSettingsFile();
                return false;
            }
            bool showShort = true;
            if (printLongHelp)
            {
                showShort = false;
                PrintVersion();
                PrintShortUsage(false);
                PrintLongUsage();
            }
            else if (printVersion)
            {
                PrintVersion();
            }

            if (!String.IsNullOrEmpty(TargetPath))
                return true;

            if (showShort)
            {
                PrintShortUsage(true);
            }
            return false;
        }

        private static ArgumentOption GetOptionHandler(string arg)
        {
            foreach (ArgumentOption o in Options)
            {
                if (o.key.Equals(arg, StringComparison.CurrentCulture))
                    return o;
            }

            if (arg.StartsWith("--"))
            {
                throw new SettingsException("Invalid option '" + arg + "'");
            }

            return null;
        }

        #endregion //Parse Args

        #region PrintUsage

        public static void PrintShortUsage(bool showNext)
        {
            Console.Out.WriteLine("Usage:");
            Console.Out.WriteLine("  PartCover.exe  --target <file_name> [--target-work-dir <path>]");
            Console.Out.WriteLine("                [--target-args <arguments>] [--settings <file_name>]");
            Console.Out.WriteLine("                [--include <item> ... ] [--exclude <item> ... ]");
            Console.Out.WriteLine("                [--output <file_name>] [--log <log_level>]");
            Console.Out.WriteLine("                [--generate <file_name>] [--help] [--version]");
            Console.Out.WriteLine("");
            if (showNext)
            {
                Console.Out.WriteLine("For more help execute:");
                Console.Out.WriteLine("  PartCover.exe --help");
            }
        }

        public static void PrintLongUsage()
        {
            Console.Out.WriteLine("Arguments:  ");
            Console.Out.WriteLine("   --target=<file_name> :");
            Console.Out.WriteLine("       specifies path to executable file to count coverage. <file_name> may be");
            Console.Out.WriteLine("       either full path or relative path to file.");
            Console.Out.WriteLine("   --target-work-dir=<path> :");
            Console.Out.WriteLine("       specifies working directory to target process. By default, working");
            Console.Out.WriteLine("       directory will be working directory for PartCover");
            Console.Out.WriteLine("   --target-args=<arguments> :");
            Console.Out.WriteLine("       specifies arguments for target process. If target argument contains");
            Console.Out.WriteLine("       spaces - quote <argument>. If you want specify quote (\") in <arguments>,");
            Console.Out.WriteLine("       then precede it by slash (\\)");
            Console.Out.WriteLine("   --include=<item>, --exclude=<item> :");
            Console.Out.WriteLine(
                "       specifies item to include or exclude from report. Item is in following format: ");
            Console.Out.WriteLine("          [<assembly_regexp>]<class_regexp>");
            Console.Out.WriteLine("       where <regexp> is simple regular expression, containing only asterix and");
            Console.Out.WriteLine("       characters to point item. For example:");
            Console.Out.WriteLine("          [mscorlib]*");
            Console.Out.WriteLine("          [System.*]System.IO.*");
            Console.Out.WriteLine("          [System]System.Colle*");
            Console.Out.WriteLine("          [Test]Test.*+InnerClass+SecondInners*");
            Console.Out.WriteLine("   --settings=<file_name> :");
            Console.Out.WriteLine("       specifies input settins in xml file.");
            Console.Out.WriteLine("   --generate=<file_name> :");
            Console.Out.WriteLine("       generates setting file using settings specified. By default, <file_name>");
            Console.Out.WriteLine("       is 'PartCover.settings.xml'");
            Console.Out.WriteLine("   --output=<file_name> :");
            Console.Out.WriteLine("       specifies output file for writing result xml. It will be placed in UTF-8");
            Console.Out.WriteLine("       encoding. By default, output data will be processed via console output.");
            Console.Out.WriteLine("   --log=<log_level> :");
            Console.Out.WriteLine("       specifies log level for driver. If <log_level> greater than 0, log file");
            Console.Out.WriteLine("       will be created in working directory for PartCover");
            Console.Out.WriteLine("   --help :");
            Console.Out.WriteLine("       shows current help");
            Console.Out.WriteLine("   --version :");
            Console.Out.WriteLine("       shows version of PartCover console application");
            Console.Out.WriteLine("");
        }

        public static void PrintVersion()
        {
            Console.Out.WriteLine("PartCover (console)");
            Console.Out.WriteLine("   application version {0}.{1}.{2}",
                                  Assembly.GetExecutingAssembly().GetName().Version.Major,
                                  Assembly.GetExecutingAssembly().GetName().Version.Minor,
                                  Assembly.GetExecutingAssembly().GetName().Version.Revision);
            Type connector = typeof (Connector);
            Console.Out.WriteLine("   connector version {0}.{1}.{2}",
                                  connector.Assembly.GetName().Version.Major,
                                  connector.Assembly.GetName().Version.Minor,
                                  connector.Assembly.GetName().Version.Revision);
            Console.Out.WriteLine("");
        }

        #endregion PrintUsage

        #region Properties

        private readonly List<string> excludeItems = new List<string>();
        private readonly List<string> includeItems = new List<string>();
        private string generateSettingsFileName;
        private int logLevel;
        private string outputFile;
        private string reportFormat;
        private string settingsFile;
        private string targetArgs;
        private string targetPath;
        private string targetWorkingDir;

        public string SettingsFile
        {
            get { return settingsFile; }
            set { settingsFile = value; }
        }

        public string GenerateSettingsFileName
        {
            get { return generateSettingsFileName; }
            set { generateSettingsFileName = value; }
        }

        public int LogLevel
        {
            get { return logLevel; }
            set { logLevel = value; }
        }

        public string[] IncludeItems
        {
            get { return includeItems.ToArray(); }
        }

        public string[] ExcludeItems
        {
            get { return excludeItems.ToArray(); }
        }

        public string TargetPath
        {
            get { return targetPath; }
            set { targetPath = value; }
        }

        public string TargetWorkingDir
        {
            get { return targetWorkingDir; }
            set { targetWorkingDir = value; }
        }

        public string TargetArgs
        {
            get { return targetArgs; }
            set { targetArgs = value; }
        }

        public string FileNameForReport
        {
            get { return outputFile; }
            set { outputFile = value; }
        }

        public string ReportFormat
        {
            get { return reportFormat; }
            set { reportFormat = value; }
        }

        public bool OutputToFile
        {
            get { return FileNameForReport != null; }
        }

        #endregion //Properties

        #region SerializeSettings

        private static void AppendValue(XmlNode parent, string name, string value)
        {
            Debug.Assert(parent != null && parent.OwnerDocument != null);
            XmlNode node = parent.AppendChild(parent.OwnerDocument.CreateElement(name));
            node.InnerText = value;
        }

        public void GenerateSettingsFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateElement("PartCoverSettings"));
            if (targetPath != null) AppendValue(xmlDoc.DocumentElement, "Target", targetPath);
            if (targetWorkingDir != null) AppendValue(xmlDoc.DocumentElement, "TargetWorkDir", targetWorkingDir);
            if (targetArgs != null) AppendValue(xmlDoc.DocumentElement, "TargetArgs", targetArgs);
            if (logLevel > 0)
                AppendValue(xmlDoc.DocumentElement, "LogLevel", logLevel.ToString(CultureInfo.InvariantCulture));
            if (outputFile != null) AppendValue(xmlDoc.DocumentElement, "Output", outputFile);
            if (printLongHelp)
                AppendValue(xmlDoc.DocumentElement, "ShowHelp", printLongHelp.ToString(CultureInfo.InvariantCulture));
            if (printVersion)
                AppendValue(xmlDoc.DocumentElement, "ShowVersion", printVersion.ToString(CultureInfo.InvariantCulture));

            foreach (string item in IncludeItems) AppendValue(xmlDoc.DocumentElement, "Rule", "+" + item);
            foreach (string item in ExcludeItems) AppendValue(xmlDoc.DocumentElement, "Rule", "-" + item);

            try
            {
                if ("console".Equals(generateSettingsFileName, StringComparison.InvariantCulture))
                    xmlDoc.Save(Console.Out);
                else
                    xmlDoc.Save(generateSettingsFileName);
            }
            catch (Exception ex)
            {
                throw new SettingsException("Cannot write settings (" + ex.Message + ")");
            }
        }

        public void ReadSettingsFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(settingsFile);
                logLevel = 0;

                XmlNode node = xmlDoc.SelectSingleNode("/PartCoverSettings/Target/text()");
                if (node != null && node.Value != null) targetPath = node.Value;
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/TargetWorkDir/text()");
                if (node != null && node.Value != null) targetWorkingDir = node.Value;
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/TargetArgs/text()");
                if (node != null && node.Value != null) targetArgs = node.Value;
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/LogLevel/text()");
                if (node != null && node.Value != null) logLevel = int.Parse(node.Value);
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/Output/text()");
                if (node != null && node.Value != null) outputFile = node.Value;
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/ShowHelp/text()");
                if (node != null && node.Value != null) printLongHelp = bool.Parse(node.Value);
                node = xmlDoc.SelectSingleNode("/PartCoverSettings/ShowVersion/text()");
                if (node != null && node.Value != null) printVersion = bool.Parse(node.Value);

                XmlNodeList list = xmlDoc.SelectNodes("/PartCoverSettings/Rule");
                if (list != null)
                {
                    foreach (XmlNode rule in list)
                    {
                        XmlNode ruleText = rule.SelectSingleNode("text()");
                        if (ruleText == null || ruleText.Value == null || ruleText.Value.Length == 0)
                            continue;
                        string[] rules = ruleText.Value.Split(',');
                        foreach (string s in rules)
                        {
                            if (s.Length <= 1)
                                continue;
                            if (s[0] == '+')
                                includeItems.Add(s.Substring(1));
                            else if (s[0] == '-')
                                excludeItems.Add(s.Substring(1));
                            else
                                throw new SettingsException("Wrong rule format (" + s + ")");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SettingsException("Cannot load settings (" + ex.Message + ")");
            }
        }

        #endregion

        public void IncludeRules(ICollection<string> strings)
        {
            includeItems.AddRange(strings);
        }

        public void ExcludeRules(ICollection<string> strings)
        {
            excludeItems.AddRange(strings);
        }

        public void ClearRules()
        {
            includeItems.Clear();
            excludeItems.Clear();
        }
    }
}