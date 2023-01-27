using System;
using System.IO;
using System.Text;
using GMWare.M2.MArchive;

namespace m2_unpacker
{
    internal class Program
    {
        static string findKey(byte[] data)
        {
            uint[] p = new uint[8];
            uint pi = 0;
            for (uint i = 3; i < data.Length; i += 4)
            {
                if ((data[i]==0)&&(data[i+1]!=0))
                {
                    if ((i-p[pi])==0x18)
                    {
                        uint chsm = 0;
                        for (uint j = 1; j < 4; ++j)
                            chsm = chsm*2 + data[p[pi]+j];
                        if (chsm==0x331)
                        {
                            for (int j = 4; j < 0x18; ++j)
                                chsm = chsm*2 + data[p[pi]+j];
                            if (chsm==0x39ba8fcc)
                            {
                                string key = null;
                                for (int j = 6; j > 0; --j)
                                {
                                    int s = (int)p[(pi+j+0)&7];
                                    int e = (int)p[(pi+j+1)&7];
                                    while (data[e]==0)
                                        --e;
                                    key = Encoding.ASCII.GetString(data, s+1, e-s);
                                    if (key.IndexOf("data")<0)
                                        return key;
                                }
                                return null;
                            }
                        }
                    }
                    pi = (pi+1)&7;
                    p[pi] = i;
                }
            }
            return null;
        }

        static string findBinary(string path)
        {
	            if (path.Substring(path.Length - 4) == ".exe")
                return path;

            path = Path.GetDirectoryName(path);
            string[] dirs = {path, Path.Combine(path, "..")};
            string[] names = {"m2engage", "game", "NMA1", "NMA2"};
            string[] ext = {"", ".exe"};

            for (uint i = 0; i < dirs.Length; ++i)		
            {
                for (uint j = 0; j < names.Length; ++j)
                {
                    string name = Path.Combine(dirs[i], names[j]);
                    for (uint k = 0; k < ext.Length; ++k)
                    {
                        string res = name + ext[k];
                        if (File.Exists(res))
                            return res;
                    }
                }
            }
            return Path.Combine(dirs[0], names[0]);
        }

        static int notMain(string[] args)
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
            string m2Path = findBinary(args[0]);

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

            string key = findKey(File.ReadAllBytes(m2Path));
            if (key==null)
            {
                Console.WriteLine("m2engage key not found.");
                return 6;
            }

            Console.WriteLine(Console.Title = $"m2engage key found: {key}");

            string type = Encoding.ASCII.GetString(File.ReadAllBytes(alldataPsbPath), 0, 3);
            IMArchiveCodec codec = null;
            if (type=="mdf")
                codec = new ZlibCodec();
            if (type=="mfl")
                codec = new FastLzCodec();
            if (type=="mzs")
                codec = new ZStandardCodec();

            if (codec==null)
            {
                Console.WriteLine($"Unknown compression: {type}");
                return 7;
            }
        }
    }

            var unpackedPath = Path.Combine(Path.GetDirectoryName(args[0]), "Unpacked alldata.bin");
            Directory.CreateDirectory(unpackedPath);

            var packer = new MArchivePacker(codec, key, 64);
            AllDataPacker.UnpackFiles(alldataPsbPath, unpackedPath, packer);
            packer.DecompressDirectory(unpackedPath);
            Console.WriteLine("");
            Console.WriteLine("done!");
            return 0;
        }

        static int Main(string[] args)
        {
            int res = notMain(args);
            Console.Read();
            return res;
}
