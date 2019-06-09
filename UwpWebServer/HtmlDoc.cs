using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UwpWebServer
{
    public sealed class HtmlDoc
    {

        public HtmlDoc()
        {
            ReadFile();
        }

        public string ResponseText { get; private set; }

        void ReadFile()
        {
            try
            {
                string basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string s = basePath.Remove(basePath.Length - 10);
                string path = $"{s}\\response.html";
                using (StreamReader sr = new StreamReader(path))
                {
                    ResponseText = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
    }
}
