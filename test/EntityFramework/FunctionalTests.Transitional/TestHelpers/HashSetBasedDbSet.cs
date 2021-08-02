﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Internal;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// In-memory implementation of IDbSet based on a <see cref="HashSet{T}" />
    /// </summary>
    /// <typeparam name="T"> Type of elements to be stored in the set </typeparam>
    public class HashSetBasedDbSet<T> : IDbSet<T>
        where T : class, new()
    {
        private readonly HashSet<T> _data;
        private readonly IQueryable _query;
        private readonly Func<IEnumerable<T>, T> _findFunc;

        public HashSetBasedDbSet()
            : this(null)
        {
        }

        public HashSetBasedDbSet(Func<IEnumerable<T>, T> findFunc)
        {
            _data = new HashSet<T>();
            _query = _data.AsQueryable();
            _findFunc = findFunc;
        }

        public T Find(params object[] keyValues)
        {
            if (_findFunc == null)
            {
                throw new NotSupportedException("If you want to call find then use the constructor that specifies a find func.");
            }

            return _findFunc(_data);
        }

        public Task<T> FindAsync(CancellationToken cancellationToken, params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public T Add(T item)
        {
            _data.Add(item);
            return item;
        }

        public T Remove(T item)
        {
            _data.Remove(item);
            return item;
        }

        public T Attach(T item)
        {
            _data.Add(item);
            return item;
        }

        Type IQueryable.ElementType
        {
            get { return _query.ElementType; }
        }

        Expression IQueryable.Expression
        {
            get { return _query.Expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return _query.Provider; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public ObservableCollection<T> Local
        {
            get { return new DbLocalView<T>(_data); }
        }

        public T Create()
        {
            return new T();
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, T
        {
            throw new NotImplementedException();
        }
    }
}
