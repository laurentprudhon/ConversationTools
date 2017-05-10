using System.IO;
using System.Xml;

namespace dialogtool
{
    public static class ViewFile
    {
        public static void Write(Dialog dialog, string sourceFilePath)
        {

            ViewGenerator viewGenerator = new ViewGenerator(dialog);

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "   ";
            settings.OmitXmlDeclaration = true;

            using (var xw = XmlWriter.Create(sourceFilePath + ".html", settings))
            {
                xw.WriteStartElement("body");

                foreach (var intent in viewGenerator.Intents)
                {
                    xw.WriteElementString("h1", intent.Name);

                    if (intent.ViewNodes.Count > 0) WriteColorCode(xw);

                    xw.WriteStartElement("table");

                    xw.WriteAttributeString("border", "2");
                    xw.WriteAttributeString("width", "80%");
                    xw.WriteAttributeString("cellpadding", "5");
                    xw.WriteAttributeString("cellspacing", "5");

                    foreach (var condition in intent.ViewNodes)
                    {
                        ReadRoot(condition, xw);
                    }

                    xw.WriteEndElement(); // table
                }

                xw.WriteEndElement(); // body
            }

        }

        public static void WriteColorCode(XmlWriter xw)
        {
            xw.WriteStartElement("p");
            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "green");
            xw.WriteString("Person  |  ");

            xw.WriteEndElement(); // font

            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "red");
            xw.WriteString("Object  |  ");

            xw.WriteEndElement(); // font

            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "blue");
            xw.WriteString("Event  |  ");

            xw.WriteEndElement(); // font

            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "Magenta");
            xw.WriteString("Product  |  ");

            xw.WriteEndElement(); // font

            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "purple");
            xw.WriteString("Guarantee  |  ");

            xw.WriteEndElement(); // font

            xw.WriteStartElement("font");

            xw.WriteAttributeString("color", "olive");
            xw.WriteString("SubDomain");

            xw.WriteEndElement(); // font
            xw.WriteEndElement(); // p

        }

        public static void ReadRoot(ViewNode condition, XmlWriter xw)
        {
            xw.WriteStartElement("tr");
            ReadNode(condition, xw);
            xw.WriteEndElement();
        }

        public static void ReadNode(ViewNode condition, XmlWriter xw)
        {
            if (condition.DisplayValues[0].Value != "root" && condition.DisplayValues[0].Value != "")
            {
                xw.WriteStartElement("td");

                xw.WriteAttributeString("RowSpan", GetRawSpan(condition).ToString());

                //TODO : remonter les attributs au niveau ViewNode
                if (condition.DisplayValues[0].Attributes.Count > 0)
                {
                    xw.WriteAttributeString(condition.DisplayValues[0].Attributes[0].Name, condition.DisplayValues[0].Attributes[0].Value);
                }

                if (condition.DisplayValues.Count > 1)
                {
                    foreach (var value in condition.DisplayValues)
                    {                     
                        xw.WriteStartElement("font");
                        if (value.Attributes.Count > 0)
                        {
                            foreach (var attribute in value.Attributes)
                            {
                                xw.WriteAttributeString(attribute.Name, attribute.Value);
                            }
                        }

                        xw.WriteAttributeString("color", GetColor(value));

                        xw.WriteString(value.Value + " | ");

                        xw.WriteEndElement(); //font
                    }
                }
                else
                {
                    xw.WriteStartElement("font");

                    xw.WriteAttributeString("color", GetColor(condition.DisplayValues[0]));

                    xw.WriteString(condition.DisplayValues[0].Value);

                    xw.WriteEndElement(); //font

                }

               
                xw.WriteEndElement(); //td
            }

            if (condition.Children != null && condition.Children.Count > 0)
            {
                foreach (var child in condition.Children)
                {
                    ReadNode(child, xw);
                }
            }

            if (condition.Children != null || condition.Children.Count > 0)
            {
                xw.WriteEndElement();
                xw.WriteStartElement("tr");

            }

        }

        //Get the number of leaves a node has
        public static int GetRawSpan(ViewNode condition)
        {

            int rowspan = ( GetNextChild(condition, 0) >=1) ? GetNextChild(condition, 0) : 1;

            return rowspan;

        }

        public static int GetNextChild(ViewNode condition, int rowspan)
        {

            rowspan = condition.Children.Count;

            if (condition.Children != null && condition.Children.Count > 0)
            {
                foreach (var child in condition.Children)
                {                  
                    rowspan += GetNextChild(child, rowspan);
                }
            }

            return rowspan;
        }

        //Colorize text with reference to the variable
        public static string GetColor(DisplayValue displayValue)
        {
            string color;

            switch (displayValue.Variable)
            {
                case "Object_Var":
                    color = "red";
                    break;
                case "Event_Var":
                    color = "blue";
                    break;
                case "Person_Var":
                    color = "green";
                    break;
                case "Product_Var":
                    color = "Magenta";
                    break;
                case "Guarantee_Var":
                    color = "purple";
                    break;
                case "SubDomain_Var":
                    color = "olive";
                    break;
                default:
                    color = "black";
                    break;
            }

            return color;

        }

    }
}
