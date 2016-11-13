using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace DeploymentSettingsManager
{
    class Program
    {
        static void Main(string[] args)
        {
            string deploymentDirectory, masterDirectory;
            if (!ReadDirectoryFromInputArgs(args, out deploymentDirectory, out masterDirectory))
            {
                return;
            }
            string backupFilename = GetTheProperBackupSettingsFilename(masterDirectory);
            BackupTheSettingsFile(masterDirectory, deploymentDirectory, backupFilename);
            UpdateMasterSettingsFile(masterDirectory, backupFilename);
        }

        private static void UpdateMasterSettingsFile(string masterDirectory, string backupFilename)
        {
            var mainSettingsFile = masterDirectory + "\\ServiceLykkeWalletsettings.json";
            if (File.Exists(mainSettingsFile))
            {
                File.Delete(mainSettingsFile);
            }
            File.Copy(masterDirectory + "\\" + backupFilename, mainSettingsFile);
        }

        private static void BackupTheSettingsFile(string masterDirectory, string deploymentDirectory,
            string backupfileName)
        {
            string sourceFilename = deploymentDirectory + "\\settings.json";
            string destinationFilename = masterDirectory + "\\" + backupfileName;

            File.Copy(sourceFilename, destinationFilename);
        }

        private static string GetTheProperBackupSettingsFilename(string directoryName)
        {
            var fileNames = (Directory.GetFiles(directoryName, "settings*.json").Select(fileName => Path.GetFileName(fileName))).OrderBy(f => f);

            var counter = 0;
            string possiblieFileName = null;
            while (true)
            {
                possiblieFileName = "settings" + DateTime.UtcNow.ToString("yyyyMMdd") + counter.ToString("D3") + ".json";
                if (fileNames.Contains(possiblieFileName))
                {
                    counter++;
                }
                else
                {
                    break;
                }
            }

            return possiblieFileName;
        }

        private static bool ReadDirectoryFromInputArgs(string[] args, out string deploymentDirectory, out string masterDirectory)
        {
            deploymentDirectory = null;
            masterDirectory = null;

            if (args.Length != 2)
            {
                System.Console.WriteLine("Exactly two arguments should be provided.");
                return false;
            }

            deploymentDirectory = args[0];
            masterDirectory = args[1];
            return true;
        }
    }
}
