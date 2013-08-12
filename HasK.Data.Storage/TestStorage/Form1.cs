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

        Storage CreateAndSave(string fname)
        {
            var storage = new Storage();
            storage.RegisterType("Worker", typeof(Worker));
            storage.RegisterType("Dept", typeof(Dept));
            var hask = storage.CreateItem("Worker", "HasK") as Worker;
            var w2 = storage.CreateItem("Worker", "Other me") as Worker;

            for (int i = 0; i < 20; i++)
                storage.CreateItem("Worker", "worker #" + i);

            for (int i = 0; i < 5; i++)
            {
                var dept = storage.CreateItem("Dept", "dept_" + i) as Dept;
                dept.Level = i;
            }

            var stream = File.OpenWrite(fname);
            stream.SetLength(0);
            storage.WriteData(stream);
            stream.Flush();
            stream.Close();

            return storage;
        }

        Storage LoadStorage(string fname)
        {
            var storage = new Storage();
            storage.RegisterType("Worker", typeof(Worker));
            storage.RegisterType("Dept", typeof(Dept));
            var read = File.OpenRead(fname);
            storage.ReadData(read);
            return storage;
        }

        Storage CreateRandom(int count)
        {
            var storage = new Storage();
            storage.RegisterType("Worker", typeof(Worker));

            for (int i = 0; i < count; i++)
                storage.CreateItem("Worker", "worker_" + i);

            return storage;
        }

        void DoTest(int items_count)
        {
            DateTime t0, t1;
            var count = 0;
            var r = new Random();
            int i;

            var rand_indexes = new ulong[items_count];
            for (i = 0; i < items_count; i++)
                rand_indexes[i] = (ulong)r.Next(1, items_count);

            var names = new string[items_count];
            for (i = 0; i < items_count; i++)
                names[i] = "worker_" + i;

            t0 = DateTime.Now;
            var storage = CreateRandom(items_count);
            t1 = DateTime.Now;
            var time_create = t1 - t0;

            t0 = DateTime.Now;
            count = 0;
            foreach (var item in storage.GetItems("Worker"))
                count += 1;
            t1 = DateTime.Now;
            var time_enum_worker = t1 - t0;

            t0 = DateTime.Now;
            for (i = 0; i < items_count; i++)
                storage.GetItemById(rand_indexes[i]);
            t1 = DateTime.Now;
            var time_id_lookup = t1 - t0;

            t0 = DateTime.Now;
            for (i = 0; i < items_count; i++)
                storage.GetItemByName("Worker", names[i]);
            t1 = DateTime.Now;
            var time_name_lookup = t1 - t0;

            storage.ClearItems();
            Console.WriteLine("{0};{1};{2};{3};{4}",
                items_count,
                time_create.TotalMilliseconds,
                time_enum_worker.TotalMilliseconds,
                time_id_lookup.TotalMilliseconds,
                time_name_lookup.TotalMilliseconds);
        }

        void DoTests()
        {
            Console.WriteLine("create;enum;id lookup;name lookup");
            var count = 100000;
            DoTest(count);

            count *= 2; DoTest(count);
            count *= 2; DoTest(count);
            count *= 2; DoTest(count);
            count *= 2; DoTest(count);
            count *= 2; DoTest(count);
            count *= 2; DoTest(count);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateAndSave("test3.xml");
            var storage = LoadStorage("test3.xml");
            foreach (var w in storage.GetItems("Worker"))
                Console.WriteLine(w);

            var stor2 = new Storage();
            stor2.RegisterType("SuperWorker", typeof(SuperWorker));

            var sw = stor2.CreateItem("SuperWorker", "sw #1");
            Console.WriteLine("sw1: " + sw);

            sw.DeleteItem();

            stor2.WriteData(File.OpenWrite("test4.xml"));
        }
    }

    public class Worker : StorageItem
    {
        public Worker()
        {
            PrivAge = 24;
            PubProp = 554;
            PrivProp = "private property";
            Age2 = 43;
        }

        [StorageItemMemberIgnore]
        private int _age2;

        private int Age2
        {
            get
            {
                Console.WriteLine("Age2::get() => {0}", _age2);
                return _age2;
            }
            set
            {
                Console.WriteLine("Age2::set(): {0} => {1}", _age2, value);
                _age2 = value;
            }
        }

        private int PrivAge;
        public string PubField = "ddd";
        protected bool ProtField = true;

        public int PubProp { get; set; }
        [StorageItemMemberIgnore]
        private string PrivProp { get; set; }

        public override string ToString()
        {
            return String.Format("Worker '{0}' of age {1} with ID {2}", Name, PrivAge, ID);
        }
    }

    public class SuperWorker : Worker
    {
        public int SuperProperty { get; set; }

        public SuperWorker()
        {
            SuperProperty = 451;
        }
    }

    public class Dept : StorageItem
    {
        public int Level;

        public override string ToString()
        {
            return String.Format("<{0}-level Dept '{1}'>", Level, Name);
        }
    }
}
