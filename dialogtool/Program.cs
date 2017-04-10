using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dialogtool
{
    class Program
    {
        public enum DialogToolCommands
        {
            gensource,
            gendialog,
            check,
            view,
            answers,
            debug,
            compare,
            internaltest
        }

        private static void DisplayDialogToolSyntax()
        {
            Console.WriteLine("Watson Dialog tool syntax :");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool gensource dialog\savings_0703.xml");
            Console.WriteLine(@">  generates compact code from dialog file => source\savings_0703.code.xml");
            Console.WriteLine(@">  (and a template => source\savings_0703.template.xml)");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool gendialog  source\savings_0703.code.xml");
            Console.WriteLine(@">  generates dialog file from compact code => dialog\savings_0703.xml");
            Console.WriteLine(@">  (using a template => source\savings_0703.template.xml)");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool check dialog\savings_0703.xml");
            Console.WriteLine(@"dialogtool check source\savings_0703.code.xml");
            Console.WriteLine(@">  checks source or dialog file consistency => result\savings_0703.errors.csv");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool view dialog\savings_0703.xml");
            Console.WriteLine(@"dialogtool view source\savings_0703.code.xml");
            Console.WriteLine(@">  generates HTML view of dialog => result\savings_0703.view.html");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool answers dialog\savings_0703.xml");
            Console.WriteLine(@"dialogtool answers source\savings_0703.code.xml");
            Console.WriteLine(@">  extracts answers mapping URIs from dialog => result\savings_0703.answers.csv");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool debug dialog\savings_0703.xml input\questions1.csv");
            Console.WriteLine(@"dialogtool debug source\savings_0703.code.xml input\questions1.csv");
            Console.WriteLine(@">  explains dialog behavior => result\savings_0703.questions1.debug.html");
            Console.WriteLine(@">  for a table of [questions | intents] in csv file (input\questions1.csv)");
            Console.WriteLine();
            Console.WriteLine(@"dialogtool compare source\savings_0703-v2.code.xml source\savings_0703-v1.code.xml input\questions.csv");
            Console.WriteLine(@">  compare answers and dialog behavior for 2 versions of code file");
            Console.WriteLine(@">  on a table of [questions | intents] in csv file (input\questions.csv)");
            Console.WriteLine(@">  => savings_0703-v2.savings_0703-v1.questions.compare.csv");
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    DisplayDialogToolSyntax();
                    return;
                }

                var command = args[0].ToLower();
                var dialogCommand = DialogToolCommands.view;
                switch (command)
                {
                    case "gensource":
                        dialogCommand = DialogToolCommands.gensource;
                        break;
                    case "gendialog":
                        dialogCommand = DialogToolCommands.gendialog;
                        break;
                    case "check":
                        dialogCommand = DialogToolCommands.check;
                        break;
                    case "view":
                        dialogCommand = DialogToolCommands.view;
                        break;
                    case "answers":
                        dialogCommand = DialogToolCommands.answers;
                        break;
                    case "debug":
                        dialogCommand = DialogToolCommands.debug;
                        break;
                    case "compare":
                        dialogCommand = DialogToolCommands.compare;
                        break;
                    case "internaltest":
                        dialogCommand = DialogToolCommands.internaltest;
                        break;
                    default:
                        Console.WriteLine("ERROR : unknown dialogtool command \"" + command + "\"");
                        Console.WriteLine();
                        DisplayDialogToolSyntax();
                        return;
                }

                switch (dialogCommand)
                {
                    case DialogToolCommands.gensource:
                    case DialogToolCommands.gendialog:
                    case DialogToolCommands.check:
                    case DialogToolCommands.view:
                    case DialogToolCommands.answers:
                    case DialogToolCommands.internaltest:
                        if (args.Length != 2)
                        {
                            Console.WriteLine("ERROR : one parameter expected for dialogtool command \"" + command + "\"");
                            Console.WriteLine();
                            DisplayDialogToolSyntax();
                            return;
                        }
                        break;
                    case DialogToolCommands.debug:
                        if (args.Length != 3)
                        {
                            Console.WriteLine("ERROR : two parameters expected for dialogtool command \"" + command + "\"");
                            Console.WriteLine();
                            DisplayDialogToolSyntax();
                            return;
                        }
                        break;
                    case DialogToolCommands.compare:
                        if (args.Length != 4)
                        {
                            Console.WriteLine("ERROR : three parameters expected for dialogtool command \"" + command + "\"");
                            Console.WriteLine();
                            DisplayDialogToolSyntax();
                            return;
                        }
                        break;
                }

                CreateDialogToolDirectory("dialog");
                CreateDialogToolDirectory("input");
                CreateDialogToolDirectory("result");
                CreateDialogToolDirectory("source");

                var relativeFilePath1 = args[1];
                FileInfo fileInfo1 = null;
                if (!File.Exists(relativeFilePath1))
                {
                    Console.WriteLine("ERROR : file not found \"" + relativeFilePath1 + "\"");
                    return;
                }
                else
                {
                    fileInfo1 = new FileInfo(relativeFilePath1);
                }

                string relativeFilePath2 = null;
                FileInfo fileInfo2 = null;
                if (dialogCommand == DialogToolCommands.debug || dialogCommand == DialogToolCommands.compare)
                {
                    relativeFilePath2 = args[2];
                    if (!File.Exists(relativeFilePath2))
                    {
                        Console.WriteLine("ERROR : file not found \"" + relativeFilePath2 + "\"");
                        return;
                    }
                    else
                    {
                        fileInfo2 = new FileInfo(relativeFilePath2);
                    }
                }

                string relativeFilePath3 = null;
                FileInfo fileInfo3 = null;
                if (dialogCommand == DialogToolCommands.compare)
                {
                    relativeFilePath3 = args[3];
                    if (!File.Exists(relativeFilePath3))
                    {
                        Console.WriteLine("ERROR : file not found \"" + relativeFilePath3 + "\"");
                        return;
                    }
                    else
                    {
                        fileInfo3 = new FileInfo(relativeFilePath3);
                    }
                }

                switch (dialogCommand)
                {
                    case DialogToolCommands.gensource:
                        GenerateSourceFile(fileInfo1);
                        break;
                    case DialogToolCommands.gendialog:
                        GenerateDialogFile(fileInfo1);
                        break;
                    case DialogToolCommands.check:
                        CheckFileConsistency(fileInfo1);
                        break;
                    case DialogToolCommands.view:
                        ViewDialogBranches(fileInfo1);
                        break;
                    case DialogToolCommands.answers:
                        GenerateAnswersMappingURIs(fileInfo1);
                        break;
                    case DialogToolCommands.debug:
                        DebugDialogBehavior(fileInfo1, fileInfo2);
                        break;
                    case DialogToolCommands.compare:
                        CompareResultsAcrossDialogVersions(fileInfo1, fileInfo2, fileInfo3);
                        break;
                    case DialogToolCommands.internaltest:
                        InternalTest_DisplayMatchResults(fileInfo1);
                        break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine();
                Console.WriteLine("----------");
                Console.WriteLine("An unexpected ERROR occured, please report the technical details below :");
                ReportException(e);
                Console.WriteLine("----------");
            }
        }
               
        private static void ReportException(Exception e)
        {
            if(e.InnerException != null)
            {
                ReportException(e.InnerException);
            }
            Console.WriteLine();
            Console.WriteLine(">>> " + e.Message);
            Console.WriteLine();
            Console.WriteLine(e.StackTrace);
        }

        private static void CreateDialogToolDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                var dirInfo = Directory.CreateDirectory(directory);
                Console.WriteLine("dialogtool created work directory " + directory + " in " + dirInfo.Parent.FullName);
            }
        }

        private static void GenerateSourceFile(FileInfo dialogFileInfo)
        {
            Console.WriteLine("Generate source file :");
            Console.WriteLine();

            // Load dialog file
            string sourceOrDialogFileName;
            Dialog dialog = LoadDialogFile(dialogFileInfo, out sourceOrDialogFileName);

            // Write source file
            var sourceFilePath = @"source\" + sourceOrDialogFileName + ".code.xml";
            Console.Write("Writing " + sourceFilePath + " ... ");
            SourceFile.Write(dialog, sourceFilePath);

            Console.WriteLine("OK");
            Console.WriteLine("");
        }

        private static void GenerateDialogFile(FileInfo sourceFileInfo)
        {
            Console.WriteLine("dialogtool command \"gendialog\" not yet implemented");
        }

        private static void CheckFileConsistency(FileInfo sourceOrDialogFileInfo)
        {
            Console.WriteLine("Check dialog file consistency :");
            Console.WriteLine();

            // Load dialog file
            string sourceOrDialogFileName;
            Dialog dialog = LoadDialogFile(sourceOrDialogFileInfo, out sourceOrDialogFileName);
            
            Console.WriteLine("Dialog file metrics :");
            var nodeTypeCounts = dialog.ComputeNodesStatistics();
            foreach(var nodeType in nodeTypeCounts.Keys)
            {
                Console.WriteLine("- " + nodeTypeCounts[nodeType] + " " + nodeType.ToString() + " nodes");
            }
            Console.WriteLine("- " + dialog.Entities.Values.SelectMany(entity => entity.Values).Count() + " entity values");
            Console.WriteLine("- " + dialog.Concepts.Values.Distinct().Count() + " concepts");
            Console.WriteLine("- " + dialog.ConceptsSynonyms.Keys.Count() + " concepts synonyms");
            Console.WriteLine("");


            Console.WriteLine(dialog.Errors.Count + " inconsistencies found :");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.InvalidReference.ToString())).Count() + " invalid references");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.IncorrectPattern.ToString())).Count() + " incorrect patterns");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.DuplicateKey.ToString())).Count() + " duplicate keys");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.DuplicateConcept.ToString())).Count() + " duplicate concepts");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.DuplicateSynonym.ToString())).Count() + " duplicate synonyms");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.NeverUsed.ToString())).Count() + " elements never used");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.Info.ToString())).Count() + " infos");
            Console.WriteLine("");

            // Write list of inconsistencies
            var errorsFilePath = @"result\" + sourceOrDialogFileName + ".errors.csv";
            Console.Write("Writing " + errorsFilePath + " ... ");
            using (StreamWriter sw = new StreamWriter(errorsFilePath, false, Encoding.GetEncoding("iso8859-1")))
            {
                foreach (var error in dialog.Errors.OrderBy(error => error))
                {
                    sw.WriteLine(error);
                }
            }
            Console.WriteLine("OK");
            Console.WriteLine("");
        }

        private static Dialog LoadDialogFile(FileInfo sourceOrDialogFileInfo, out string sourceOrDialogFileName, bool isInternalTest = false)
        {
            sourceOrDialogFileName = sourceOrDialogFileInfo.Name;
            Console.Write("Reading " + sourceOrDialogFileName + " ... ");
            var dialogFile = new DialogFile(sourceOrDialogFileInfo);
            Dialog dialog = dialogFile.Read(isInternalTest);
            Console.WriteLine("OK");
            Console.WriteLine();
            return dialog;
        }

        private static void ViewDialogBranches(FileInfo sourceOrDialogFileInfo)
        {
            Console.WriteLine("dialogtool command \"view\" not yet implemented");
        }

        private static void GenerateAnswersMappingURIs(FileInfo sourceOrDialogFileInfo)
        {
            Console.WriteLine("Generate answers mapping URIs :");
            Console.WriteLine();

            // Load dialog file
            string sourceOrDialogFileName;
            Dialog dialog = LoadDialogFile(sourceOrDialogFileInfo, out sourceOrDialogFileName);

            // Write answers mapping URIs file
            var mappingFilePath = @"result\" + sourceOrDialogFileName + ".answers.csv";
            Console.Write("Writing " + mappingFilePath + " ... ");
            HashSet<string> mappingURISet = new HashSet<string>();            
            using (StreamWriter sw = new StreamWriter(mappingFilePath, false, Encoding.GetEncoding("iso8859-1")))
            {
                foreach(var intent in dialog.Intents.Values)
                {
                    WriteAnswers(sw, intent, mappingURISet);
                }
            }

            Console.WriteLine("OK");
            Console.WriteLine("");
            Console.WriteLine("=> generated " + mappingURISet.Count + " distinct mapping URIs");
            Console.WriteLine("");
        }

        private static void WriteAnswers(StreamWriter sw, DialogNode dialogNode, HashSet<string> mappingURISet)
        {
            if (dialogNode.Type == DialogNodeType.FatHeadAnswers)
            {
                var fatHeadAnswers = (FatHeadAnswers)dialogNode;
                if (fatHeadAnswers.MappingUris != null)
                {
                    foreach (var mappingUri in fatHeadAnswers.MappingUris)
                    {
                        if (!mappingURISet.Contains(mappingUri))
                        {
                            mappingURISet.Add(mappingUri);
                            sw.WriteLine(dialogNode.LineNumber + ";" + mappingUri);
                        }                        
                    }
                }
            }
            else if(dialogNode.ChildrenNodes != null)
            {
                foreach(var childNode in dialogNode.ChildrenNodes)
                {
                    WriteAnswers(sw, childNode, mappingURISet);
                }
            }
        }

        private static void DebugDialogBehavior(FileInfo sourceOrDialogFileInfo, FileInfo annotatedQuestionsFileInfo)
        {
            Console.WriteLine("dialogtool command \"debug\" not yet implemented");
        }

        private static void CompareResultsAcrossDialogVersions(FileInfo newSourceOrDialogFileInfo, FileInfo oldSourceOrDialogFileInfo, FileInfo annotatedQuestionsFileInfo)
        {
            Console.WriteLine("Compare results for two dialog files :");
            Console.WriteLine();

            // Load new dialog file
            string newSourceOrDialogFileName;
            Dialog newDialog = LoadDialogFile(newSourceOrDialogFileInfo, out newSourceOrDialogFileName);

            // Load old dialog file
            string oldSourceOrDialogFileName;
            Dialog oldDialog = LoadDialogFile(oldSourceOrDialogFileInfo, out oldSourceOrDialogFileName);

            // Load all questions
            Console.WriteLine("Reading " + annotatedQuestionsFileInfo.Name + " ... ");
            int questionCount = 0;
            IList<string> impacts = new List<string>();
            using (StreamReader sr = new StreamReader(annotatedQuestionsFileInfo.FullName, Encoding.GetEncoding("iso8859-1")))
            {
                string line = null;
                while((line = sr.ReadLine()) != null)
                {
                    string[] columns = line.Split(';');
                    string questionId = columns[0];
                    string questionText = columns[1];
                    string intentName = columns[2];

                    // Simulate new dialog execution 
                    var newResult = DialogInterpreter.AnalyzeInitialQuestion(newDialog, questionId, questionText, intentName);

                    // Simulate old dialog execution 
                    var oldResult = DialogInterpreter.AnalyzeInitialQuestion(oldDialog, questionId, questionText, intentName);

                    // Compare results
                    if(!newResult.ReturnsSameResultAs(oldResult))
                    {
                        impacts.Add(newResult.QuestionId + ";" + newResult.QuestionText + ";" + newResult.ToString() + ";" + oldResult.ToString());
                    }

                    questionCount++;
                    if(questionCount % 500 == 0)
                    {
                        Console.WriteLine(questionCount + " test set questions analyzed");
                    }
                }

                // TO DO : do the same thing with all DisambiguationQuestion nodes found in the tree
            }
            Console.WriteLine("OK");

            Console.WriteLine("Analysis completed :");
            Console.WriteLine("- impacts found on " + impacts.Count + " questions of the test set");
            Console.WriteLine("");

            // Write comparison results file
            var comparisonFilePath = @"result\" + newSourceOrDialogFileName + "." + oldSourceOrDialogFileName + "." + annotatedQuestionsFileInfo.Name + ".compare.csv";
            Console.Write("Writing " + comparisonFilePath + " ... ");
            HashSet<string> mappingURISet = new HashSet<string>();
            using (StreamWriter sw = new StreamWriter(comparisonFilePath, false, Encoding.GetEncoding("iso8859-1")))
            {
                foreach(var impact in impacts)
                {
                    sw.WriteLine(impact);
                }
            }
            Console.WriteLine("OK");
            Console.WriteLine("");
        }

        private static void InternalTest_DisplayMatchResults(FileInfo sourceOrDialogFileInfo)
        {           
            Console.WriteLine("Internal test - Check entity values matches :");
            Console.WriteLine();

            // Load dialog file
            string sourceOrDialogFileName;
            Dialog dialog = LoadDialogFile(sourceOrDialogFileInfo, out sourceOrDialogFileName, true);

            Console.WriteLine(dialog.Errors.Count + " inconsistencies found :");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.DuplicateKey.ToString())).Count() + " duplicate keys");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.IncorrectPattern.ToString())).Count() + " incorrect patterns");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.InvalidReference.ToString())).Count() + " invalid references");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.NeverUsed.ToString())).Count() + " elements never used");
            Console.WriteLine("- " + dialog.Errors.Where(error => error.Contains(MessageType.Info.ToString())).Count() + " infos");
            Console.WriteLine("");
            
            // Write list of inconsistencies
            var errorsFilePath = @"result\" + sourceOrDialogFileName + ".errors.csv";
            Console.Write("Writing " + errorsFilePath + " ... ");
            using (StreamWriter sw = new StreamWriter(errorsFilePath, false, Encoding.GetEncoding("iso8859-1")))
            {
                foreach (var error in dialog.Errors.OrderBy(error => error))
                {
                    sw.WriteLine(error);
                }
            }
            Console.WriteLine("OK");
            Console.WriteLine("");

            // Write list of entity values matches
            var testsIntentName = "TESTS";
            var simulationFilePath = @"result\" + sourceOrDialogFileName + ".internaltest.csv";
            Console.Write("Writing " + simulationFilePath + " ... ");
            using (StreamWriter sw = new StreamWriter(simulationFilePath, false, Encoding.GetEncoding("iso8859-1")))
            {
                sw.WriteLine("#num;question;Test_Var;Test_Var_2;Test2_Var;Test2_Var_2");

                var testIntent = dialog.Intents[testsIntentName];
                int i = 0;
                foreach (var question in testIntent.Questions)
                {
                    i++;
                    var result = DialogInterpreter.AnalyzeInitialQuestion(dialog, i.ToString(), question, testsIntentName);
                    string var11 = null;
                    result.VariablesValues.TryGetValue("Test_Var", out var11);
                    string var12 = null;
                    result.VariablesValues.TryGetValue("Test_Var_2", out var12);
                    string var21 = null;
                    result.VariablesValues.TryGetValue("Test2_Var", out var21);
                    string var22 = null;
                    result.VariablesValues.TryGetValue("Test2_Var_2", out var22);

                    sw.WriteLine(i.ToString() + ";" + question + ";" + var11 + ";" + var12 + ";" + var21 + ";" + var22);
                    //sw.WriteLine(i.ToString() + ";" + question + ";" + "{{\"TestMatch1\": \"{0}\", \"TestMatch2\": \"{1}\", \"Test2Match1\": \"{2}\", \"Test2Match2\": \"{3}\"}}", var11, var12, var21, var22);
                }
            }
            Console.WriteLine("OK");
            Console.WriteLine("");
        }
    }
}
