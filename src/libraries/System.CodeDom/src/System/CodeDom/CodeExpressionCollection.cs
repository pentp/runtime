// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.CodeDom
{
    public class CodeExpressionCollection : CollectionBase
    {
        public CodeExpressionCollection() { }

        public CodeExpressionCollection(CodeExpressionCollection value)
        {
            AddRange(value);
        }

        public CodeExpressionCollection(CodeExpression[] value)
        {
            AddRange(value);
        }

        public CodeExpression this[int index]
        {
            get => (CodeExpression)List[index];
            set => List[index] = value;
        }

        public int Add(CodeExpression value) => List.Add(value);

        public void AddRange(CodeExpression[] value)
        {
            ArgumentNullException.ThrowIfNull(value);

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(CodeExpressionCollection value)
        {
            ArgumentNullException.ThrowIfNull(value);

            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(CodeExpression value) => List.Contains(value);

        public void CopyTo(CodeExpression[] array, int index) => List.CopyTo(array, index);

        public int IndexOf(CodeExpression value) => List.IndexOf(value);

        public void Insert(int index, CodeExpression value) => List.Insert(index, value);

        public void Remove(CodeExpression value) => List.Remove(value);
    }
}
