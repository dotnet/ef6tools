// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Allows configuration to be performed for a lightweight convention based on
    /// the entity types in a model that inherit from a common, specified type.
    /// </summary>
    /// <typeparam name="T"> The common type of the entity types that this convention applies to. </typeparam>
    public class TypeConventionConfiguration<T>
        where T : class
    {
        private readonly ConventionsConfiguration _conventionsConfiguration;
        private readonly IEnumerable<Func<Type, bool>> _predicates;

        internal TypeConventionConfiguration(ConventionsConfiguration conventionsConfiguration)
            : this(conventionsConfiguration, Enumerable.Empty<Func<Type, bool>>())
        {
            DebugCheck.NotNull(conventionsConfiguration);
        }

        private TypeConventionConfiguration(
            ConventionsConfiguration conventionsConfiguration,
            IEnumerable<Func<Type, bool>> predicates)
        {
            DebugCheck.NotNull(conventionsConfiguration);
            DebugCheck.NotNull(predicates);

            _conventionsConfiguration = conventionsConfiguration;
            _predicates = predicates;
        }

        internal ConventionsConfiguration ConventionsConfiguration
        {
            get { return _conventionsConfiguration; }
        }

        internal IEnumerable<Func<Type, bool>> Predicates
        {
            get { return _predicates; }
        }

        /// <summary>
        /// Filters the entity types that this convention applies to based on a
        /// predicate.
        /// </summary>
        /// <param name="predicate"> A function to test each entity type for a condition. </param>
        /// <returns>
        /// An <see cref="TypeConventionConfiguration{T}" /> instance so that multiple calls can be chained.
        /// </returns>
        public TypeConventionConfiguration<T> Where(Func<Type, bool> predicate)
        {
            Check.NotNull(predicate, "predicate");

            return new TypeConventionConfiguration<T>(
                _conventionsConfiguration,
                _predicates.Append(predicate));
        }

        /// <summary>
        /// Filters the entity types that this convention applies to based on a predicate
        /// while capturing a value to use later during configuration.
        /// </summary>
        /// <typeparam name="TValue"> Type of the captured value. </typeparam>
        /// <param name="capturingPredicate">
        /// A function to capture a value for each entity type. If the value is null, the
        /// entity type will be filtered out.
        /// </param>
        /// <returns>
        /// An <see cref="TypeConventionWithHavingConfiguration{T,TValue}" /> instance so that multiple calls can be chained.
        /// </returns>
        public TypeConventionWithHavingConfiguration<T, TValue> Having<TValue>(Func<Type, TValue> capturingPredicate)
            where TValue : class
        {
            Check.NotNull(capturingPredicate, "capturingPredicate");

            return new TypeConventionWithHavingConfiguration<T, TValue>(
                _conventionsConfiguration,
                _predicates,
                capturingPredicate);
        }

        /// <summary>
        /// Allows configuration of the entity types that this convention applies to.
        /// </summary>
        /// <param name="entityConfigurationAction">
        /// An action that performs configuration against a
        /// <see
        ///     cref="ConventionTypeConfiguration{T}" />
        /// .
        /// </param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public void Configure(Action<ConventionTypeConfiguration<T>> entityConfigurationAction)
        {
            Check.NotNull(entityConfigurationAction, "entityConfigurationAction");

            _conventionsConfiguration.Add(new TypeConvention<T>(_predicates, entityConfigurationAction));
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Gets the <see cref="Type" /> of the current instance.
        /// </summary>
        /// <returns>The exact runtime type of the current instance.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
