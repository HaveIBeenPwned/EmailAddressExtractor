using System.IO.Compression;
using System.Text;
using System.Xml;

namespace MyAddressExtractor.Objects.Readers {
    internal sealed class OpenDocumentTextReader : PlainTextReader
    {
        private static string DestinationPath = Path.GetTempFileName();

        public OpenDocumentTextReader(string zipPath)
            : base(ExtractContent(zipPath))
        {

        }

        private static string ExtractContent(string zipPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals("content.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        using(var writer = new StreamWriter(DestinationPath, true, Encoding.UTF8, 4096))
                        {
                            using(var reader = new XmlTextReader(entry.Open()))
                            {
                                while(reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Text ||
                                        reader.NodeType == XmlNodeType.CDATA)
                                    {
                                        writer.WriteLine(reader.ReadContentAsString());
                                    }
                                }
                            }
                        }

                        return DestinationPath;
                    }
                }
            }
            
            throw new Exception($"Unable to load content.xml from '{zipPath}'");
        }

        public async override ValueTask DisposeAsync()
        {
            if(Path.Exists(OpenDocumentTextReader.DestinationPath))
            {
                File.Delete(OpenDocumentTextReader.DestinationPath);
            }

            await base.DisposeAsync();
        }
    }
}