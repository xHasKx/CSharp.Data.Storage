using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;

namespace HasK.Data.Storage
{
    /// <summary>
    /// Class of universal storage instances
    /// </summary>
    public class Storage
    {
        #region Internal fields
        /// <summary>
        /// Stores last ID in this storage
        /// </summary>
        protected ulong _last_id = 0;
        /// <summary>
        /// Stores count of items in storage
        /// </summary>
        protected ulong _items_count = 0;
        /// <summary>
        /// Dictionary with registered types of storage, by names
        /// </summary>
        protected Dictionary<string, Type> _types = new Dictionary<string, Type>();
        /// <summary>
        /// Dictionary with storage items, by IDs
        /// </summary>
        protected Dictionary<ulong, StorageItem> _items_by_id = new Dictionary<ulong, StorageItem>();
        /// <summary>
        /// Dictionary which stores items by types
        /// </summary>
        protected Dictionary<string, Dictionary<string, StorageItem>> _items_by_type = new Dictionary<string, Dictionary<string, StorageItem>>();
        #endregion

        /// <summary>
        /// Array of supported types
        /// </summary>
        public readonly Type[] SupportedTypes = new Type[] {
            typeof(Boolean), typeof(String), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(Double)
        };

        /// <summary>
        /// Gets next free id and increment it
        /// </summary>
        public ulong GetNextID()
        {
            _last_id += 1;
            return _last_id;
        }

        /// <summary>
        /// Register item type in storage
        /// </summary>
        /// <param name="name">Internal name of type</param>
        /// <param name="type">Type instance</param>
        /// <returns>Returns false if given type or name already presented in storage, otherwise true</returns>
        public bool RegisterType(string name, Type type)
        {
            if (!_types.ContainsKey(name) && !_types.ContainsValue(type))
            {
                _types[name] = type;
                _items_by_type[name] = new Dictionary<string, StorageItem>();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get registered type by given type name
        /// </summary>
        /// <param name="name">Internal name of type</param>
        /// <returns>Returns type by given name if presented, otherwise null</returns>
        public Type GetTypeByName(string name)
        {
            if (_types.ContainsKey(name))
                return _types[name];
            return null;
        }

        /// <summary>
        /// Get all items in storage
        /// </summary>
        public IEnumerable<StorageItem> GetItems()
        {
            foreach (var item in _items_by_id.Values)
                yield return item;
        }

        /// <summary>
        /// Get all items with specified type
        /// </summary>
        /// <param name="type">Type name of items to enumerate</param>
        public IEnumerable<StorageItem> GetItems(string type)
        {
            if (_items_by_type.ContainsKey(type))
                foreach (var item in _items_by_type[type].Values)
                    yield return item;
            /*
            foreach (var item in _items_by_id.Values)
                if (item.TypeName == type)
                    yield return item; */
        }

        /// <summary>
        /// Create item with given type and name
        /// </summary>
        /// <param name="type">Type of item</param>
        /// <param name="name">Name of item</param>
        /// <returns>Returns created item</returns>
        public StorageItem CreateItem(string type, string name)
        {
            if (!_types.ContainsKey(type))
                throw new StorageException(this, String.Format("Can't create item: type {0} is not registered in storage", type));
            var item = CreateItemImpl(type, name, GetNextID());
            _items_count += 1;
            return item;
        }

        private StorageItem CreateItemImpl(string type, string name, ulong id)
        {
            if (GetItemByName(type, name) != null)
                throw new StorageItemExistsException(null);
            var type_instance = GetTypeByName(type);
            var constructor = type_instance.GetConstructor(new Type[0]);
            var item = constructor.Invoke(new object[0]) as StorageItem;
            item.InitStorageItem(this, type, name, id);
            _items_by_id[id] = item;
            _items_by_type[type][name] = item;
            return item;
        }

        /// <summary>
        /// Get storage item by specified type and name
        /// </summary>
        /// <param name="type">Type of item</param>
        /// <param name="name">Name of item</param>
        /// <returns>Returns storage item by specified type and name if exists, otherwise null</returns>
        public StorageItem GetItemByName(string type, string name)
        {
            if (!_items_by_type.ContainsKey(type))
                return null;
            var type_map = _items_by_type[type];
            if (!type_map.ContainsKey(name))
                return null;
            return type_map[name];
        }

        /// <summary>
        /// Determine if name can be changed (will not cause name conflicts in its type) and change it in storage
        /// </summary>
        /// <param name="item">Item to change name</param>
        /// <param name="new_name">New name of item</param>
        /// <returns>Returns true if name can be changed and change it in storage internal map</returns>
        internal bool TryChangeItemName(StorageItem item, string new_name)
        {
            var type_map = _items_by_type[item.TypeName];
            if (type_map.ContainsKey(new_name))
                return false;
            type_map.Remove(item.Name);
            type_map[new_name] = item;
            return true;
        }

        /// <summary>
        /// Get item by given ID
        /// </summary>
        /// <param name="id">ID of item</param>
        /// <returns>Returns item with given ID or null if not presented in storage</returns>
        public StorageItem GetItemById(ulong id)
        {
            if (_items_by_id.ContainsKey(id))
                return _items_by_id[id];
            return null;
        }

        /// <summary>
        /// Write XML attribute value - as is or in base64
        /// </summary>
        /// <param name="xml">XML writer ready for attribute</param>
        /// <param name="name">Name of attribute</param>
        /// <param name="value">Value of attribute</param>
        private void WriteXmlAttr(XmlWriter xml, string name, string value)
        {
            var has_illegal_chars = false;
            for (int i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (character == 0x9 || character == 0xA || character == 0xD ||
                    (character >= 0x20 && character <= 0xD7FF) ||
                    (character >= 0xE000 && character <= 0xFFFD))
                {
                    // ok
                }
                else
                {
                    has_illegal_chars = true;
                    break;
                }
            }
            if (has_illegal_chars)
            {
                xml.WriteStartAttribute("x", name, "base64");
                var bytes = Encoding.Unicode.GetBytes(value);
                xml.WriteBase64(bytes, 0, bytes.Length);
            }
            else
            {
                xml.WriteStartAttribute(name);
                xml.WriteValue(value);
            }
            xml.WriteEndAttribute();
        }

        /// <summary>
        /// Clear all items in storage
        /// </summary>
        public void ClearItems()
        {
            _items_by_id.Clear();
            foreach (var pair in _items_by_type)
                pair.Value.Clear();
            _last_id = 0;
            _items_count = 0;
        }

        /// <summary>
        /// Read data from stream, all existing items will be cleared
        /// </summary>
        /// <param name="stream">Stream to read data from</param>
        public void ReadData(Stream stream)
        {
            ClearItems();
            var xml = XmlReader.Create(stream);
            xml.Read();
            if (xml.Name != "Storage")
                throw new StorageException(this, "Can't read Storage node from input stream");
            if (!xml.HasAttributes)
                throw new StorageException(this, "Storage node hasn't attributes in input stream");

            // read storage props
            if (!xml.MoveToAttribute("LastID"))
                throw new StorageException(this, "Storage node hasn't LastID attribute in input stream");
            if (!ulong.TryParse(xml.GetAttribute("LastID"), out _last_id))
                throw new StorageException(this, "Storage node has wrong LastID attribute in input stream");

            if (!xml.MoveToAttribute("ItemsCount"))
                throw new StorageException(this, "Storage node hasn't ItemsCount attribute in input stream");
            if (!ulong.TryParse(xml.GetAttribute("ItemsCount"), out _items_count))
                throw new StorageException(this, "Storage node has wrong ItemsCount attribute in input stream");

            // now read items
            var type_fields = new Dictionary<string, FieldInfo[]>();
            foreach (var pair in _types)
                type_fields[pair.Key] = pair.Value.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.GetField |
                BindingFlags.GetProperty);

            ulong index = 0;
            while (index < _items_count)
            {
                if (!xml.Read())
                    throw new StorageException(this, "Can't read #{0} storage item from input stream", index);
                if (xml.Name == "Item" && xml.NodeType == XmlNodeType.Element)
                {
                    if (!xml.HasAttributes)
                        throw new StorageException(this, "Can't find attributes in #{0} storage item in input stream", index);

                    if (!xml.MoveToAttribute("Type"))
                        throw new StorageException(this, "Can't find Type attribute in #{0} storage item in input stream", index);
                    var item_type = xml.GetAttribute("Type");
                    var type_instance = GetTypeByName(item_type);
                    if (type_instance == null)
                        throw new StorageException(this, "Wrong item's Type in #{0} storage item in input stream", index);

                    if (!xml.MoveToAttribute("ID"))
                        throw new StorageException(this, "Can't find ID attribute in #{0} storage item in input stream", index);
                    var item_id_str = xml.GetAttribute("ID");
                    ulong item_id = 0;
                    if (!ulong.TryParse(item_id_str, out item_id))
                        throw new StorageException(this, "Can't parse ID attribute as ulong in #{0} storage item in input stream", index);

                    bool x_name = false;
                    if (!xml.MoveToAttribute("Name"))
                        if (!xml.MoveToAttribute("x:Name"))
                            throw new StorageException(this, "Can't find Name attribute in #{0} storage item in input stream", index);
                        else
                            x_name = true;
                    string item_name;
                    if (x_name)
                        item_name = Encoding.Unicode.GetString(Convert.FromBase64String(xml.GetAttribute("x:Name")));
                    else
                        item_name = xml.GetAttribute("Name");

                    var item = CreateItemImpl(item_type, item_name, item_id);

                    // read other props
                    foreach (var field in type_fields[item_type])
                    {
                        var name = field.Name;
                        if (name[0] == '<') // ugly but working
                        {
                            var end = name.IndexOf('>');
                            name = name.Substring(1, end - 1);
                        }
                        var supported = false;
                        foreach (var type in SupportedTypes)
                            if (field.FieldType == type)
                            {
                                supported = true;
                                break;
                            }
                        if (!supported)
                            throw new StorageItemException(item, "Can't read property of item with ID {0}: unsupported field '{1}' type {2}", item.ID, name, field.FieldType);
                        if (Attribute.IsDefined(field, typeof(StorageItemMemberIgnore)))
                            continue;
                        if (!xml.MoveToAttribute(name))
                            throw new StorageException(this, "Can't find attribute '{0}' in storage item with ID {1} in input stream", name, item.ID);
                        var value = xml.GetAttribute(name);

                        var ftp = field.FieldType;
                        if (ftp == typeof(Boolean))
                        {
                            if (value == "True")
                                    field.SetValue(item, true);
                                else
                                    field.SetValue(item, false);
                        }
                        else if (ftp == typeof(String))
                        {
                            if (name.Substring(0, 2) == "x:")
                                value = Encoding.Unicode.GetString(Convert.FromBase64String(value));
                            field.SetValue(item, value);
                        }
                        else if (ftp == typeof(Int32))
                        {
                            Int32 ready_value;
                            if (!Int32.TryParse(value, out ready_value))
                                throw new StorageItemException(item, "Can't parse {0} property '{1}' of item with ID {2}", ftp, name, item.ID);
                            else
                                field.SetValue(item, ready_value);
                        }
                        else if (ftp == typeof(UInt32))
                        {
                            UInt32 ready_value;
                            if (!UInt32.TryParse(value, out ready_value))
                                throw new StorageItemException(item, "Can't parse {0} property '{1}' of item with ID {2}", ftp, name, item.ID);
                            else
                                field.SetValue(item, ready_value);
                        }
                        else if (ftp == typeof(Int64))
                        {
                            Int64 ready_value;
                            if (!Int64.TryParse(value, out ready_value))
                                throw new StorageItemException(item, "Can't parse {0} property '{1}' of item with ID {2}", ftp, name, item.ID);
                            else
                                field.SetValue(item, ready_value);
                        }
                        else if (ftp == typeof(UInt64))
                        {
                            UInt64 ready_value;
                            if (!UInt64.TryParse(value, out ready_value))
                                throw new StorageItemException(item, "Can't parse {0} property '{1}' of item with ID {2}", ftp, name, item.ID);
                            else
                                field.SetValue(item, ready_value);
                        }
                        else if (ftp == typeof(Double))
                        {
                            Double ready_value;
                            if (!Double.TryParse(value, out ready_value))
                                throw new StorageItemException(item, "Can't parse {0} property '{1}' of item with ID {2}", ftp, name, item.ID);
                            else
                                field.SetValue(item, ready_value);
                        }
                    }
                    index += 1;
                }
            }
        }

        /// <summary>
        /// Write storage items to stream
        /// </summary>
        /// <param name="stream">Stream to write data</param>
        public void WriteData(Stream stream)
        {
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            var xml = XmlWriter.Create(stream, settings);

            // write storage info
            xml.WriteStartElement("Storage");
            WriteXmlAttr(xml, "LastID", _last_id.ToString());
            WriteXmlAttr(xml, "ItemsCount", _items_count.ToString());

            xml.Flush();
            xml.WriteRaw("\n");

            var type_fields = new Dictionary<string, FieldInfo[]>();
            foreach (var pair in _types)
                type_fields[pair.Key] = pair.Value.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.GetField |
                BindingFlags.GetProperty);

            // write items
            foreach (var pair in _items_by_id)
            {
                var item = pair.Value;
                xml.WriteStartElement("Item");
                WriteXmlAttr(xml, "Type", item.TypeName);
                WriteXmlAttr(xml, "ID", item.ID.ToString());
                WriteXmlAttr(xml, "Name", item.Name);
                var fields = type_fields[item.TypeName];
                foreach (var field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(StorageItemMemberIgnore)))
                        continue;
                    var supported = false;
                    foreach (var type in SupportedTypes)
                        if (field.FieldType == type)
                        {
                            supported = true;
                            break;
                        }
                    if (!supported)
                        throw new StorageItemException(item, "Can't write item with ID {0}: unsupported field {1} type {2}", item.ID, field.Name, field.FieldType);
                    var name = field.Name;
                    if (name[0] == '<') // ugly but working
                    {
                        var end = name.IndexOf('>');
                        name = name.Substring(1, end - 1);
                    }
                    WriteXmlAttr(xml, name, field.GetValue(item).ToString());
                }
                xml.WriteEndElement();
                xml.WriteRaw("\n");
            }
            xml.WriteEndElement();
            xml.Flush();
            xml.Close();
        }

        /// <summary>
        /// Delete item from storage
        /// </summary>
        /// <param name="item">Item to delete</param>
        public void DeleteItem(StorageItem item)
        {
            if (item.Storage == this)
            {
                _items_by_type[item.TypeName].Remove(item.Name);
                _items_by_id.Remove(item.ID);
            }
        }
    }
}
