using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using  ContentRepository.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContentRepository.Storage;
using System.Diagnostics;


namespace ContentRepository.Tests.CrossDomain
{
	[Serializable]
    public class RemotedTests : MarshalByRefObject, IRemotedTests
	{
        public void Initialize(string startupPath)
        {
            var pluginsPath = startupPath;
            try
            {
                pluginsPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(startupPath, @"..\..\..\WebSite\bin"));
                ContentRepository.Storage.TypeHandler.LoadAssembliesFrom(pluginsPath);
                var preload1 = Portal.AppModel.HttpActionManager.PresenterFolderName;
                var preload2 = ContentRepository.Repository.Root;
                var preload3 = typeof(Search.Indexing.DocumentPopulator);
                var dummy2 = DistributedApplication.ClusterChannel;
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                pluginsPath = System.IO.Directory.GetCurrentDirectory();
                ContentRepository.Storage.TypeHandler.LoadAssembliesFrom(pluginsPath);
            }

        }

        public RemotedTests()
        {
        }

		public string[] GetContentTypeNames()
		{
			try
			{
				return ContentType.GetContentTypeNames();
			}
			catch (Exception e)
			{
				return new string[] { e.Message };
			}
		}
		public string[] GetCacheKeys()
		{
			var list = new List<string>();
			foreach (var item in DistributedApplication.Cache)
				list.Add(((System.Collections.DictionaryEntry)item).Key.ToString());
			return list.ToArray();
		}
        public int LoadNodeAndGetId(string path)
        {
            var node = Node.LoadNode(path);
            if (node == null)
                return 0;
            return node.Id;
        }
        public string LoadNodeAndGetFileContent(string path)
        {
            var file = Node.Load<File>(path);
            if (file == null)
                return "[file not found]";
            return Tools.GetStreamString(file.Binary.GetStream());
        }
	}
}
