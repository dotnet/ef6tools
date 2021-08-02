// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Collections.Generic;
    using System.Data.Entity.ModelConfiguration.Configuration;
    using System.Data.Entity.ModelConfiguration.Configuration.Types;
    using System.Data.Entity.Utilities;

    internal class TypeConventionWithHaving<T> : TypeConventionWithHavingBase<T>
        where T : class
    {
        private readonly Action<ConventionTypeConfiguration, T> _entityConfigurationAction;

        public TypeConventionWithHaving(
            IEnumerable<Func<Type, bool>> predicates,
            Func<Type, T> capturingPredicate,
            Action<ConventionTypeConfiguration, T> entityConfigurationAction)
            : base(predicates, capturingPredicate)
        {
            DebugCheck.NotNull(predicates);
            DebugCheck.NotNull(capturingPredicate);
            DebugCheck.NotNull(entityConfigurationAction);

            _entityConfigurationAction = entityConfigurationAction;
        }

        internal Action<ConventionTypeConfiguration, T> EntityConfigurationAction
        {
            get { return _entityConfigurationAction; }
        }

        protected override void InvokeAction(
            Type memberInfo, ModelConfiguration modelConfiguration, T value)
        {
            DebugCheck.NotNull(memberInfo);
            DebugCheck.NotNull(modelConfiguration);
            DebugCheck.NotNull(value);

            _entityConfigurationAction(new ConventionTypeConfiguration(memberInfo, modelConfiguration), value);
        }

        protected override void InvokeAction(
            Type memberInfo, Func<EntityTypeConfiguration> configuration, ModelConfiguration modelConfiguration, T value)
        {
            DebugCheck.NotNull(memberInfo);
            DebugCheck.NotNull(configuration);
            DebugCheck.NotNull(modelConfiguration);
            DebugCheck.NotNull(value);

            _entityConfigurationAction(new ConventionTypeConfiguration(memberInfo, configuration, modelConfiguration), value);
        }

        protected override void InvokeAction(
            Type memberInfo, Func<ComplexTypeConfiguration> configuration, ModelConfiguration modelConfiguration, T value)
        {
            DebugCheck.NotNull(memberInfo);
            DebugCheck.NotNull(configuration);
            DebugCheck.NotNull(value);

            _entityConfigurationAction(new ConventionTypeConfiguration(memberInfo, configuration, modelConfiguration), value);
        }
    }
}
