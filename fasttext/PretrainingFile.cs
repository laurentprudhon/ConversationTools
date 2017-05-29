using dialogtool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fasttext
{
    class PretrainingFile
    {
        static void Main(string[] args)
        {
            string answersDir = @".\answers\";
            string answerUnitsFile = "au_epargne.json";
            string pretrainingFilePath = "savings_pretraining.txt";
            GenerateFasttextPretrainingFileFromJsonDocuments(answersDir, answerUnitsFile, pretrainingFilePath);

            /*string pretrainingFilePath = @".\fasttext\wiki.fr";
            int numberOfWordEmbeddings = 400000;

            ExtractMostFrequentWordEmbeddingsFromWordVectorsFile(pretrainingFilePath, numberOfWordEmbeddings);*/
        }
        
        public static void GenerateFasttextPretrainingFileFromJsonDocuments(string answersDir, string answerUnitsFile, string pretrainingFilePath)
        {
            var documents = new AnswerStoreSimulator(answerUnitsFile, answersDir);

            using (StreamWriter sw = new StreamWriter(pretrainingFilePath, false, Encoding.UTF8))
            {
                foreach (var paragraph in documents.AnswerUnits)
                {
                    foreach (var title in paragraph.content.title)
                    {
                        if (title != "no-title")
                        {
                            sw.WriteLine(SentenceClassifier.PreprocessSentence(title));
                        }
                    }
                    sw.WriteLine(SentenceClassifier.PreprocessSentence(paragraph.content.plainText));
                }
            }
        }

        public static void GenerateFasttextTrainingFileFromCsvTable(string csvFilePath, string pretrainingFilePath)
        {
            if (File.Exists(csvFilePath))
            {
                using (StreamReader sr = new StreamReader(csvFilePath, Encoding.UTF8))
                using (StreamWriter sw = new StreamWriter(pretrainingFilePath, false, Encoding.UTF8))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        int intentIndex = line.LastIndexOf(',');
                        if (intentIndex < 0) throw new Exception("invalid file format");
                        var question = line.Substring(0, intentIndex);

                        sw.WriteLine(SentenceClassifier.PreprocessSentence(question));
                    }
                }
            }
        }

        public static void ExtractMostFrequentWordEmbeddingsFromWordVectorsFile(string pretrainingFilePath, int numberOfWordEmbeddings)
        {
            int lineCount = 0;
            using (StreamReader sr = new StreamReader(pretrainingFilePath + ".vec", Encoding.UTF8))
            {
                using (StreamWriter sw = new StreamWriter(pretrainingFilePath + "." + numberOfWordEmbeddings + ".vec", false, Encoding.GetEncoding("iso8859-1")))
                {
                    string line = null;
                    sr.ReadLine();
                    sw.WriteLine(numberOfWordEmbeddings + " " + 300);
                    while ((line = sr.ReadLine()) != null)
                    {
                        sw.WriteLine(line);
                        lineCount++;
                        if (lineCount >= numberOfWordEmbeddings) break;
                    }
                }
            }
        }
    }
}
