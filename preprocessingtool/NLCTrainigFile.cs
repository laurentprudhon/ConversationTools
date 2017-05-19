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

            int splitTrainingSets = 1;
            if(!String.IsNullOrEmpty(args[1]))
            {
                splitTrainingSets = Int32.Parse(args[1]);
            }

            GenerateFasttextTrainingFileFromCsvTable(csvFilePath, splitTrainingSets);
        }

        private static string FASTTEXT_LABEL_PREFIX = "__label__";

        class LabelAndQuestion
        {
            public string Label { get; set; }
            public string Question { get; set; }
        }

        public static void GenerateFasttextTrainingFileFromCsvTable(string csvFilePath, int splitTrainingSets)
        {
            if (File.Exists(csvFilePath))
            {
                Console.WriteLine("Reading file : " + csvFilePath + " ...");
                int lineCount = 0;
                var questions = new List<LabelAndQuestion>();
                using (StreamReader sr = new StreamReader(csvFilePath, Encoding.GetEncoding("iso8859-1")))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] columns = line.Split(',');
                        var labelAndQuestion = new LabelAndQuestion();
                        labelAndQuestion.Question = columns[0];
                        labelAndQuestion.Label = columns[1];
                        questions.Add(labelAndQuestion);
                    }
                }
                Shuffle(questions);

                int bucketQuestionsCount = questions.Count / splitTrainingSets;

                string csvFileName = Path.GetFileNameWithoutExtension(csvFilePath);
                string csvFileDirectory = Path.GetDirectoryName(csvFilePath);
                for (int trainingSetNumber = 1; trainingSetNumber <= splitTrainingSets; trainingSetNumber++)
                {
                    string trainingFilePath = csvFileDirectory + Path.DirectorySeparatorChar + csvFileName + trainingSetNumber + ".train";
                    string validationFilePath = csvFileDirectory + Path.DirectorySeparatorChar + csvFileName + trainingSetNumber + ".valid";
                    using (StreamWriter trainsw = new StreamWriter(trainingFilePath, false, Encoding.UTF8))
                    {
                        using (StreamWriter validsw = new StreamWriter(validationFilePath, false, Encoding.UTF8))
                        {
                            for (int questionIndex = 0; questionIndex < questions.Count; questionIndex++)
                            {
                                var labelAndQuestion = questions[questionIndex];

                                StringBuilder sbQuestion = new StringBuilder();
                                sbQuestion.Append(FASTTEXT_LABEL_PREFIX);
                                sbQuestion.Append(labelAndQuestion.Label);
                                sbQuestion.Append(' ');
                                bool previousCharNonSpace = false;
                                foreach (var chr in labelAndQuestion.Question)
                                {
                                    if (Char.IsLetter(chr))
                                    {
                                        sbQuestion.Append(Char.ToLower(chr));
                                        previousCharNonSpace = true;
                                    }
                                    else if (Char.IsDigit(chr))
                                    {
                                        switch (chr)
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
                                        if (previousCharNonSpace)
                                        {
                                            sbQuestion.Append(' ');
                                        }
                                    }
                                }

                                bool writeToValidation = questionIndex >= (trainingSetNumber - 1) * bucketQuestionsCount && questionIndex < trainingSetNumber * bucketQuestionsCount;
                                if (!writeToValidation)
                                {
                                    trainsw.WriteLine(sbQuestion.ToString());
                                }
                                else
                                {
                                    validsw.WriteLine(sbQuestion.ToString());
                                }
                                lineCount++;
                            }
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

        private static Random rng = new Random();

        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
