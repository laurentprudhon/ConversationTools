using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace preprocessingtool
{
    class FasttextPretrainingFile
    {
        public static void HeadOfPretrainingFile()
        {
            string pretrainingFilePath = @".\fasttext\wiki.fr";
            int lineCount = 0;
            int lineMax = 400000;
            using (StreamReader sr = new StreamReader(pretrainingFilePath + ".vec", Encoding.UTF8))
            {
                using (StreamWriter sw = new StreamWriter(pretrainingFilePath + "." + lineMax + ".vec", false, Encoding.GetEncoding("iso8859-1")))
                {
                    string line = null;
                    sr.ReadLine();
                    sw.WriteLine(lineMax + " " + 300);
                    while ((line = sr.ReadLine()) != null)
                    {
                        sw.WriteLine(line);
                        lineCount++;
                        if (lineCount >= lineMax) break;
                    }
                }
            }
        }
    }
}
