using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace XmlComparer
{
    /// <summary>
    /// Static class to store methods which are required to format XML in a friendly way
    /// </summary>
    public static class AdvancedXmlComparer
    {
        const string START_SPAN = "!!!START!!!";
        const string END_SPAN = "!!!END!!!";

        private const string HTML_SPAN_START = "<span style='background-color: #b36b00; color: white'>";
        private const string HTML_END_SPAN = "</span>";

        /// <summary>
        /// Formats XML into HTML-friendly markup.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static string FormatXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return "-";
            }

            var result = new StringBuilder();
            var document = new XmlDocument();

            try
            {
                document.LoadXml(xml);

                // We will use stringWriter to push the formated xml into our StringBuilder bob.
                using (var stringWriter = new StringWriter(result))
                {
                    // We will use the Formatting of our xmlTextWriter to provide our indentation.
                    using (var xmlTextWriter = new XmlTextWriter(stringWriter))
                    {
                        xmlTextWriter.Formatting = Formatting.Indented;
                        xmlTextWriter.IndentChar = '\t';
                        document.WriteTo(xmlTextWriter);
                    }
                }

                return HttpUtility.HtmlEncode(result.ToString())
                    .Replace("\n",
                        "<br/>")
                    .Replace("\t",
                        "&nbsp;&nbsp;&nbsp;&nbsp;")
                    .Replace(START_SPAN,
                        HTML_SPAN_START)
                    .Replace(END_SPAN,
                        HTML_END_SPAN);
            }
            catch (Exception)
            {
                return xml;
            }
        }

        /// <summary>
        /// A method to check two provided XMLs for differences and format them with these differences being highlighted
        /// </summary>
        /// <returns></returns>
        public static XmlAdvanced FormatXml(string xmlLeft, string xmlRight)
        {
            //we don't check for differences if at least one of the provided XML is empty or null
            if (string.IsNullOrWhiteSpace(xmlLeft) || string.IsNullOrWhiteSpace(xmlRight))
            {
                var xmlLeftFormatted = FormatXml(xmlLeft);
                var xmlRightFormatted = FormatXml(xmlRight);
                var xmlAdvanced = new XmlAdvanced(xmlLeftFormatted, xmlRightFormatted);
                return xmlAdvanced;
            }

            var xmlLeftDocument = XDocument.Parse(xmlLeft);
            var xmlRightDocument = XDocument.Parse(xmlRight);

            IEnumerable<XElement> xmlLeftElements = xmlLeftDocument.Elements();
            IEnumerable<XElement> xmlRightElements = xmlRightDocument.Elements();

            int xmlLeftLength = xmlLeftElements.Count();
            int xmlRightLength = xmlRightElements.Count();

            int xmlMinElementsLength = xmlLeftLength < xmlRightLength ? xmlLeftLength : xmlRightLength;

            for (int i = 0; i < xmlMinElementsLength; ++i)
            {
                XElement xmlLeftCurrentElement = xmlLeftElements.ElementAt(i);
                XElement xmlRightCurrentElement = xmlRightElements.ElementAt(i);

                RecurseHighlight(xmlLeftCurrentElement, xmlRightCurrentElement);
            }

            if (xmlLeftLength > xmlRightLength)
            {
                for (int i = xmlMinElementsLength; i < xmlLeftLength; ++i)
                {
                    XElement xmlCurrentElement = xmlLeftElements.ElementAt(i);
                    RecurseHighlight(xmlCurrentElement);
                }
            }

            if (xmlRightLength > xmlLeftLength)
            {
                for (int i = xmlMinElementsLength; i < xmlRightLength; ++i)
                {
                    XElement xmlCurrentElement = xmlRightElements.ElementAt(i);
                    RecurseHighlight(xmlCurrentElement);
                }
            }

            string xmlLeftProcessed = FormatXml(string.Concat(xmlLeftElements.Select(element => element.ToString())));
            string xmlRightProcessed = FormatXml(string.Concat(xmlRightElements.Select(element => element.ToString())));
            return new XmlAdvanced(xmlLeftProcessed, xmlRightProcessed);
        }

        private static void RecurseHighlight(XElement xmlLeftCurrentElement, XElement xmlRightCurrentElement)
        {
            #region Work with current elements' attributes

            IEnumerable<XAttribute> xmlLeftAttributes = xmlLeftCurrentElement?.Attributes();
            IEnumerable<XAttribute> xmlRightAttributes = xmlRightCurrentElement?.Attributes();

            //check if both elements have attributes
            if (xmlLeftAttributes != null && xmlRightAttributes != null)
            {
                int xmlLeftAttrsLength = xmlLeftAttributes.Count();
                int xmlRightAttrsLength = xmlRightAttributes.Count();

                int xmlMinAttrsLength = xmlLeftAttrsLength < xmlRightAttrsLength ? xmlLeftAttrsLength : xmlRightAttrsLength;

                for (int j = 0; j < xmlMinAttrsLength; ++j)
                {
                    XAttribute xmlLeftCurrentAttr = xmlLeftAttributes.ElementAt(j);
                    XAttribute xmlRightCurrentAttr = xmlRightAttributes.ElementAt(j);

                    if (xmlLeftCurrentAttr.Name.LocalName != xmlRightCurrentAttr.Name.LocalName)
                    {
                        xmlLeftCurrentAttr.SetValue(START_SPAN + xmlLeftCurrentAttr.Value + END_SPAN);
                        xmlRightCurrentAttr.SetValue(START_SPAN + xmlRightCurrentAttr.Value + END_SPAN);
                    }
                    else
                    {
                        if (xmlLeftCurrentAttr.Value != xmlRightCurrentAttr.Value)
                        {
                            xmlLeftCurrentAttr.SetValue(START_SPAN + xmlLeftCurrentAttr.Value + END_SPAN);
                            xmlRightCurrentAttr.SetValue(START_SPAN + xmlRightCurrentAttr.Value + END_SPAN);
                        }
                    }
                }

                if (xmlLeftAttrsLength > xmlRightAttrsLength)
                {
                    for (int j = xmlMinAttrsLength; j < xmlLeftAttrsLength; ++j)
                    {
                        XAttribute xmlLeftCurrentAttr = xmlLeftAttributes.ElementAt(j);
                        xmlLeftCurrentAttr.SetValue(START_SPAN + xmlLeftCurrentAttr.Value + END_SPAN);
                    }
                }

                if (xmlRightAttrsLength > xmlLeftAttrsLength)
                {
                    for (int j = xmlMinAttrsLength; j < xmlRightAttrsLength; ++j)
                    {
                        XAttribute xmlRightCurrentAttr = xmlRightAttributes.ElementAt(j);
                        xmlRightCurrentAttr.SetValue(START_SPAN + xmlRightCurrentAttr.Value + END_SPAN);
                    }
                }
            }
            else if (xmlLeftAttributes != null && xmlRightAttributes == null
                || xmlLeftAttributes == null && xmlRightAttributes != null)
            {
                IEnumerable<XAttribute> xmlAttributes = xmlLeftAttributes ?? xmlRightAttributes;

                for (int i = 0; i < xmlAttributes.Count(); ++i)
                {
                    XAttribute xmlCurrentAttr = xmlAttributes.ElementAt(i);
                    xmlCurrentAttr.SetValue(START_SPAN + xmlCurrentAttr.Value + END_SPAN);
                }
            }

            #endregion

            #region Work with current elements' nodes

            IEnumerable<XElement> xmlLeftNodes = xmlLeftCurrentElement?.Nodes()?.Select(node => node as XElement);
            IEnumerable<XElement> xmlRightNodes = xmlRightCurrentElement?.Nodes()?.Select(node => node as XElement);

            //check if both elements have children
            if (xmlLeftNodes != null && xmlRightNodes != null)
            {
                int xmlLeftNodesLength = xmlLeftNodes.Count();
                int xmlRightNodesLength = xmlRightNodes.Count();

                int xmlMinNodesLength = xmlLeftNodesLength < xmlRightNodesLength ? xmlLeftNodesLength : xmlRightNodesLength;

                for (int k = 0; k < xmlMinNodesLength; ++k)
                {
                    XElement xmlLeftCurrentNode = xmlLeftNodes.ElementAt(k);
                    XElement xmlRightCurrentNode = xmlRightNodes.ElementAt(k);

                    RecurseHighlight(xmlLeftCurrentNode, xmlRightCurrentNode);
                }

                if (xmlLeftNodesLength > xmlRightNodesLength)
                {
                    for (int k = xmlMinNodesLength; k < xmlLeftNodesLength; ++k)
                    {
                        XElement xmlLeftCurrentNode = xmlLeftNodes.ElementAt(k);

                        RecurseHighlight(xmlLeftCurrentNode);
                    }
                }

                if (xmlRightNodesLength > xmlLeftNodesLength)
                {
                    for (int k = xmlMinNodesLength; k < xmlRightNodesLength; ++k)
                    {
                        XElement xmlRightCurrentNode = xmlRightNodes.ElementAt(k);

                        RecurseHighlight(xmlRightCurrentNode);
                    }
                }
            }
            // if only one element has children
            else if (xmlLeftNodes != null && xmlRightNodes == null
                || xmlLeftNodes == null && xmlRightNodes != null)
            {
                IEnumerable<XElement> xmlElements = xmlLeftNodes ?? xmlRightNodes;

                for (int i = 0; i < xmlElements.Count(); ++i)
                {
                    XElement xmlElement = xmlElements.ElementAt(i);

                    RecurseHighlight(xmlElement);
                }
            }

            #endregion
        }

        private static void RecurseHighlight(XElement xmlElement)
        {
            IEnumerable<XAttribute> xmlAttributes = xmlElement.Attributes();
            if (xmlAttributes != null)
            {
                int xmlAttrsLength = xmlAttributes.Count();

                for (int i = 0; i < xmlAttrsLength; ++i)
                {
                    XAttribute xmlCurrentAttr = xmlAttributes.ElementAt(i);
                    xmlCurrentAttr.SetValue(START_SPAN + xmlCurrentAttr.Value + END_SPAN);
                }
            }

            IEnumerable<XElement> xmlNodes = xmlElement.Nodes()?.Select(node => node as XElement);
            if (xmlNodes != null)
            {
                for (int i = 0; i < xmlNodes.Count(); ++i)
                {
                    XElement xmlCurrentElement = xmlNodes.ElementAt(i);
                    RecurseHighlight(xmlCurrentElement);
                }
            }
        }

        /// <summary>
        /// Class to store two XMLs (assigned and actual)
        /// </summary>
        public class XmlAdvanced
        {
            public string XmlLeft { private set; get; }

            public string XmlRight { private set; get; }

            public XmlAdvanced(string xmlLeft, string xmlRight)
            {
                XmlLeft = xmlLeft;
                XmlRight = xmlRight;
            }
        }
    }
}
