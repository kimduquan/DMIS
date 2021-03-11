using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Linq.Expressions;
using ContentRepository.Storage.Caching.DistributedActions;
using Communication.Messaging;
using ContentRepository;


namespace Tools.ClusterMessageConsole
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ClusterMessage[] creatableMessages = new ClusterMessage[] 
            {
                new CacheCleanAction(), 
                new PingMessage()
            };
            comboBox1.DataSource = creatableMessages;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DistributedApplication.ClusterChannel.MessageReceived += 
                new MessageReceivedEventHandler(ClusterChannel_MessageReceived);
            var channel = ContentRepository.DistributedApplication.ClusterChannel;
            channel.AllowMessageProcessing = true;
        }

        void MessageHandler(object sender, EventArgs args)
        {
            listView1.Items.Add(new ClusterMessageListViewItem(((MessageReceivedEventArgs)args).Message));
        }

        void ClusterChannel_MessageReceived(object sender, MessageReceivedEventArgs args)
        {

            listView1.Invoke(new EventHandler(MessageHandler), sender, args);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //new PingMessage().Send();
            ((ClusterMessage)comboBox1.SelectedItem).Send();
        }

        //private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //}
    }



    public class ClusterMessageListViewItem : ListViewItem
    {
        private static int _counter;

        public ClusterMessageListViewItem(ClusterMessage message)
        {
            this.Text = _counter++.ToString();
            this.SubItems.AddRange(new string[] { message.ToString(), message.SenderInfo.InstanceID, DateTime.UtcNow.ToString() });
        }
    }
}
