using System;
using System.Collections.Generic;
using System.Text;

namespace HasK.Data.Storage
{
    /// <summary>
    /// Common storage-related exception type
    /// </summary>
    public class StorageException : Exception
    {
        /// <summary>
        /// Storage which throws exception
        /// </summary>
        public Storage Storage { get; private set; }
        /// <summary>
        /// Create storage exception with specified message
        /// </summary>
        /// <param name="storage">Storage which throws exception</param>
        /// <param name="message">Message of exception</param>
        /// <param name="format_params">Params to format exception message</param>
        public StorageException(Storage storage, string message, params object[] format_params)
            : base(String.Format(message, format_params))
        {
            Storage = storage;
        }
    }

    /// <summary>
    /// Common storage item-related exception type
    /// </summary>
    public class StorageItemException : StorageException
    {
        /// <summary>
        /// Storage item which throws exception
        /// </summary>
        public StorageItem Item { get; private set; }
        /// <summary>
        /// Create storage item exception with specified message
        /// </summary>
        /// <param name="item">Storage item which throws exception</param>
        /// <param name="message">Message of exception</param>
        /// <param name="format_params">Params to format exception message</param>
        public StorageItemException(StorageItem item, string message, params object[] format_params)
            : base(item.Storage, message, format_params)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Storage item-related exception which raised when item with given name and type already exists in storage
    /// </summary>
    public class StorageItemExistsException : StorageItemException
    {
        /// <summary>
        /// Create storage item exception with specified message
        /// </summary>
        /// <param name="item">Storage item which throws exception</param>
        /// <param name="message">Message of exception</param>
        /// <param name="format_params">Params to format exception message</param>
        public StorageItemExistsException(StorageItem item, string message, params object[] format_params)
            : base(item, message, format_params) { }

        /// <summary>
        /// Create storage item exception with default message
        /// </summary>
        /// <param name="item">Storage item which throws exception</param>
        public StorageItemExistsException(StorageItem item)
            : base(item, "Item with given name and type already exists in storage") { }
    }
}
