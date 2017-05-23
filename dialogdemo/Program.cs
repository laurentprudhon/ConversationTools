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
        static void Main(string[] args)
        {
            var INTENTS_MODEL_FILE_NAME = "model_demo_savings.ftz";
            var DIALOG_FILE_NAME = "WDD-Account-sav_ded_dev_ttc2_nov14-300.xml";

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("EURO ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("INFORMATION");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - Démonstration Assistant Virtuel autonome");

            Stopwatch chrono = new Stopwatch();
            chrono.Start();
            using (SentenceClassifier classifier = new SentenceClassifier(INTENTS_MODEL_FILE_NAME))
            {
                classifier.PredictLabels("test");
                chrono.Stop();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Intents model loaded in " + chrono.ElapsedMilliseconds + " ms, memory usage " + classifier.GetWorkingSetMo() + " Mo");

                chrono.Restart();
                var dialogFile = new DialogFile(new FileInfo(@".\dialog\" + DIALOG_FILE_NAME));
                Dialog dialog = dialogFile.Read();
                chrono.Stop();
                Console.WriteLine("Dialog configuration loaded in " + chrono.ElapsedMilliseconds + " ms, memory usage " + (Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024) + " Mo");
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
                    string question = Console.ReadLine();
                    Console.WriteLine();
                    if (question == "exit")
                    {
                        break;
                    }

                    chrono.Restart();
                    var intentResult = classifier.PredictLabels(question);
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
                    Console.WriteLine("Intents detected in " + (chrono.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L))) + " microsec");
                    Console.WriteLine();

                    if (isFatHead)
                    {
                        chrono.Restart();
                        var dialogResult = DialogInterpreter.AnalyzeInitialQuestion(dialog, String.Empty, question, intentResult.Label1);
                        chrono.Stop();

                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        var nodeExecution = dialogResult.ExecutionResult;
                        if (nodeExecution is FatHeadAnswerNodeExecution)
                        {
                            Console.WriteLine("@ J'ai trouvé une réponse exacte à cette question : ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("  " + ((FatHeadAnswerNodeExecution)nodeExecution).MappingURI);
                        }
                        else if (nodeExecution.DialogNode is DisambiguationQuestion)
                        {
                            var disambiguation = ((DisambiguationQuestion)nodeExecution.DialogNode);
                            Console.WriteLine("@ Votre question est ambigüe, je dois vous demander en complément : ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("  " + disambiguation.QuestionText);
                            if (disambiguation.DisambiguationOptions != null)
                            {
                                foreach (var option in disambiguation.DisambiguationOptions)
                                {
                                    Console.WriteLine("  - " + option.Text);
                                }
                            }
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("@ Merci de ne pas répondre pour l'instant - disambiguation NON supportée");
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
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("Dialog interpreted in " + (chrono.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L))) + " microsec");
                        Console.WriteLine();
                    }
                }
                
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("@ Aurevoir.");
            }           
        }
    }
}
