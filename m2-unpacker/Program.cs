using GMWare.M2.MArchive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace m2_unpacker
{
    internal class Program
    {
        internal enum KeyTypes { SegaGenesisMini }

        internal class AllDataFile
        {
            public byte[] KeyHash { get; set; }

            public int KeyLength { get; set; }
            public Func<IMArchiveCodec> CodecConstructor { get; set; }
        }

        public static readonly IReadOnlyDictionary<KeyTypes, AllDataFile> knownFiles = new Dictionary<KeyTypes, AllDataFile>()
        {
            {
                KeyTypes.SegaGenesisMini, new AllDataFile()
                {
                    KeyHash = new byte[] { 0xC7, 0x54, 0xF9, 0x22, 0x59, 0x6D, 0xDB, 0x68, 0x0A, 0x9D, 0xA4, 0xB2, 0xA0, 0x28, 0x03, 0x38 },
                    KeyLength = 64,
                    CodecConstructor = () =>
                    {
                        return new ZStandardCodec();
                    }
                }
            }
        };

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No input file specified.");
                return 1;
            }

            if (args.Length > 0 && !File.Exists(args[0]))
            {
                Console.WriteLine("Input file does not exist.");
                return 2;
            }

            string alldataPath = Path.Combine(Path.GetDirectoryName(args[0]), "alldata.bin");
            string alldataPsbPath = Path.Combine(Path.GetDirectoryName(args[0]), "alldata.psb.m");
            string m2Path = Path.Combine(Path.GetDirectoryName(args[0]), "m2engage");

            if (!File.Exists(alldataPath))
            {
                Console.WriteLine("alldata.bin not found");
                return 3;
            }

            if (!File.Exists(alldataPsbPath))
            {
                Console.WriteLine("alldata.psb.m not found, please place alongside alldata.bin");
                return 4;
            }

            if (!File.Exists(m2Path))
            {
                Console.WriteLine("m2engage not found, please place alongside alldata.bin");
                return 5;
            }

            byte[] alldataKey = new byte[13];

            using (var m2Stream = new MemoryStream())
            using (var m2FileStream = File.OpenRead(m2Path))
            {
                m2FileStream.CopyTo(m2Stream);

                using (MD5 md5Hasher = MD5.Create())
                {
                    m2Stream.Seek(0, SeekOrigin.Begin);

                    for (int i = 0; i < m2Stream.Length - alldataKey.Length; i += 4)
                    {
                        m2Stream.Seek(i, SeekOrigin.Begin);
                        m2Stream.Read(alldataKey, 0, alldataKey.Length);

                        foreach (var knownFile in knownFiles)
                        {
                            if (md5Hasher.ComputeHash(alldataKey).SequenceEqual(knownFile.Value.KeyHash))
                            {
                                Console.WriteLine(Console.Title = $"{knownFile.Key.ToString()} Key found: {Encoding.ASCII.GetString(alldataKey)}");
                                var unpackedPath = Path.Combine(Path.GetDirectoryName(args[0]), "Unpacked alldata.bin");
                                Directory.CreateDirectory(unpackedPath);

                                var packer = new MArchivePacker(knownFile.Value.CodecConstructor(), Encoding.ASCII.GetString(alldataKey), knownFile.Value.KeyLength);
                                AllDataPacker.UnpackFiles(alldataPsbPath, unpackedPath, packer);
                                packer.DecompressDirectory(unpackedPath);
                                return 0;
                            }
                        }
                    }
                }

                Console.WriteLine("Key not found.");
                Console.Read();
                return 255;
            }
        }
    }
}
