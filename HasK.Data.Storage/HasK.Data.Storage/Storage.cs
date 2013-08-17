using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Linq;

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
        /// Dictionary with fields of registered types
        /// </summary>
        protected Dictionary<string, TypeMembers> _type_members = new Dictionary<string, TypeMembers>();
        /// <summary>
        /// Dictionary with storage items, by IDs
        /// </summary>
        protected Dictionary<ulong, StorageItem> _items_by_id = new Dictionary<ulong, StorageItem>();
        /// <summary>
        /// Dictionary which stores items by types
        /// </summary>
        protected Dictionary<string, Dictionary<string, StorageItem>> _items_by_type = new Dictionary<string, Dictionary<string, StorageItem>>();
        /// <summary>
        /// List of restricted member names
        /// </summary>
        protected string[] _restricted_names;
        #endregion

        #region Internal types
        /// <summary>
        /// Class which stores item's members list
        /// </summary>
        protected class TypeMembers
        {
            /// <summary>
            /// List of fields
            /// </summary>
            public FieldInfo[] Fields;
            /// <summary>
            /// List of properties
            /// </summary>
            public PropertyInfo[] Properties;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create storage instance
        /// </summary>
        public Storage()
        {
            // collect list of restricted names
            var rnames = new List<string>();
            var type = typeof(StorageItem);
            foreach (var property in type.GetProperties(BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                rnames.Add(property.Name);
            foreach (var field in type.GetFields(BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                if (!IsBackendField(field.Name))
                    rnames.Add(field.Name);
            rnames.Add("Type");
            _restricted_names = rnames.ToArray();
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Gets next free id and increment it
        /// </summary>
        internal ulong GetNextID()
        {
            _last_id += 1;
            return _last_id;
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
        /// Create item method implementation
        /// </summary>
        /// <param name="type">Type of item</param>
        /// <param name="name">Name of item</param>
        /// <param name="id">ID of item</param>
        /// <returns>Returns new created item</returns>
        private StorageItem CreateItemImpl(string type, string name, ulong id)
        {
            if (GetItemById(id) != null || GetItemByName(type, name) != null)
                throw new StorageItemExistsException(this);
            var type_instance = GetTypeByName(type);
            var constructor = type_instance.GetConstructor(new Type[0]);
            var item = constructor.Invoke(new object[0]) as StorageItem;
            item.InitStorageItem(this, type, name, id);
            _items_by_id[id] = item;
            _items_by_type[type][name] = item;
            return item;
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
                    // ok, but ugly
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
        #endregion

        #region Internal checker methods
        /// <summary>
        /// Check if field is a Backend Field
        /// </summary>
        /// <param name="name">Name of field</param>
        /// <returns>Returns true of field is a Backend Field</returns>
        private bool IsBackendField(string name)
        {
            return name.StartsWith("<");
        }

        /// <summary>
        /// Check if item's member name allowed to store in item
        /// </summary>
        /// <param name="name">Name of item's member</param>
        /// <returns>Returns true if item's member name allowed</returns>
        protected bool IsNameAllowed(string name)
        {
            return (!_restricted_names.Contains(name) && !IsBackendField(name));
        }

        /// <summary>
        /// Check if item's member should be ignored
        /// </summary>
        /// <param name="member">Item's member to check</param>
        /// <returns>Returns true if item's member should be ignored</returns>
        protected bool IsMemberIgnored(MemberInfo member)
        {
            return member.IsDefined(typeof(StorageItemMemberIgnore), true);
        }

        /// <summary>
        /// Check if specified type can be stored
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>Returns true if type can be stored</returns>
        protected bool IsTypeCanBeStored(Type type)
        {
            return (type == typeof(String) || (type.GetMethod("Parse", new Type[] { typeof(String) }) != null &&
                type.GetMethod("ToString", new Type[0]) != null));
        }

        /// <summary>
        /// Check item's property for all storage constraints
        /// </summary>
        /// <param name="property">Item's property to check</param>
        /// <returns>Returns true if property can be stored</returns>
        protected bool CheckProperty(PropertyInfo property)
        {
            if (IsNameAllowed(property.Name) &&
                !IsMemberIgnored(property) &&
                property.CanRead && property.CanWrite &&
                IsTypeCanBeStored(property.PropertyType))
                return true;
            return false;
        }

        /// <summary>
        /// Check item's field for all storage constraints
        /// </summary>
        /// <param name="field">Item's field to check</param>
        /// <returns>Returns true if field can be stored</returns>
        protected bool CheckField(FieldInfo field)
        {
            if (IsNameAllowed(field.Name) &&
                !IsMemberIgnored(field) &&
                IsTypeCanBeStored(field.FieldType))
                return true;
            return false;
        }
        #endregion

        #region Public methods
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
                
                var fields = new List<FieldInfo>();
                var properties = new List<PropertyInfo>();

                while (type != typeof(StorageItem))
                {
                    foreach (var property in type.GetProperties(BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                        if (CheckProperty(property) && (from p in properties where p.Name == property.Name select p).Count() == 0)
                            properties.Add(property);
                    foreach (var field in type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic))
                        if (CheckField(field) && (from f in fields where f.Name == field.Name select f).Count() == 0)
                            fields.Add(field);
                    type = type.BaseType;
                }
                var tm = new TypeMembers();
                tm.Fields = fields.ToArray();
                tm.Properties = properties.ToArray();
                _type_members[name] = tm;
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
        /// Read data from string content, all existing items will be cleared
        /// </summary>
        /// <param name="content"></param>
        public void ReadData(string content)
        {
            ReadData(XmlReader.Create(new StringReader(content)));
        }

        /// <summary>
        /// Read data from stream, all existing items will be cleared
        /// </summary>
        /// <param name="stream"></param>
        public void ReadData(Stream stream)
        {
            ReadData(XmlReader.Create(stream));
        }

        /// <summary>
        /// Read data from xml reader, all existing items will be cleared
        /// </summary>
        /// <param name="xml">Xml reader to read data from</param>
        public void ReadData(XmlReader xml)
        {
            ClearItems();
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

                    var tm = _type_members[item_type];
                    // read fields
                    foreach (var field in tm.Fields)
                    {
                        var name = field.Name;
                        if (!xml.MoveToAttribute(name))
                            throw new StorageException(this, "Can't find attribute '{0}' in storage item with ID {1} in input stream", name, item.ID);
                        var value = xml.GetAttribute(name);
                        Object parsed_value;
                        if (field.FieldType == typeof(String))
                            parsed_value = value;
                        else
                        {
                            var parse = field.FieldType.GetMethod("Parse", new Type[] { typeof(String) });
                            try
                            {
                                parsed_value = parse.Invoke(null, new object[] { value });
                            }
                            catch (Exception exc)
                            {
                                throw new StorageException(this,
                                    "Can't parse field '{0}' in storage item with ID {1} from value '{2}' in input stream: {3}", name, item.ID, value, exc);
                            }
                        }
                        field.SetValue(item, parsed_value);
                    }
                    // read properties
                    foreach (var property in tm.Properties)
                    {
                        var name = property.Name;
                        if (!xml.MoveToAttribute(name))
                            throw new StorageException(this, "Can't find attribute '{0}' in storage item with ID {1} in input stream", name, item.ID);
                        var value = xml.GetAttribute(name);
                        Object parsed_value;
                        if (property.PropertyType == typeof(String))
                            parsed_value = value;
                        else
                        {
                            var parse = property.PropertyType.GetMethod("Parse", new Type[] { typeof(String) });
                            try
                            {
                                parsed_value = parse.Invoke(null, new object[] { value });
                            }
                            catch (Exception exc)
                            {
                                throw new StorageException(this,
                                    "Can't parse field '{0}' in storage item with ID {1} from value '{2}' in input stream: {3}", name, item.ID, value, exc);
                            }
                        }
                        property.GetSetMethod(true).Invoke(item, new object[] { parsed_value });
                    }
                    index += 1;
                }
            }
        }

        /// <summary>
        /// Write storage content to string and return it
        /// </summary>
        /// <returns>Returns string with storage data</returns>
        public string WriteData()
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            WriteData(XmlWriter.Create(sb, settings));
            return sb.ToString();
        }

        /// <summary>
        /// Write storage content to stream
        /// </summary>
        /// <param name="stream">Stream to write data</param>
        public void WriteData(Stream stream)
        {
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            WriteData(XmlWriter.Create(stream, settings));
        }

        /// <summary>
        /// Write storage content to xml writer
        /// </summary>
        /// <param name="xml">Xml writer to write data</param>
        public void WriteData(XmlWriter xml)
        {
            // write storage info
            xml.WriteStartElement("Storage");
            WriteXmlAttr(xml, "LastID", _last_id.ToString());
            WriteXmlAttr(xml, "ItemsCount", _items_count.ToString());

            xml.Flush();
            xml.WriteRaw("\n");

            // write items
            foreach (var pair in _items_by_id)
            {
                var item = pair.Value;
                xml.WriteStartElement("Item");
                WriteXmlAttr(xml, "Type", item.TypeName);
                WriteXmlAttr(xml, "ID", item.ID.ToString());
                WriteXmlAttr(xml, "Name", item.Name);
                var tm = _type_members[item.TypeName];
                // write fields
                foreach (var field in tm.Fields)
                {
                    var name = field.Name;
                    WriteXmlAttr(xml, name, field.GetValue(item).ToString());
                }
                // write properties
                foreach (var property in tm.Properties)
                {
                    var name = property.Name;
                    var value = property.GetGetMethod(true).Invoke(item, new object[0]);
                    WriteXmlAttr(xml, name, value.ToString());
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
                _items_count -= 1;
            }
        }
        #endregion
    }
}
