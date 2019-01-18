using McMaster.Extensions.CommandLineUtils;
using Narchive.Formats;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Narchive
{
    [Command("specific",
        Description = "Creates a NARC archive with specific files")]
    [HelpOption("-? | -h | --help",
        Description = "Show help information.")]
    class SpecificCommand
    {
        [Required]
        [LegalFilePath]
        [Argument(0, "output", "The output name of the NARC archive to create.")]
        public string OutputPath { get; set; }

        [Required]
        [Option("-f | --file", Description="A file. Must use this option multiple times for multiple files.")]
        public string[] InputFiles { get; set; }

        [Required]
        [Option("-b | --base", Description="Base directory of all your passed files")]
        public string BaseDirectory { get; set; }


        [Option("-nf | --nofilenames", "Specifies the entries in the NARC archive will not have filenames.", CommandOptionType.NoValue)]
        public bool NoFilenames { get; set; }

        private int OnExecute(IConsole console)
        {
            var reporter = new ConsoleReporter(console);
            try
            {
                // Strip end slash
                if (BaseDirectory[BaseDirectory.Length-1]==Path.DirectorySeparatorChar || BaseDirectory[BaseDirectory.Length-1]==Path.AltDirectorySeparatorChar){
                    BaseDirectory = BaseDirectory.Substring(0,BaseDirectory.Length-1);
                }

                List<NarcArchiveEntry> narcObjects = new List<NarcArchiveEntry>();
                // Initialize with base directory and passed files
                narcObjects.Add(new NarcArchiveRootDirectoryEntry
                    {
                        Name =  Path.GetFileName(BaseDirectory),
                        Path = BaseDirectory,
                        Parent = null,
                    });
                for (int i=0;i<InputFiles.Length;++i){
                    narcObjects.Add(new NarcArchiveFileEntry
                        {
                            Name = Path.GetFileName(InputFiles[i]),
                            Path = InputFiles[i],
                            Directory = null,
                        });
                }
                // Add missing directories and assign parents. Skip base directory.
                for (int i=1;i<narcObjects.Count;++i){
                    string _cachedParent = Path.GetDirectoryName(narcObjects[i].Path);
                    NarcArchiveDirectoryEntry _possibleParent = (NarcArchiveDirectoryEntry)narcObjects.FirstOrDefault(o => o.Path == _cachedParent);
                    if (_possibleParent==null){
                        _possibleParent = new NarcArchiveDirectoryEntry
                            {
                                Name = Path.GetFileName(_cachedParent),
                                Path = _cachedParent,
                                Parent = null,
                            };
                        narcObjects.Add(_possibleParent);
                    }
                    _possibleParent.Entries.Add(narcObjects[i]);
                    if (narcObjects[i] is NarcArchiveDirectoryEntry _currentFolder)
                    {
                        _currentFolder.Parent = _possibleParent;
                    }
                    else if (narcObjects[i] is NarcArchiveFileEntry _currentFile)
                    {
                        _currentFile.Directory = _possibleParent;
                    }
                }

                NarcArchiveRootDirectoryEntry _passRoot = (NarcArchiveRootDirectoryEntry)narcObjects[0];
                NarcArchive.Create((NarcArchiveRootDirectoryEntry)_passRoot,OutputPath,!NoFilenames);
                return 0;
            }
            catch (Exception e)
            {
                reporter.Error(e.Message);

                return e.HResult;
            }
        }
    }
}
