using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Schema;
using ContentRepository.Storage;
using Search;

namespace ContentRepository
{
    [ContentHandler]
    public class RuntimeContentContainer : Folder
    {
        public RuntimeContentContainer(Node parent) : this(parent, typeof(RuntimeContentContainer).Name) { }
        public RuntimeContentContainer(Node parent, string nodeTypeName) : base(parent, nodeTypeName) { }
        protected RuntimeContentContainer(NodeToken token) : base(token) { }

        IEnumerable<Node> __children;

        IEnumerable<Node> _children {
            get
            {
                if (__children == null)
                    __children = InitChildrenForTest();
                return __children;
            }
            set
            {
                __children = value;
            }
        }


        public override IEnumerable<Node> Children { get { return _children; } }
        public void SetChildren(IEnumerable<Node> children)
        {
            if (children == null)
                throw new ArgumentNullException("children");
            _children = children;
        }
        protected override IEnumerable<Node> GetChildren()
        {
            return _children;
        }
        public override QueryResult GetChildren(string text, QuerySettings settings, bool getAllChildren)
        {
            return new QueryResult(_children);
        }




        IEnumerable<Node> InitChildrenForTest()
        {
            string ctd = @"<ContentType name=""RuntimeNode"" parentType=""GenericContent"" handler=""ContentRepository.GenericContent"" xmlns=""http://schemas.com/ContentRepository/ContentTypeDefinition"">
	<DisplayName>RuntimeNode</DisplayName>
	<Description>Use RuntimeNodes to handle an object.</Description>
	<Icon>Folder</Icon>
    <Fields>
        <Field name=""name"" type=""ShortText"">
            <DisplayName>Object Name</DisplayName>
        </Field>
        <Field name=""counter"" type=""Integer"">
            <DisplayName>Counter</DisplayName>
        </Field>

        <Field name=""ModificationDate"" type=""DateTime"">
            <DisplayName>Modification Date</DisplayName>
            <Description>Content was last modified on this date.</Description>
            <Configuration>
                <VisibleBrowse>Hide</VisibleBrowse>
                <VisibleEdit>Hide</VisibleEdit>
                <VisibleNew>Hide</VisibleNew>
                <DateTimeMode>DateAndTime</DateTimeMode>
            </Configuration>
        </Field>

    </Fields>
</ContentType>
";
            var nodes = new Node[3];
            var objectToEdit = new ClassForTest[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                var name = "MyObjectInstance" + i;
                objectToEdit[i] = new ClassForTest() { name = name, counter = 123 + i };
                var content = Content.Create(objectToEdit[i], ctd);
                var runtimeCH = (ContentRepository.Content.RuntimeContentHandler)content.ContentHandler;
                runtimeCH.Name = name;
                nodes[i] = runtimeCH;
            }
            return nodes;
        }
        public class ClassForTest
        {
            string _name;
            public string name
            {
                get { return _name; }
                set { _name = value; }
            }

            int _counter;
            public int counter
            {
                get { return _counter; }
                set { _counter = value; }
            }

            public DateTime ModificationDate { get; set; }
        }

    }
}
