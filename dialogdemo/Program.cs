using dialogtool;
using fasttext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace dialogdemo
{
    class Program
    {
        private static string DIALOG_DIR = @".\dialog\";

        static void Main(string[] args)
        {
            var selectInsurance = true; 

            var INTENTS_INSURANCE_MODEL_FILE_NAME = "model_demo_insurance.ftz";
            var INTENTS_SAVINGS_MODEL_FILE_NAME = "model_demo_savings.ftz";

            var DIALOG_INSURANCE_FILE_NAME = "MCT-Account-ins_ded_prod_patch2_0703.xml";
            var DIALOG_SAVINGS_FILE_NAME = "MCT-Account-sav_ded_uat_cmne_1105.xml";

            var ANSWERS_INSURANCE_FILE_NAME = "au_assurance.json";
            var ANSWERS_SAVINGS_FILE_NAME = "au_epargne.json";

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("EURO ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("INFORMATION");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - Démo Assistant Virtuel " + (selectInsurance?"Assurance Auto-IRD":"Epargne") + " autonome");

            Stopwatch chrono = new Stopwatch();
            chrono.Start();
            using (SentenceClassifier classifier = new SentenceClassifier(selectInsurance ? INTENTS_INSURANCE_MODEL_FILE_NAME : INTENTS_SAVINGS_MODEL_FILE_NAME))
            {
                classifier.PredictLabels("test");
                chrono.Stop();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Intents model loaded in " + chrono.ElapsedMilliseconds + " ms, C++ memory usage " + classifier.GetWorkingSetMo() + " Mo");

                chrono.Restart();
                var dialogFile = new DialogFile(new FileInfo(DIALOG_DIR + (selectInsurance ? DIALOG_INSURANCE_FILE_NAME : DIALOG_SAVINGS_FILE_NAME)));
                Dialog dialog = dialogFile.Read();
                chrono.Stop();
                Console.WriteLine("Dialog configuration loaded in " + chrono.ElapsedMilliseconds + " ms, C# memory usage " + (Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024) + " Mo");

                chrono.Restart();
                var answerStore = new AnswerStoreSimulator(selectInsurance ? ANSWERS_INSURANCE_FILE_NAME : ANSWERS_SAVINGS_FILE_NAME);
                chrono.Stop();
                Console.WriteLine(answerStore.AnswerUnits.Count + " Answer units loaded in " + chrono.ElapsedMilliseconds + " ms, C# memory usage " + (Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024) + " Mo");
                Console.WriteLine("");

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("@ Bonjour, vous pouvez me poser des questions dans le domaine de l'épargne.");
                Console.WriteLine("@ (puis saisissez \"exit\" pour sortir du programme)");
                Console.WriteLine();
                for (;;)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("@ Quelle est votre question ?");
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("> ");
                    string userInputText = Console.ReadLine();
                    Console.WriteLine();
                    if (userInputText == "exit")
                    {
                        break;
                    }

                    chrono.Restart();
                    var intentResult = classifier.PredictLabels(userInputText);
                    chrono.Stop();

                    bool isFatHead = intentResult.Proba1 > 0.5;
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    if (isFatHead)
                    {
                        Console.WriteLine("@ J'ai compris que votre intention est : ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  " + intentResult.Label1);
                    }
                    else
                    {
                        Console.WriteLine("@ Je pense que votre intention est : ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  " + intentResult.Label1);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("@  ou peut-être ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("  " + intentResult.Label2);
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("@  mais je n'en suis pas sûr -> je vous redirige vers le mode Recherche ...");
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
                    long timeInMicrosec = (chrono.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L)));
                    Console.WriteLine("Intents detected in " + (timeInMicrosec>1000?(timeInMicrosec/1000).ToString():("0."+timeInMicrosec)) + " ms");
                    Console.WriteLine();

                    if (isFatHead)
                    {
                        DialogExecutionResult dialogResult = null;
                        DisambiguationQuestion lastDisambiguationQuestion = null;
                        do
                        {
                            chrono.Restart();
                            if (dialogResult == null)
                            {
                                dialogResult = DialogInterpreter.AnalyzeInitialQuestion(dialog, String.Empty, userInputText, intentResult.Label1);
                            }
                            else
                            {
                                dialogResult = DialogInterpreter.ExecuteUserInputNode(dialog, lastDisambiguationQuestion, userInputText, dialogResult);
                                lastDisambiguationQuestion = null;
                            }
                            chrono.Stop();

                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            var nodeExecution = dialogResult.ExecutionResult;
                            if (nodeExecution is FatHeadAnswerNodeExecution)
                            {
                                var mappingUri = ((FatHeadAnswerNodeExecution)nodeExecution).MappingURI;
                                if (!selectInsurance) mappingUri = "/federationGroup/CM" + mappingUri;
                                Console.WriteLine("@ J'ai trouvé une réponse exacte à cette question : ");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("  " + mappingUri);

                                var answerUnit = answerStore.GetAnswerUnitForMappingUri(mappingUri);
                                if (answerUnit != null)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.WriteLine();
                                    foreach (string title in answerUnit.content.title)
                                    {
                                        if (title != "no-title")
                                        {
                                            Console.WriteLine("# " + title);
                                        }
                                    }
                                    var text = answerUnit.content.plainText;
                                    if (text.Length > 800) text = text.Substring(0, 795) + " ...";
                                    Console.WriteLine(text);
                                    Console.WriteLine();
                                }
                            }
                            else if (nodeExecution.DialogNode is DisambiguationQuestion)
                            {
                                lastDisambiguationQuestion = ((DisambiguationQuestion)nodeExecution.DialogNode);
                                Console.WriteLine("@ Votre question est ambigüe, je dois vous demander en complément : ");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("  " + lastDisambiguationQuestion.QuestionText);
                                if (lastDisambiguationQuestion.DisambiguationOptions != null)
                                {
                                    foreach (var option in lastDisambiguationQuestion.DisambiguationOptions)
                                    {
                                        Console.WriteLine("  - " + option.Text + " ["+option.EntityValue.CanonicalValue+"]");
                                    }
                                }
                                Console.WriteLine();

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write("> ");
                                userInputText = Console.ReadLine();
                                Console.WriteLine();
                            }
                            else if (nodeExecution.DialogNode.Type == DialogNodeType.RedirectToLongTail)
                            {
                                Console.WriteLine("@ Dans ce cas de figure, le dialogue vous redirige vers le mode Recherche ...");
                            }
                            else if (nodeExecution.DialogNode is DirectAnswer)
                            {
                                var direct = ((DirectAnswer)nodeExecution.DialogNode);
                                Console.WriteLine("@ Je dois afficher le message suivant : ");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("  " + direct.MessageText);
                            }
                            else
                            {

                            }
                            Console.ForegroundColor = ConsoleColor.Gray;
                            timeInMicrosec = (chrono.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L)));
                            Console.WriteLine("Dialog interpreted in " + (timeInMicrosec > 1000 ? (timeInMicrosec / 1000).ToString() : ("0." + timeInMicrosec)) + " ms");
                            Console.WriteLine();

                        } while (lastDisambiguationQuestion != null);
                    }
                }
                
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("@ Aurevoir.");
            }           
        }
    }
}
