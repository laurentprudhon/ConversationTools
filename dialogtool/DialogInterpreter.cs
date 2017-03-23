using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dialogtool
{
    public static class DialogInterpreter
    {
        public static DialogExecutionResult AnalyzeQuestion(Dialog dialog, int questionId, string questionText, string intentName)
        {
            var result = new DialogExecutionResult(questionId, questionText, intentName);

            MatchIntentAndEntities intent = null;
            if(dialog.Intents.TryGetValue(intentName, out intent))
            {
                var entities = intent.EntityMatches.Select(entityMatch => entityMatch.Entity);
                EntityValuesMatchResult matchResult = EntityValuesMatcher.MatchEntityValues(entities, questionText, dialog.ConceptsSynonyms, dialog.ConceptsRegex);
                IDictionary<string, string> variablesValues = new Dictionary<string, string>();
                foreach (var entityValue in matchResult.EntityValues)
                {
                    // ...
                }
            }
            else
            {
                result.LogMessage("Intent name " + intentName + " undefined in dialog file " + dialog.FilePath);
            }

            return result;
        }
    }

    public class DialogExecutionResult
    {
        public DialogExecutionResult(int questionId, string questionText, string intentName)
        {
            QuestionId = questionId;
            QuestionText = questionText;
            IntentName = intentName;
        }

        public int QuestionId { get; private set; }
        public string QuestionText { get; private set; }
        public string IntentName { get; private set; }

        public IList<string> Messages { get; private set; }

        public void LogMessage(string message)
        {
            if (Messages == null) Messages = new List<string>();
            Messages.Add(message);
        }

        internal bool IsIdenticalTo(DialogExecutionResult oldResult)
        {
            throw new NotImplementedException();
        }
    }
}
