using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace dialogtool
{
    public static class SourceFile
    {
        public static Dialog Read(FileInfo sourceFileInfo)
        {
            return null;
        }

        public static void Write(Dialog dialog, string sourceFilePath)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "   ";
            settings.OmitXmlDeclaration = true;
            using (var xw = XmlWriter.Create(sourceFilePath, settings))
            {
                xw.WriteStartElement("Dialog");

                // Dialog file properties
                xw.WriteElementString("FilePath", dialog.FilePath);
                xw.WriteElementString("MappingUriConfig", dialog.MappingUriConfig.ToString());
                xw.WriteElementString("StartOfDialogNodeId", dialog.StartOfDialogNodeId);
                xw.WriteStartElement("FatHeadAnswerNodeIds");
                foreach (var nodeId in dialog.FatHeadAnswerNodeIds)
                {
                    xw.WriteElementString("string", nodeId);
                }
                xw.WriteEndElement();
                xw.WriteElementString("LongTailAnswerNodeId", dialog.LongTailAnswerNodeId);

                xw.WriteStartElement("Intents");
                foreach (var intent in dialog.Intents.Values.OrderBy(i => i.Name))
                {
                    WriteIntent(xw, intent);
                }
                xw.WriteEndElement();

                xw.WriteStartElement("Entities");
                foreach (var entity in dialog.Entities.Values.OrderBy(e => e.Name))
                {
                    WriteEntity(xw, entity);
                }
                xw.WriteEndElement();

                xw.WriteStartElement("Concepts");
                foreach (var concept in dialog.Concepts.Values.OrderBy(c => c.CanonicalValue))
                {
                    WriteConcept(xw, concept);
                }
                xw.WriteEndElement();

                xw.WriteStartElement("Constants");
                foreach (var constant in dialog.Constants.Values.OrderBy(c => c.Name))
                {
                    WriteConstant(xw, constant);
                }
                xw.WriteEndElement();

                xw.WriteStartElement("Variables");
                foreach (var variable in dialog.Variables.Values.OrderBy(v => v.Name))
                {
                    WriteVariable(xw, variable);
                }
                xw.WriteEndElement();

                xw.WriteEndElement();
            }
        }

        private static void WriteIntent(XmlWriter xw, MatchIntentAndEntities intent)
        {
            xw.WriteStartElement("Intent");
            xw.WriteAttributeString("Name", intent.Name);
            if (!String.IsNullOrEmpty(intent.Folder))
            {
                xw.WriteAttributeString("Folder", intent.Folder);
            }
            if (intent.Questions.Count > 0)
            {
                xw.WriteStartElement("Questions");
                foreach (string question in intent.Questions)
                {
                    xw.WriteElementString("string", question);
                }
                xw.WriteEndElement();
            }
            if (intent.EntityMatches.Count > 0)
            {
                xw.WriteStartElement("EntityValues");
                foreach (var entityMatch in intent.EntityMatches)
                {
                    WriteEntityMatch(xw, entityMatch);
                }
                xw.WriteEndElement();
            }
            WriteChildrenNodes(xw, intent);
            xw.WriteEndElement();
        }

        private static void WriteEntityMatch(XmlWriter xw, EntityMatch entityMatch)
        {
            xw.WriteStartElement("Match");
            xw.WriteAttributeString("Entity", entityMatch.EntityName);
            xw.WriteAttributeString("Variable1", entityMatch.EntityVariableName1);
            if (entityMatch.EntityVariableName2 != null)
            {
                xw.WriteAttributeString("Variable2", entityMatch.EntityVariableName2);
            }
            xw.WriteEndElement();
        }

        private static void WriteChildrenNodes(XmlWriter xw, DialogNode dialogNode)
        {
            if (dialogNode.ChildrenNodes != null && dialogNode.ChildrenNodes.Count > 0)
            {
                foreach (var childNode in dialogNode.ChildrenNodes)
                {
                    WriteDialogNode(xw, childNode);
                }
            }
        }

        private static void WriteDialogNode(XmlWriter xw, DialogNode childNode)
        {
            switch (childNode.Type)
            {
                case DialogNodeType.DialogVariableConditions:
                    WriteDialogVariableConditions(xw, (DialogVariableConditions)childNode);
                    break;
                case DialogNodeType.SwitchOnEntityVariables:
                    WriteSwitchOnEntityVariables(xw, (SwitchOnEntityVariables)childNode);
                    break;
                case DialogNodeType.DirectAnswer:
                    WriteDirectAnswer(xw, (DirectAnswer)childNode);
                    break;
                case DialogNodeType.DisambiguationQuestion:
                    WriteDisambiguationQuestion(xw, (DisambiguationQuestion)childNode);
                    break;
                case DialogNodeType.FatHeadAnswers:
                    WriteFatHeadAnswers(xw, (FatHeadAnswers)childNode);
                    break;
                case DialogNodeType.GotoNext:
                    WriteGotoNext(xw, (GotoNode)childNode);
                    break;
                case DialogNodeType.GotoNode:
                    WriteGotoNode(xw, (GotoNode)childNode);
                    break;
                case DialogNodeType.RedirectToLongTail:
                    WriteRedirectToLongTail(xw, (GotoNode)childNode);
                    break;
            }
        }

        private static void WriteDialogNodeProperties(XmlWriter xw, DialogNode dialogNode)
        {
            if(dialogNode.DialogNodeReferences != null && dialogNode.DialogNodeReferences.Where(r => r.Type != DialogNodeType.GotoNext).Count() > 0)
            {
                xw.WriteAttributeString("Id", dialogNode.Id);
            }
            if(dialogNode.VariableAssignments != null && dialogNode.VariableAssignments.Count > 0)
            {
                foreach(var variableAssignment in dialogNode.VariableAssignments)
                {
                    xw.WriteStartElement("Set");
                    xw.WriteAttributeString("Variable", variableAssignment.VariableName);
                    xw.WriteAttributeString("ToValue", variableAssignment.Value);
                    xw.WriteEndElement();
                }
            }
        }

        private static void WriteDialogVariableConditions(XmlWriter xw, DialogVariableConditions childNode)
        {
            xw.WriteStartElement("If");
            var op = childNode.Operator.ToString().ToLower();
            string expression = null;
            foreach (var cond in childNode.VariableConditions)
            {
                if(expression != null)
                {
                    expression += " " + op + " ";
                }
                if (cond.Comparison == ConditionComparison.HasValue)
                {
                    expression += cond.VariableName + " has value";
                }
                else
                {
                    expression += cond.VariableName + "='" + cond.Value + "'";
                }
            }
            xw.WriteAttributeString("Expr", expression);
            WriteDialogNodeProperties(xw, childNode);            
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteSwitchOnEntityVariables(XmlWriter xw, SwitchOnEntityVariables childNode)
        {
            xw.WriteStartElement("Switch");            
            xw.WriteAttributeString("On", childNode.EntityMatch.EntityVariableName1);
            xw.WriteAttributeString("ThenOn", childNode.EntityMatch.EntityVariableName2);
            WriteDialogNodeProperties(xw, childNode);
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteDirectAnswer(XmlWriter xw, DirectAnswer childNode)
        {
            xw.WriteStartElement("DirectAnswer");
            WriteDialogNodeProperties(xw, childNode);
            if (!String.IsNullOrEmpty(childNode.MessageExpression))
            {
                xw.WriteStartElement("Message");
                xw.WriteString(childNode.MessageExpression);
                xw.WriteEndElement();
            }
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteDisambiguationQuestion(XmlWriter xw, DisambiguationQuestion childNode)
        {
            xw.WriteStartElement("DisambiguationQuestion");
            WriteDialogNodeProperties(xw, childNode);
            xw.WriteElementString("Message", childNode.QuestionExpression);
            if (childNode.DisambiguationOptions != null && childNode.DisambiguationOptions.Count > 0)
            {
                xw.WriteStartElement("Options");
                foreach (var option in childNode.DisambiguationOptions)
                {
                    xw.WriteStartElement("Option");
                    xw.WriteAttributeString("Text", option.Text);
                    if (option.EntityValue != null)
                    {
                        xw.WriteAttributeString("Entity", option.EntityValue.Entity.Name);
                        xw.WriteAttributeString("EntityValue", option.EntityValue.Name);
                    }
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
            }
            WriteEntityMatch(xw, childNode.EntityMatch);                    
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteFatHeadAnswers(XmlWriter xw, FatHeadAnswers childNode)
        {           
            xw.WriteStartElement("FatHeadAnswer");
            WriteDialogNodeProperties(xw, childNode);
            if (!String.IsNullOrEmpty(childNode.MessageExpression))
            {
                xw.WriteStartElement("Message");
                xw.WriteString(childNode.MessageExpression);
                xw.WriteEndElement();
            }            
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
            if (childNode.MappingUris != null)
            {
                foreach (var uri in childNode.MappingUris)
                {
                    xw.WriteComment(uri);
                }
            }
        }

        private static void WriteGotoNext(XmlWriter xw, GotoNode childNode)
        {
            xw.WriteStartElement("GotoNext");
            WriteDialogNodeProperties(xw, childNode);
            if (!String.IsNullOrEmpty(childNode.MessageExpression))
            {
                xw.WriteStartElement("Message");
                xw.WriteString(childNode.MessageExpression);
                xw.WriteEndElement();
            }
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteGotoNode(XmlWriter xw, GotoNode childNode)
        {
            xw.WriteStartElement("Goto");
            xw.WriteAttributeString("Ref", childNode.TargetNodeId);
            WriteDialogNodeProperties(xw, childNode);
            if (!String.IsNullOrEmpty(childNode.MessageExpression))
            {
                xw.WriteStartElement("Message");
                xw.WriteString(childNode.MessageExpression);
                xw.WriteEndElement();
            }
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteRedirectToLongTail(XmlWriter xw, GotoNode childNode)
        {
            xw.WriteStartElement("RedirectToLongTail");
            WriteDialogNodeProperties(xw, childNode);
            if (!String.IsNullOrEmpty(childNode.MessageExpression))
            {
                xw.WriteStartElement("Message");
                xw.WriteString(childNode.MessageExpression);
                xw.WriteEndElement();
            }
            WriteChildrenNodes(xw, childNode);
            xw.WriteEndElement();
        }

        private static void WriteEntity(XmlWriter xw, Entity entity)
        {
            xw.WriteStartElement("Entity");
            xw.WriteAttributeString("Name", entity.Name);
            xw.WriteStartElement("Values");
            foreach(var entityValue in entity.Values.OrderBy(v => v.Name))
            {
                if(entityValue.DialogNodeReferences != null && entityValue.DialogNodeReferences.Count > 0)
                {
                    xw.WriteStartElement("EntityValue");
                    xw.WriteAttributeString("Name", entityValue.Name);
                    if(entityValue.Concept != null)
                    {
                        xw.WriteAttributeString("ConceptId", entityValue.Concept.Id);
                    }
                    xw.WriteString(entityValue.CanonicalValue);
                    xw.WriteEndElement();
                }
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private static void WriteConcept(XmlWriter xw, Concept concept)
        {            
            xw.WriteStartElement("Concept");
            xw.WriteAttributeString("CanonicalValue", concept.CanonicalValue);
            if (!String.IsNullOrEmpty(concept.Id))
            {
                xw.WriteAttributeString("Id", concept.Id);
            }
            xw.WriteStartElement("Synonyms");
            foreach (var synonym in concept.Synonyms)
            {
                xw.WriteElementString("string", synonym);
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        private static void WriteConstant(XmlWriter xw, Constant constant)
        {
            if(constant.DialogNodeReferences != null && constant.DialogNodeReferences.Count > 0)
            {
                xw.WriteStartElement("Constant");
                xw.WriteAttributeString("Name", constant.Name);
                xw.WriteString(constant.Value);
                xw.WriteEndElement();
            }
        }

        private static void WriteVariable(XmlWriter xw, DialogVariable variable)
        {
            if(variable.DialogNodeReferences != null && variable.DialogNodeReferences.Count > 0)
            {
                xw.WriteStartElement("Variable");
                xw.WriteAttributeString("Name", variable.Name);
                xw.WriteAttributeString("Type", variable.Type.ToString());
                if (!String.IsNullOrEmpty(variable.InitValue))
                {
                    xw.WriteAttributeString("InitValue", variable.InitValue);
                }
                xw.WriteEndElement();
            }
        }
    }
}
