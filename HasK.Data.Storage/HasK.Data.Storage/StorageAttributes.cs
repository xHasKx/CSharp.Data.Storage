using System;
using System.Collections.Generic;
using System.Text;

namespace HasK.Data.Storage
{
    /// <summary>
    /// Use this attribute to ignore some members of storage items
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StorageItemMemberIgnore : Attribute { }
}