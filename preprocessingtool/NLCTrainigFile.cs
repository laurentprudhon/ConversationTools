using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace preprocessingtool
{
    public class NLCTrainigFile
    {
        static void Main(string[] args)
        {
            string csvFilePath = args[0];
            GenerateFasttextTrainingFileFromCsvTable(csvFilePath);
        }

        private static string FASTTEXT_LABEL_PREFIX = "__label__";

        public static void GenerateFasttextTrainingFileFromCsvTable(string csvFilePath)
        {
            if (File.Exists(csvFilePath))
            {
                Console.WriteLine("Reading file : " + csvFilePath + " ...");
                using (StreamReader sr = new StreamReader(csvFilePath, Encoding.GetEncoding("iso8859-1")))
                {
                    string csvFileName = Path.GetFileNameWithoutExtension(csvFilePath);
                    string csvFileDirectory = Path.GetDirectoryName(csvFilePath);
                    string trainingFilePath = csvFileDirectory + Path.DirectorySeparatorChar + csvFileName + ".txt";

                    int lineCount = 0;
                    using (StreamWriter sw = new StreamWriter(trainingFilePath, false, Encoding.UTF8))
                    {
                        string line = null;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] columns = line.Split(';');
                            string question = columns[0];
                            string label = columns[1];

                            StringBuilder sbQuestion = new StringBuilder();
                            sbQuestion.Append(FASTTEXT_LABEL_PREFIX);
                            sbQuestion.Append(label);
                            sbQuestion.Append(' ');
                            bool previousCharNonSpace = false;
                            foreach(var chr in question)
                            {
                                if(Char.IsLetter(chr))
                                {
                                    sbQuestion.Append(Char.ToLower(chr));
                                    previousCharNonSpace = true;
                                }
                                else if(Char.IsDigit(chr))
                                {
                                    switch(chr)
                                    {
                                        case '0':
                                            sbQuestion.Append("zero ");
                                            break;
                                        case '1':
                                            sbQuestion.Append("un ");
                                            break;
                                        case '2':
                                            sbQuestion.Append("deux ");
                                            break;
                                        case '3':
                                            sbQuestion.Append("trois ");
                                            break;
                                        case '4':
                                            sbQuestion.Append("quatre ");
                                            break;
                                        case '5':
                                            sbQuestion.Append("cinq ");
                                            break;
                                        case '6':
                                            sbQuestion.Append("six ");
                                            break;
                                        case '7':
                                            sbQuestion.Append("sept ");
                                            break;
                                        case '8':
                                            sbQuestion.Append("huit ");
                                            break;
                                        case '9':
                                            sbQuestion.Append("neuf ");
                                            break;
                                    }
                                    previousCharNonSpace = true;
                                }
                                else
                                {
                                    if(previousCharNonSpace)
                                    {
                                        sbQuestion.Append(' ');
                                    }
                                }
                            }
                            sw.WriteLine(sbQuestion.ToString());
                            lineCount++;
                        }                        
                    }
                    Console.WriteLine("OK - " + lineCount + " training samples written to " + trainingFilePath);
                }
            }
            else
            {
                Console.WriteLine("ERROR : File " + csvFilePath + " doesn't exist");
            }
        }
    }
}
