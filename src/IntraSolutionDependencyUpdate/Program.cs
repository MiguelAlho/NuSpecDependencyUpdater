using semver.tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IntraSolutionDependencyUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            /* this app is VERY opinionated. It builds upon zero29 conventions.
             * should go through working directory building a map of assembly projects, 
             * versions, and nuspec files (and dependencies).
             * 
             * Once determined, all intra-solution dependencies in nuspec files should 
             * be updated so that the lowest version allowed is the current. This way an
             * update on dependencies to the current version is possible.
             * 
             * This is more of a convention than a requirement, and will simplify dependency 
             * management by convention
             * 
             * */


            string currentDirectory = Directory.GetCurrentDirectory();  //not necessariely the exec's dir.
            Console.WriteLine("scanning :{0}", currentDirectory);

            List<Package> packages = new List<Package>();
            string[] nuspecs = Directory
                                    .GetFiles(currentDirectory, "*.nuspec", SearchOption.AllDirectories)
                                    .Where(o => !o.Contains(@"\obj\") && !o.Contains(@"\packages\")).ToArray();

            //get dependency info
            if (nuspecs.Length > 0)
            {
                Console.WriteLine("Found {0} packages.", nuspecs.Count());

                ReadNuspecs(nuspecs, packages);
                //update dependencyInfo;
                UpdateDependencyList(nuspecs, packages);
            }
            else
                Console.WriteLine("no package files found in {0}", currentDirectory);           

        }

        private static void UpdateDependencyList(string[] nuspecFiles, List<Package> packages)
        {
            foreach(var package in packages)
            {
                Console.WriteLine("Updating {0}", package.NuSpecFile);

                if(package.DependencyIds.Count == 0)
                {
                    Console.WriteLine("\tNo package references to update.");
                }
                else
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(package.NuSpecFile);

                    foreach(Dependency dependency in package.DependencyIds)
                    {
                        Package intraSolutionPackage = packages.FirstOrDefault(o => o.PackageId == dependency.Id);

                        if (intraSolutionPackage == null)
                        {
                            Console.WriteLine("\t{0} is not an Intrasolution dependency.", dependency.Id);
                            continue;
                        }

                        var currentVersion = intraSolutionPackage.CurrentVersion;
                        int nextVersion = int.Parse(currentVersion.ToString().Substring(0, currentVersion.ToString().IndexOf("."))) + 1;
                        string newVersionString = string.Format("[{0},{1})", 
                                            currentVersion.ToString(),
                                            new SemanticVersion(nextVersion, 0, 0));
                        
                        XmlNode node = doc.SelectSingleNode(
                            String.Format("/package/metadata/dependencies/dependency[@id=\"{0}\"]",intraSolutionPackage.PackageId));
                        node.Attributes["version"].Value = newVersionString;

                        Console.WriteLine("\t{0} updated to {1}", intraSolutionPackage.PackageId, newVersionString);
                    }

                    doc.Save(package.NuSpecFile);
                }
            }
        }

        static void ReadNuspecs(string[] nuspecFiles, List<Package> packageList)
        {
            foreach(string s in nuspecFiles)
            {
                Package package;

                string nuspecFile = s;
                string projectDirectory = Path.GetDirectoryName(s);

                XmlDocument nuspecXmlDoc = new XmlDocument();                
                nuspecXmlDoc.Load(s);

                string packageId = nuspecXmlDoc.SelectSingleNode("/package/metadata/id").InnerText;
                Console.WriteLine("Found package {0} @ {1}.", packageId, Path.GetFileName(s));

                package = new Package(projectDirectory, s, packageId, GetCurrentAssemblyVersion(projectDirectory));
                Console.WriteLine("\tPackage Id: {0}", package.PackageId);
                Console.WriteLine("\tAssembly version: {0}", package.CurrentVersion.ToString());

                XmlNodeList dependencies = nuspecXmlDoc.SelectNodes("/package/metadata/dependencies/dependency");
                Console.WriteLine("\tDepends on:");

                foreach(XmlNode dependency in dependencies)
                {
                    string id = dependency.Attributes["id"].Value.ToString();
                    string versions = dependency.Attributes["version"].Value.ToString();

                    Dependency dependsOn = new Dependency(id, versions);
                    package.DependencyIds.Add(dependsOn);
                    Console.WriteLine("\t\t{0} : {1}", dependsOn.Id, dependsOn.Version.ToStringNuGet());
                }
                

                packageList.Add(package);
            }
        
        }



        static SemanticVersion GetCurrentAssemblyVersion(string projectDir)
        {

            string path = Path.Combine(projectDir,@"Properties\AssemblyInfo.cs");
            if (File.Exists(path))
            {
                // Open the file to read from.
                string[] readText = File.ReadAllLines(path);
                var versionInfoLines = readText.Where(t => t.Contains("[assembly: AssemblyVersion"));
                foreach (string item in versionInfoLines)
                {
                    string version = item.Substring(item.IndexOf('(') + 2, item.LastIndexOf(')') - item.IndexOf('(') - 3);
                    
                    return SemanticVersion.ParseNuGet(version);
                }
            }

            throw new Exception("unable to extract version info from assemblyInfo @ " + projectDir);
        }
    }

    class Package
    {
        public string ProjectDirectory { get; private set; }
        public string NuSpecFile { get; private set; }
        public string PackageId { get; private set; }
        public SemanticVersion CurrentVersion { get; private set; }

        public IList<Dependency> DependencyIds { get; private set; }

        public Package(string projDir, string nuspecFile, string id, SemanticVersion currentVersion)
        {
            ProjectDirectory = projDir;
            NuSpecFile = nuspecFile;
            PackageId = id;
            CurrentVersion = currentVersion;

            DependencyIds = new List<Dependency>();
        }
    }

    class Dependency
    {
        public string Id {get; private set;}
        public VersionSpec Version {get; private set;}

        public Dependency(string id, string versionString)
        {
            Id = id;
            Version = VersionSpec.ParseNuGet(versionString) as VersionSpec;
        }
    }



    


}
