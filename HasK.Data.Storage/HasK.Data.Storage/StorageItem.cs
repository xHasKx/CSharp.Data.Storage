using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.IO;

namespace HasK.Data.Storage
{
    /// <summary>
    /// Base class for all in-storage items
    /// </summary>
    public class StorageItem
    {
        /// <summary>
        /// Storage which contains this item
        /// </summary>
        public Storage Storage { get; private set; }
        /// <summary>
        /// ID of item
        /// </summary>
        public ulong ID { get; internal set; }
        /// <summary>
        /// Name of item
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Internal type name of item
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// Init storage part of item
        /// </summary>
        /// <param name="storage">Storage instance which stores this item</param>
        /// <param name="type">Type of item</param>
        /// <param name="name">Name of item</param>
        /// <param name="id">ID of item</param>
        internal void InitStorageItem(Storage storage, string type, string name, ulong id)
        {
            Storage = storage;
            TypeName = type;
            Name = name;
            ID = id;
        }

        /// <summary>
        /// Get item type instance
        /// </summary>
        /// <returns>Returns item type if it's registered in storage, otherwise null</returns>
        public Type GetItemType()
        {
            return Storage.GetTypeByName(TypeName);
        }

		/*
        public static StorageItem ReadItemFromData(Storage storage, Stream stream, params object[] params_list)
        {
            var xml = XmlReader.Create(stream);
            xml.Read();
            if (xml.Name != "Item")
                throw new StorageException(storage, "Can't read Item node from input stream");

            if (!xml.MoveToFirstAttribute())
                throw new StorageException(storage, "Can't find any attributes in input stream");

            do {
                Console.WriteLine(xml.Name);

            } while (xml.MoveToNextAttribute());

            return null;
            /*
            if (!xml.MoveToAttribute("Type"))
                throw new StorageException(storage, "Can't find Type attribute in input stream");
            var item_type = xml.GetAttribute("Type");
            var type_instance = storage.GetTypeByName(item_type);
            if (type_instance == null)
                throw new StorageException(storage, "Wrong item's Type in input stream");

            if (!xml.MoveToAttribute("ID"))
                throw new StorageException(storage, "Can't find ID attribute in input stream");
            var item_id_str = xml.GetAttribute("ID");
            ulong item_id = 0;
            if (!ulong.TryParse(item_id_str, out item_id))
                throw new StorageException(storage, "Can't parse ID attribute as ulong in input stream");

            if (!xml.MoveToAttribute("Name"))
                throw new StorageException(storage, "Can't find Name attribute in input stream");
            var item_name = xml.GetAttribute("Name");


            var params_types = new Type[params_list.Length];
            for (var i = 0; i < params_list.Length; i++)
                params_types[i] = params_list[i].GetType();
            var constructor = type_instance.GetConstructor(params_types);
            var item = constructor.Invoke(params_list) as StorageItem;



            item.InitStorageItem(storage, item_type, item_name);



            var fields = type_instance.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.GetField |
                BindingFlags.GetProperty);

            */
		/*
        }
		*/
    }
}
