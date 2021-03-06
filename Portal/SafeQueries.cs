using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Search;

namespace Portal
{
    /// <summary>Holds safe queries in public static readonly string properties.</summary>
    public class SafeQueries : ISafeQueryHolder
    {
        /// <summary>Returns with the following query: "+Name:'*.xslt' +TypeIs:File .SORT:Path .AUTOFILTERS:OFF"</summary>
        public static string PreloadXslt { get { return "+Name:'*.xslt' +TypeIs:File .SORT:Path .AUTOFILTERS:OFF"; } }
        /// <summary>Returns with the following query: "+InTree:@0 +Depth:@1 .AUTOFILTERS:OFF"</summary>
        public static string PreloadContentTemplates { get { return "+InTree:@0 +Depth:@1 .AUTOFILTERS:OFF"; } }

        public static string PreloadControls { get { return "+Name:\"*.ascx\" -InTree:\"/Root/Global/celltemplates\" -Path:'/Root/Global/renderers/MyDataboundView.ascx' .SORT:Path .AUTOFILTERS:OFF"; } }
        /// <summary>Returns with the following query: ""</summary>
        public static string Resources { get { return "+TypeIs:Resource"; } }
        /// <summary>Returns with the following query: "+TypeIs:Resource +ModificationDate:>@0"</summary>
        public static string ResourcesAfterADate { get { return "+TypeIs:Resource +ModificationDate:>@0"; } }


        // =============================================================== Wiki queries

        public static string WikiArticlesByDisplayName { get { return "+TypeIs:WikiArticle +DisplayName:@0"; } }
        public static string WikiArticlesByDisplayNameAndSubtree { get { return "+TypeIs:WikiArticle +DisplayName:@0 +InTree:@1"; } }
        public static string WikiArticlesByPath { get { return "+TypeIs:WikiArticle +Path:@0"; } }
        public static string WikiArticlesByReferenceTitlesAndSubtree { get { return "+TypeIs:WikiArticle +ReferencedWikiTitles:@0 +InTree:@1"; } }
    }
}
