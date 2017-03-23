using System;
using System.Collections.Generic;
using System.Linq;

namespace dialogtool
{
    public class DialogVariablesSimulator
    {
        public DialogVariablesSimulator(IDictionary<string, DialogVariable> variables)
        {
            Variables = variables;
            VariablesValues = new Dictionary<DialogVariable, IList<string>>();
        }

        public IDictionary<string, DialogVariable> Variables { get; private set; }

        public IDictionary<DialogVariable, IList<string>> VariablesValues { get; private set; }

        public IList<EntityMatch> LastEntityMatches { get; private set; }

        public bool AddDialogVariableConditions(DialogVariableConditions variableConditions)
        {
            bool variablesChanged = false;
            var conditionsGroupsByVariable = variableConditions.VariableConditions.Where(condition => condition.Comparison == ConditionComparison.Equals).GroupBy(condition => condition.Variable);
            foreach (var conditionGroup in conditionsGroupsByVariable)
            {
                var variable = conditionGroup.Key;
                IList<string> previousValues = null;
                if (VariablesValues.ContainsKey(variable))
                {
                    previousValues = VariablesValues[variable];
                }
                IList<string> newValues = null;
                if (conditionGroup.Count() > 1 && variableConditions.Operator == ConditionOperator.Or)
                {
                    newValues = conditionGroup.Select(condition => condition.Value).ToList();
                }
                else
                {
                    newValues = new List<string>(1);
                    newValues.Add(conditionGroup.First().Value);
                }
                VariablesValues[variable] = newValues;
                if (previousValues == null ||
                    previousValues.Count != newValues.Count ||
                    previousValues.Intersect(newValues).Count() < previousValues.Count)
                {
                    variablesChanged = true;
                }
            }
            return variablesChanged;
        }

        public bool AddDialogVariableAssignment(DialogVariableAssignment variableAssignment)
        {
            switch (variableAssignment.Operator)
            {
                case DialogVariableOperator.SetTo:
                case DialogVariableOperator.SetToYes:
                case DialogVariableOperator.SetToNo:
                    if (VariablesValues.ContainsKey(variableAssignment.Variable) &&
                        VariablesValues[variableAssignment.Variable].Count == 1 &&
                        VariablesValues[variableAssignment.Variable][0] == variableAssignment.Value)
                    {
                        return false;
                    }
                    else
                    {
                        var values = new List<string>(1);
                        values.Add(variableAssignment.Value);
                        VariablesValues[variableAssignment.Variable] = values;
                        return true;
                    }
                case DialogVariableOperator.SetToBlank:
                    if (VariablesValues.ContainsKey(variableAssignment.Variable))
                    {
                        VariablesValues.Remove(variableAssignment.Variable);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    throw new Exception("Unexpected variable assignment operator " + variableAssignment.Operator);
            }
        }

        public void AddMatchIntentAndEntities(MatchIntentAndEntities intent)
        {
            var intentVariable = Variables["CLASSIFIER_CLASS_0"];
            var variableAssignment = new DialogVariableAssignment(intentVariable, DialogVariableOperator.SetTo, intent.Name);
            AddDialogVariableAssignment(variableAssignment);

            LastEntityMatches = intent.EntityMatches;
        }

        public void AddDisambiguationQuestion(DisambiguationQuestion question)
        {
            LastEntityMatches = new List<EntityMatch>(1);
            LastEntityMatches.Add(question.EntityMatch);
        }

        public string TryGetVariableValue(string variableName)
        {
            if (Variables.ContainsKey(variableName) && VariablesValues.ContainsKey(Variables[variableName]))
            {
                var candidateValues = VariablesValues[Variables[variableName]];
                if (candidateValues.Count == 1)
                {
                    return candidateValues[0];
                }
                else
                {
                    throw new Exception("Several candidate values for dialog variable " + variableName);
                }
            }
            else
            {
                return null;
            }
        }

        public IList<string> TryGetVariableValues(string variableName)
        {
            if (Variables.ContainsKey(variableName) && VariablesValues.ContainsKey(Variables[variableName]))
            {
                return VariablesValues[Variables[variableName]];
            }
            else
            {
                return null;
            }
        }

        public bool SetVariableValue(string variableName, string value)
        {
            return AddDialogVariableAssignment(
                new DialogVariableAssignment(Variables[variableName], DialogVariableOperator.SetTo, value));
        }

        public Entity TryGetEntityFromVariable(DialogVariable variable)
        {
            if (LastEntityMatches != null)
            {
                foreach (var entityMatch in LastEntityMatches)
                {
                    if (entityMatch != null && (entityMatch.EntityVariable1 == variable || entityMatch.EntityVariable2 == variable))
                    {
                        return entityMatch.Entity;
                    }
                }
            }
            return null;
        }

        public DialogVariablesSimulator Clone()
        {
            var nestedVariableSimulator = new DialogVariablesSimulator(Variables);
            foreach (var variable in VariablesValues.Keys)
            {
                nestedVariableSimulator.VariablesValues[variable] = VariablesValues[variable];
            }
            nestedVariableSimulator.LastEntityMatches = LastEntityMatches;
            return nestedVariableSimulator;
        }
    }
}
