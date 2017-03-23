using System;
using System.Collections.Generic;

namespace dialogtool
{
    public class DialogVariable
    {
        public DialogVariable(string name, DialogVariableType type, string initValue)
        {
            Name = name;
            Type = type;
            InitValue = initValue;
        }

        public string Name { get; private set; }
        public DialogVariableType Type { get; private set; }
        public string InitValue { get; private set; }

        public IList<DialogNodeVariableReference> DialogNodeReferences { get; private set; }
        internal void AddDialogNodeReference(DialogNode dialogNode, VariableReferenceType referenceType)
        {
            if (DialogNodeReferences == null) DialogNodeReferences = new List<DialogNodeVariableReference>();
            DialogNodeReferences.Add(new DialogNodeVariableReference() { Node = dialogNode, ReferenceType = referenceType });
        }

        internal int LineNumber { get; set; }
    }

    public enum DialogVariableType
    {
        Text,
        Number,
        YesNo
    }

    public enum VariableReferenceType
    {
        Read,
        Write
    }

    public class DialogNodeVariableReference
    {
        public DialogNode Node { get; set; }
        public VariableReferenceType ReferenceType { get; set; }
    }

    public enum DialogVariableOperator
    {
        SetTo,
        SetToBlank,
        SetToYes,
        SetToNo
    }

    public class DialogVariableAssignment
    {
        public DialogVariableAssignment(string variableName, DialogVariableOperator @operator, string value)
        {
            VariableName = variableName;
            Operator = @operator;
            if (Operator == DialogVariableOperator.SetTo)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    Value = value;
                }
                else
                {
                    Operator = DialogVariableOperator.SetToBlank;
                }
            }
            else if (Operator == DialogVariableOperator.SetToYes)
            {
                Value = "yes";
            }
            else if (Operator == DialogVariableOperator.SetToNo)
            {
                Value = "no";
            }
        }

        public DialogVariableAssignment(DialogVariable variable, DialogVariableOperator @operator, string value) :
            this(variable.Name, @operator, value)
        {
            Variable = variable;
        }

        public string VariableName { get; private set;  }
        public DialogVariable Variable { get; internal set; }
        public DialogVariableOperator Operator { get; private set; }
        public string Value { get; private set; }
    }   
}
