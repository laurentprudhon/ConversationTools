using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace fasttext
{
    class PredictionResult
    {
        public string Question { get; set;  }
        public int ExpectedIntentIndex { get; set; }

        public int IntentIndex1 { get; set; }
        public float IntentProba1 { get; set; }
        public int IntentIndex2 { get; set; }
        public float IntentProba2 { get; set; }
    }

    class IntentPerf
    {
        public string Intent { get; set; }
        public float Precision { get; set; }
        public float Recall { get; set; }
        public float F1 { get; set; }
    }

    class IntentsPairConfusion
    {
        public int IntentIndex1 { get; set; }
        public int IntentIndex2 { get; set; }

        public int Expected1Found1 { get; set; }
        public int Expected1Found2 { get; set; }
        public int Expected2Found1 { get; set; }
        public int Expected2Found2 { get; set; }

        public float ConfusionRate1To2 { get; set; }
        public float ConfusionRate2To1 { get; set; }
        public float ConfusionErrors { get; set; }
    }

    class FasttextLauncher
    {
        private static string EXECUTABLE_PATH = @"..\..\facebookresearch\fasttext.exe";
        private static string WORKING_DIR = @".\fasttext";
    
        static void Main(string[] args)
        {
            //var result = ExecutableLauncher.ExecuteCommand(EXECUTABLE_PATH, "test model_savings.bin savings.valid", Path.GetFullPath(WORKING_DIR));

            // fasttext.exe supervised -input savings.train -output model_savings -epoch 20 -lr 1 -wordNgrams 2 -ws 10 -dim 300 -pretrainedVectors wiki.fr.vec
            // fasttext.exe quantize model_savings.bin

            string trainingFilePath = @"C:\Users\PRUDHOLU\Documents\GitHubVisualStudio\ConversationTools\dialogtool\bin\Debug\fasttext\insurance.train";
            ISet<string> intentsSet = new HashSet<string>();
            var intentsCountTraining = new Dictionary<string, int>();
            var trainingSamples = new Dictionary<string, IList<string>>();
            using (StreamReader sr = new StreamReader(trainingFilePath))
            {
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    int endOfLabel = line.IndexOf(' ');
                    string intent = line.Substring(9, endOfLabel - 9);
                    string trainingQuestion = line.Substring(endOfLabel + 1);

                    if (!intentsSet.Contains(intent))
                    {
                        intentsSet.Add(intent);
                        intentsCountTraining.Add(intent, 1);
                    }
                    else
                    {
                        intentsCountTraining[intent] += 1;
                    }
                    if(!trainingSamples.ContainsKey(intent))
                    {                        
                        trainingSamples.Add(intent, new List<string>());
                    }
                    trainingSamples[intent].Add(trainingQuestion);
                }
            }

            string validationFilePath = @"C:\Users\PRUDHOLU\Documents\GitHubVisualStudio\ConversationTools\dialogtool\bin\Debug\fasttext\insurance.valid";
            IDictionary<string, string> annotatedQuestions = new Dictionary<string, string>();
            var intentsCountValidation = new Dictionary<string, int>();
            using (StreamReader sr = new StreamReader(validationFilePath))
            {
                string line = null;
                while((line = sr.ReadLine()) != null)
                {
                    int endOfLabel = line.IndexOf(' ');
                    string intent = line.Substring(9, endOfLabel - 9);
                    string question = line.Substring(endOfLabel + 1);

                    if(!intentsSet.Contains(intent))
                    {
                        intentsSet.Add(intent);
                    }
                    if(!intentsCountValidation.ContainsKey(intent))
                    {
                        intentsCountValidation.Add(intent, 1);                        
                    }
                    else
                    {
                        intentsCountValidation[intent] += 1;
                    }
                    if (!annotatedQuestions.ContainsKey(question))
                    {
                        annotatedQuestions.Add(question, intent);
                    }
                }
            }
            IList<string> intents = new List<string>(intentsSet.OrderBy(i => i));
            IDictionary<string, int> intentsIndexes = new Dictionary<string, int>();
            for(int i = 0; i < intents.Count; i++)
            {
                var intent = intents[i];
                intentsIndexes.Add(intent, i);
            }

            int[,] confusionMatrix = new int[intents.Count, intents.Count];
            var predictionResults = new List<PredictionResult>();

            var process = ExecutableLauncher.LaunchCommand(EXECUTABLE_PATH, "predict-prob model_insurance.bin - 2", Path.GetFullPath(WORKING_DIR));
            foreach(var question in annotatedQuestions.Keys)
            {
                var annotatedIntent = annotatedQuestions[question];
                var annotatedIntentIndex = intentsIndexes[annotatedIntent];

                ExecutableLauncher.SendInputLine(process, question);
                var output = ExecutableLauncher.ReadOutputLine(process);
                string[] results = output.Split(' ');
                var intent1 = results[0].Substring(9);
                var strproba1 = results[1];
                var intent2 = results[2].Substring(9);
                var strproba2 = results[3];

                var predictionResult = new PredictionResult();
                predictionResult.Question = question;
                predictionResult.ExpectedIntentIndex = annotatedIntentIndex;
                predictionResult.IntentIndex1 = intentsIndexes[intent1];
                predictionResult.IntentProba1 = float.Parse(strproba1, CultureInfo.InvariantCulture.NumberFormat);
                predictionResult.IntentIndex2 = intentsIndexes[intent2];
                predictionResult.IntentProba2 = float.Parse(strproba2, CultureInfo.InvariantCulture.NumberFormat);
                predictionResults.Add(predictionResult);

                confusionMatrix[annotatedIntentIndex, predictionResult.IntentIndex1]++;
            }
            ExecutableLauncher.Kill(process);

            var intentsPerfs = new List<IntentPerf>();
            for(int intentIndex = 0; intentIndex < intents.Count; intentIndex++)
            {
                string intent = intents[intentIndex];
                int truePositives = confusionMatrix[intentIndex, intentIndex];
                int falsePositives = 0;
                for(int j=0; j<intents.Count;j++)
                {
                    if (j != intentIndex) falsePositives += confusionMatrix[j, intentIndex];
                }
                int falseNegatives = 0;
                for (int j = 0; j < intents.Count; j++)
                {
                    if (j != intentIndex) falseNegatives += confusionMatrix[intentIndex,j];
                }

                var intentPerf = new IntentPerf();
                intentPerf.Intent = intent;
                if ((truePositives + falsePositives) > 0)
                {
                    intentPerf.Precision = (float)truePositives / (truePositives + falsePositives);
                }
                else
                {
                    intentPerf.Precision = -1;
                }
                if ((truePositives + falseNegatives) > 0)
                {
                    intentPerf.Recall = (float)truePositives / (truePositives + falseNegatives);
                }
                else
                {
                    intentPerf.Recall = -1;
                }
                if (intentPerf.Precision > 0 && intentPerf.Recall > 0)
                {
                    intentPerf.F1 = 2 * intentPerf.Precision * intentPerf.Recall / (intentPerf.Precision + intentPerf.Recall);
                }
                else
                {
                    intentPerf.F1 = -1;
                }
                intentsPerfs.Add(intentPerf);
            }

            var intentsConfusion = new List<IntentsPairConfusion>();
            for (int expectedIntentIndex = 0; expectedIntentIndex < intents.Count; expectedIntentIndex++)
            {
                for (int predictedIntentIndex = 0; predictedIntentIndex < expectedIntentIndex; predictedIntentIndex++)
                {
                    var intentsPair = new IntentsPairConfusion();
                    intentsPair.IntentIndex1 = expectedIntentIndex;
                    intentsPair.IntentIndex2 = predictedIntentIndex;
                    intentsPair.Expected1Found1 = confusionMatrix[expectedIntentIndex, expectedIntentIndex];
                    intentsPair.Expected1Found2 = confusionMatrix[expectedIntentIndex, predictedIntentIndex];
                    intentsPair.Expected2Found1 = confusionMatrix[predictedIntentIndex, expectedIntentIndex];
                    intentsPair.Expected2Found2 = confusionMatrix[predictedIntentIndex, predictedIntentIndex];
                    intentsPair.ConfusionErrors = intentsPair.Expected1Found2 + intentsPair.Expected2Found1;
                    if (intentsPair.Expected1Found1 > 0)
                    {
                        intentsPair.ConfusionRate1To2 = (float)intentsPair.Expected1Found2 / intentsPair.Expected1Found1;
                    }
                    else if(intentsPair.Expected1Found2 > 0)
                    {
                        intentsPair.ConfusionRate1To2 = 1;
                    }
                    if (intentsPair.Expected2Found2 > 0)
                    {
                        intentsPair.ConfusionRate2To1 = (float)intentsPair.Expected2Found1 / intentsPair.Expected2Found2;
                    }
                    else if(intentsPair.Expected2Found1 > 0)
                    {
                        intentsPair.ConfusionRate2To1 = 1;
                    }
                    intentsConfusion.Add(intentsPair);
                }
            }

            
            using (StreamWriter sw = new StreamWriter(@"C:\Users\PRUDHOLU\Documents\GitHubVisualStudio\ConversationTools\dialogtool\bin\Debug\fasttext\insurance.results.csv", false, Encoding.GetEncoding("iso8859-1")))
            {
                sw.WriteLine("1. Intents performance");
                sw.WriteLine();
                sw.WriteLine("Intent;# Training;# Validation;F1;Precision;Recall");
                foreach(var intentPerf in intentsPerfs.OrderByDescending(perf => perf.F1))
                {
                    var trainingCount = 0;
                    intentsCountTraining.TryGetValue(intentPerf.Intent, out trainingCount);
                    var validationCount = 0;
                    intentsCountValidation.TryGetValue(intentPerf.Intent, out validationCount);

                    sw.Write(intentPerf.Intent);
                    sw.Write(';');
                    sw.Write(trainingCount);
                    sw.Write(';');
                    sw.Write(validationCount);
                    sw.Write(';');
                    sw.Write(intentPerf.F1 < 0 ? "N/A" : intentPerf.F1.ToString("N2"));
                    sw.Write(';');
                    sw.Write(intentPerf.Precision < 0 ? "N/A" : intentPerf.Precision.ToString("N2"));
                    sw.Write(';');
                    sw.Write(intentPerf.Recall < 0 ? "N/A" : intentPerf.Recall.ToString("N2"));
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine("2. Intents confusion (top 50)");
                sw.WriteLine();
                sw.WriteLine("Intent 1;Intent 2;# Confusions;# Confusion 1>2;% Confusion 1>2;# Confusion 2>1;% Confusion 2>1");
                foreach (var intentPair in intentsConfusion.OrderByDescending(pair => pair.ConfusionErrors).Take(50))
                {
                    sw.Write(intents[intentPair.IntentIndex1]);
                    sw.Write(';');
                    sw.Write(intents[intentPair.IntentIndex2]);
                    sw.Write(';');
                    sw.Write(intentPair.ConfusionErrors);
                    sw.Write(';');
                    sw.Write(intentPair.Expected1Found2);
                    sw.Write(';');
                    sw.Write(intentPair.ConfusionRate1To2.ToString("N2"));
                    sw.Write(';');
                    sw.Write(intentPair.Expected2Found1);
                    sw.Write(';');
                    sw.Write(intentPair.ConfusionRate2To1.ToString("N2"));
                    sw.WriteLine();
                }
                sw.WriteLine();

                sw.WriteLine();                
                sw.WriteLine();

                sw.WriteLine("3. Detailed error analysis for each intent");
                sw.WriteLine();
                for (int expectedIntentIndex = 0; expectedIntentIndex < intents.Count; expectedIntentIndex++)
                {
                    var expectedIntent = intents[expectedIntentIndex];

                    sw.WriteLine(">>> " + expectedIntent);
                    sw.WriteLine();

                    sw.WriteLine("Training set questions");
                    if (trainingSamples.ContainsKey(expectedIntent))
                    {
                        foreach (var trainingQuestion in trainingSamples[expectedIntent])
                        {
                            sw.WriteLine(trainingQuestion);
                        }
                    }
                    else
                    {
                        sw.WriteLine("-- NONE --");
                    }
                    sw.WriteLine();

                    sw.WriteLine("Questions correctly classified in this class");
                    foreach(var predictionResult in predictionResults.Where(pred => pred.ExpectedIntentIndex == expectedIntentIndex && pred.IntentIndex1 == expectedIntentIndex).OrderByDescending(pred => pred.IntentProba1))
                    {
                        sw.Write(predictionResult.Question.Replace(';', ' '));
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1 - predictionResult.IntentProba2);
                        sw.WriteLine();
                    }
                    sw.WriteLine();

                    sw.WriteLine("Questions from this class incorrectly classified in another class");
                    foreach (var predictionResult in predictionResults.Where(pred => pred.ExpectedIntentIndex == expectedIntentIndex && pred.IntentIndex1 != expectedIntentIndex).OrderBy(pred => pred.IntentIndex1).ThenByDescending(pred => pred.IntentProba1))
                    {
                        sw.Write(predictionResult.Question.Replace(';', ' '));
                        sw.Write(';');
                        sw.Write(intents[predictionResult.IntentIndex1]);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1 - predictionResult.IntentProba2);
                        sw.Write(';');
                        sw.Write(intents[predictionResult.IntentIndex2]);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba2);
                        sw.WriteLine();
                    }
                    sw.WriteLine();

                    sw.WriteLine("Questions from another class incorrectly classified in this class");
                    foreach (var predictionResult in predictionResults.Where(pred => pred.ExpectedIntentIndex != expectedIntentIndex && pred.IntentIndex1 == expectedIntentIndex).OrderBy(pred => pred.ExpectedIntentIndex).ThenByDescending(pred => pred.IntentProba1))
                    {
                        sw.Write(predictionResult.Question.Replace(';', ' '));
                        sw.Write(';');
                        sw.Write(intents[predictionResult.ExpectedIntentIndex]);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba1 - predictionResult.IntentProba2);
                        sw.Write(';');
                        sw.Write(intents[predictionResult.IntentIndex2]);
                        sw.Write(';');
                        sw.Write(predictionResult.IntentProba2);
                        sw.WriteLine();
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}
