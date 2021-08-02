// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping.ViewGeneration.QueryRewriting
{
    using System.Data.Entity.Utilities;

    internal class TileNamed<T_Query> : Tile<T_Query>
        where T_Query : ITileQuery
    {
        public TileNamed(T_Query namedQuery)
            : base(TileOpKind.Named, namedQuery)
        {
            DebugCheck.NotNull((object)namedQuery);
        }

        public T_Query NamedQuery
        {
            get { return Query; }
        }

        public override Tile<T_Query> Arg1
        {
            get { return null; }
        }

        public override Tile<T_Query> Arg2
        {
            get { return null; }
        }

        public override string Description
        {
            get { return Query.Description; }
        }

        public override string ToString()
        {
            return Query.ToString();
        }

        internal override Tile<T_Query> Replace(Tile<T_Query> oldTile, Tile<T_Query> newTile)
        {
            return (this == oldTile) ? newTile : this;
        }
    }
}
