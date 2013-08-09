using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HasK.Data.Storage;
using System.Xml;
using System.IO;

namespace TestStorage
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var storage = new Storage();
            storage.RegisterType("Worker", typeof(Worker));
            var w = storage.CreateItem("Worker", "Ha\x01sK") as Worker;
            storage.CreateItem("Worker", "HasK");

            Console.WriteLine("item by id 1: {0}", storage.GetItemById(1));

            var w2 = storage.CreateItem("Worker", "Other me") as Worker;

			var stream = File.OpenWrite("test2.xml");
			storage.WriteData(stream);
            stream.Flush();
			stream.Close();

			var read = File.OpenRead("test2.xml");

			storage.ReadData(read);

            var ww2 = storage.GetItemById(2);
            Console.WriteLine(ww2);

            storage.CreateItem("Worker", "new item");

            ww2 = storage.GetItemById(3);
            Console.WriteLine("3: " + ww2);

            Console.WriteLine("all items:");
            foreach (var item in storage.GetItems("Worker"))
                Console.WriteLine(item);
        }
    }

    public class Worker : StorageItem
    {
        public Worker()
        {
            PrivAge = 24;
            PubProp = 554;
            PrivProp = "private property";
        }

        [StorageItemMemberIgnore]
        private int PrivAge;
        public string PubField = "ddd";
        protected bool ProtField = true;

        public int PubProp { get; set; }
        private string PrivProp { get; set; }

        public override string ToString()
        {
            return String.Format("Worker '{0}' of age {1} with ID {2}", Name, PrivAge, ID);
        }
    }
}
