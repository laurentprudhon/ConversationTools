using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace fasttext
{
    public class SentenceClassifier : IDisposable
    {
        private static string EXECUTABLE_PATH = @".\facebookresearch\fasttext.exe";
        private static string MODEL_DIR = @".\intents\";
        
        public static void TrainModel()
        {
            // fasttext.exe supervised -input savings.train -output model_savings -epoch 5 -lr 1 -wordNgrams 2 -dim 300 -pretrainedVectors wiki.fr.200000.vec
            // fasttext.exe quantize -output model_savings1 -input savings.train -qnorm -cutoff 400000
            throw new NotImplementedException();
        }

        public static void TestModel()
        {
            // fasttext.exe test model_savings1.ftz SAV_NLC_100520171.valid
            // var result = ExecutableLauncher.ExecuteCommand(EXECUTABLE_PATH, "test model_savings.bin savings.valid", Path.GetFullPath(WORKING_DIR));
            throw new NotImplementedException();
        }
        
        private Process fastTextPredictionProcess;

        public SentenceClassifier(string modelFileName)
        {
            fastTextPredictionProcess = ExecutableLauncher.LaunchCommand(EXECUTABLE_PATH, "predict-prob " + modelFileName + " - 2", Path.GetFullPath(MODEL_DIR));
        }

        public ClassifierResult PredictLabels(string sentence)
        {
            if(fastTextPredictionProcess == null)
            {
                throw new Exception("Sentence classifier was already disposed");
            }

            var preprocessedText = PreprocessSentence(sentence);
            ExecutableLauncher.SendInputLine(fastTextPredictionProcess, preprocessedText);
            var output = ExecutableLauncher.ReadOutputLine(fastTextPredictionProcess);
            string[] results = output.Split(' ');

            var result = new ClassifierResult();
            if (results.Length >= 2)
            {
                result.Label1 = results[0].Substring(9);
                result.Proba1 = float.Parse(results[1], CultureInfo.InvariantCulture.NumberFormat);
            }
            if (results.Length >= 4)
            {
                result.Label2 = results[2].Substring(9);
                result.Proba2 = float.Parse(results[3], CultureInfo.InvariantCulture.NumberFormat);
            }

            return result;
        }

        public int GetWorkingSetMo()
        {
            if (fastTextPredictionProcess == null)
            {
                throw new Exception("Sentence classifier was already disposed");
            }

            return ((int)(fastTextPredictionProcess.WorkingSet64 / 1024 / 1024));
        }

        public void Dispose()
        {
            ExecutableLauncher.Kill(fastTextPredictionProcess);
            fastTextPredictionProcess = null;
        }

        public static string PreprocessSentence(string sentence)
        {
            StringBuilder sbResult = new StringBuilder();

            bool previousCharNonSpace = false;
            int lastIndex = sentence.Length - 1;
            for (int i=0; i<=lastIndex; i++)
            {
                var chr = sentence[i];
                if (Char.IsLetter(chr))
                {
                    sbResult.Append(Char.ToLower(chr));
                    previousCharNonSpace = true;
                }
                else if (Char.IsDigit(chr))
                {
                    switch (chr)
                    {
                        case '0':
                            sbResult.Append("zero ");
                            break;
                        case '1':
                            sbResult.Append("un ");
                            break;
                        case '2':
                            sbResult.Append("deux ");
                            break;
                        case '3':
                            sbResult.Append("trois ");
                            break;
                        case '4':
                            sbResult.Append("quatre ");
                            break;
                        case '5':
                            sbResult.Append("cinq ");
                            break;
                        case '6':
                            sbResult.Append("six ");
                            break;
                        case '7':
                            sbResult.Append("sept ");
                            break;
                        case '8':
                            sbResult.Append("huit ");
                            break;
                        case '9':
                            sbResult.Append("neuf ");
                            break;
                    }
                    previousCharNonSpace = false;
                }
                else if(chr == '.' || chr == '?' || chr == '!')
                {
                    if(i == lastIndex)
                    {
                        continue;
                    }
                    else
                    {
                        var nextChr = sentence[i + 1];
                        if(Char.IsLetterOrDigit(nextChr))
                        {
                            sbResult.Append(chr);
                        }
                        else
                        {
                            sbResult.Append('\n');
                        }
                        previousCharNonSpace = false;
                    }
                }
                else if(chr == '\n')
                {
                    sbResult.Append('\n');
                    previousCharNonSpace = false;
                }
                else
                {
                    if (previousCharNonSpace && i < lastIndex)
                    {
                        sbResult.Append(' ');
                        previousCharNonSpace = false;
                    }
                }
            }

            return sbResult.ToString();
        }
    }

    public class ClassifierResult
    {
        public string Label1 { get; set; }
        public float Proba1 { get; set; }

        public string Label2 { get; set; }
        public float Proba2 { get; set; }
    }
}
