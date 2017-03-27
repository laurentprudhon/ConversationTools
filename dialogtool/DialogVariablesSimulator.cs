using System;
using System.Collections.Generic;
using System.Linq;

namespace dialogtool
{
    public class DialogVariablesSimulator
    {
        public DialogVariablesSimulator(IDictionary<string, DialogVariable> variables, IEnumerable<string> entityVariablesNames)
        {
            Variables = variables;
            VariablesValues = new Dictionary<DialogVariable, IList<string>>();
            this.entityVariablesNames = entityVariablesNames;
        }

        public IDictionary<string, DialogVariable> Variables { get; private set; }

        public IDictionary<DialogVariable, IList<string>> VariablesValues { get; private set; }

        private IEnumerable<string> entityVariablesNames { get; set; }

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

        public bool AddDialogVariableAssignment(DialogVariableAssignment variableAssignment, DialogNodeType nodeType)
        {
            switch (variableAssignment.Operator)
            {
                case DialogVariableOperator.SetTo:
                case DialogVariableOperator.SetToYes:
                case DialogVariableOperator.SetToNo:      
                    // Filter out all cases when variable assignment is not useful to simplify the code
                    if (VariablesValues.ContainsKey(variableAssignment.Variable) &&
                        VariablesValues[variableAssignment.Variable].Count == 1)
                    {
                        var previousValue = VariablesValues[variableAssignment.Variable][0];
                        // Case 1 : value not changed => variable assignment is not useful
                        if (previousValue == variableAssignment.Value)
                        {
                            return false;
                        }                       
                    }
                    var values = new List<string>(1);
                    values.Add(variableAssignment.Value);
                    VariablesValues[variableAssignment.Variable] = values;
                    return true;
                case DialogVariableOperator.SetToBlank:
                    if (VariablesValues.ContainsKey(variableAssignment.Variable))
                    {
                        if (VariablesValues[variableAssignment.Variable].Count == 1 &&
                            entityVariablesNames.Contains(variableAssignment.VariableName))
                        {
                            var previousValue = VariablesValues[variableAssignment.Variable][0];
                            // Case 2 : the default behavior of FatHeadAnswer
                            // is to reset all entity variables not explicitly set => variable assignment is not useful
                            if (nodeType == DialogNodeType.FatHeadAnswers && (String.IsNullOrEmpty(previousValue) || previousValue.StartsWith("$(")))
                            {
                                return false;
                            }
                            // Case 3 : the default behavior of RedirectToLongTail / DirectAnswer
                            // is to reset all entity variables n=> variable assignment is not useful
                            else if ((nodeType == DialogNodeType.RedirectToLongTail || nodeType == DialogNodeType.DirectAnswer))
                            {
                                return false;
                            }
                        }

                        VariablesValues.Remove(variableAssignment.Variable);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case DialogVariableOperator.CopyValueFromVariable:
                    var fromVariable = Variables[variableAssignment.Value];
                    IList<string> fromValues = null;
                    VariablesValues.TryGetValue(fromVariable, out fromValues);
                    IList<string> toValues = null;
                    VariablesValues.TryGetValue(variableAssignment.Variable, out toValues);
                    if(fromValues == null)
                    {
                        if (toValues != null) VariablesValues.Remove(variableAssignment.Variable);
                    }
                    else
                    {
                        VariablesValues[variableAssignment.Variable] = fromValues;
                    }
                    return true;
                default:
                    throw new Exception("Unexpected variable assignment operator " + variableAssignment.Operator);
            }
        }

        public void AddMatchIntentAndEntities(MatchIntentAndEntities intent)
        {
            var intentVariable = Variables["CLASSIFIER_CLASS_0"];
            var variableAssignment = new DialogVariableAssignment(intentVariable, DialogVariableOperator.SetTo, intent.Name);
            AddDialogVariableAssignment(variableAssignment, intent.Type);
            AddEntityMatches(intent.EntityMatches);
        }
        
        public void AddDisambiguationQuestion(DisambiguationQuestion question)
        {
            if (question.EntityMatch != null)
            {
                var entityMatches = new List<EntityMatch>(1);
                entityMatches.Add(question.EntityMatch);
                AddEntityMatches(entityMatches);
            }
        }

        private void AddEntityMatches(IList<EntityMatch> entityMatches)
        {
            LastEntityMatches = entityMatches;
            foreach(var entityMatch in LastEntityMatches)
            {                
                if (entityMatch.EntityVariable1 != null)
                {
                    var values = new List<string>(1);
                    values.Add("$(" + entityMatch.Entity.Name + ")[1]");
                    VariablesValues[entityMatch.EntityVariable1] = values;
                }
                if (entityMatch.EntityVariable2 != null)
                {
                    var values = new List<string>(1);
                    values.Add("$(" + entityMatch.Entity.Name + ")[2]");
                    VariablesValues[entityMatch.EntityVariable2] = values;
                }
            }
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

        public bool SetVariableValue(string variableName, string value, DialogNodeType nodeType)
        {
            return AddDialogVariableAssignment(
                new DialogVariableAssignment(Variables[variableName], DialogVariableOperator.SetTo, value), nodeType);
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
            var nestedVariableSimulator = new DialogVariablesSimulator(Variables, entityVariablesNames);
            foreach (var variable in VariablesValues.Keys)
            {
                nestedVariableSimulator.VariablesValues[variable] = VariablesValues[variable];
            }
            nestedVariableSimulator.LastEntityMatches = LastEntityMatches;
            return nestedVariableSimulator;
        }

        internal DialogVariable[] ResetEntityVariablesNotExplicitlySet(IEnumerable<string> entityVariableNames)
        {
            IList<DialogVariable> result = new List<DialogVariable>();
            foreach (var entityVariableName in entityVariableNames)
            {
                DialogVariable entityVariable = null;
                Variables.TryGetValue(entityVariableName, out entityVariable);
                if (entityVariable != null)
                {
                    if (!VariablesValues.ContainsKey(entityVariable) || 
                        VariablesValues[entityVariable].Count == 0)
                    {
                        result.Add(entityVariable);
                    }
                    else if(VariablesValues[entityVariable].Count == 1)
                    {
                        var value = VariablesValues[entityVariable][0];
                        if(String.IsNullOrEmpty(value)) 
                        {
                            result.Add(entityVariable);
                        }
                        else if(value.StartsWith("$("))
                        {
                            VariablesValues.Remove(entityVariable);
                            result.Add(entityVariable);
                        }
                    }
                }
            }
            return result.ToArray();
        }
    }
}
