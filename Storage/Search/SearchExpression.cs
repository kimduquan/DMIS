using System;

namespace ContentRepository.Storage.Search
{
	public class SearchExpression : Expression
	{
		private string _fullTextExpression;

        public string FullTextExpression
		{
			get
			{
				if (_fullTextExpression == null)
					return String.Empty;
				return _fullTextExpression;
			}
		}

		public SearchExpression(string fullTextExpression)
		{
			if (fullTextExpression == null)
				throw new ArgumentNullException("fullTextExpression");
			_fullTextExpression = fullTextExpression;
		}

		internal override void WriteXml(System.Xml.XmlWriter writer)
		{
            writer.WriteStartElement("FullText", NodeQuery.XmlNamespace);
			writer.WriteString(_fullTextExpression);
			writer.WriteEndElement();
		}
	}
}