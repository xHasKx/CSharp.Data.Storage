using System;
using System.Collections.Generic;
using System.Text;

namespace HasK.Data.Storage
{
    public class StorageException : Exception
    {
        public Storage Storage { get; private set; }

        public StorageException(Storage storage, string msg)
            : base(msg)
        {
            Storage = storage;
        }
    }

    public class StorageItemException : StorageException
    {
        public StorageItem Item { get; private set; }

        public StorageItemException(Storage storage, StorageItem item, string msg)
            : base(storage, msg)
        {
            Item = item;
        }
    }


}
