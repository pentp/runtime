// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ApplicationPartitionCollection : ReadOnlyCollectionBase
    {
        internal ApplicationPartitionCollection() { }

        internal ApplicationPartitionCollection(ArrayList values)
        {
            if (values != null)
            {
                InnerList.AddRange(values);
            }
        }

        public ApplicationPartition this[int index] => (ApplicationPartition)InnerList[index]!;

        public bool Contains(ApplicationPartition applicationPartition)
        {
            ArgumentNullException.ThrowIfNull(applicationPartition);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ApplicationPartition tmp = (ApplicationPartition)InnerList[i]!;
                if (Utils.Compare(tmp.Name, applicationPartition.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(ApplicationPartition applicationPartition)
        {
            ArgumentNullException.ThrowIfNull(applicationPartition);

            for (int i = 0; i < InnerList.Count; i++)
            {
                ApplicationPartition tmp = (ApplicationPartition)InnerList[i]!;
                if (Utils.Compare(tmp.Name, applicationPartition.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public void CopyTo(ApplicationPartition[] applicationPartitions, int index)
        {
            InnerList.CopyTo(applicationPartitions, index);
        }
    }
}
