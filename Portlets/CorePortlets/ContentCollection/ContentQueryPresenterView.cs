using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using ContentRepository;
using System.Xml;
using Search;

namespace Portal.Portlets
{


    

    public class ContentQueryPresenterView : ContentCollectionView
    {

        public new ContentQueryPresenterViewModel Model 
        { 
            get {return  base.Model as ContentQueryPresenterViewModel; }
            private set { base.Model = value; }
        }

        public string SomeT()
        {
            return T;
        }

        public string T
        {
            get { return "Hajni"; }
        }
    }
}
