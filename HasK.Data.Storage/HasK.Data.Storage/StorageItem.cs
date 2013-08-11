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
        private string _name = String.Empty;
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
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == String.Empty)
                    return;
                if (Storage.TryChangeItemName(this, value))
                    _name = value;
                else
                    throw new StorageItemExistsException(this.Storage);
            }
        }
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
            _name = name;
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

        /// <summary>
        /// Delete item from its storage
        /// </summary>
        public void DeleteItem()
        {
            Storage.DeleteItem(this);
        }
    }
}
