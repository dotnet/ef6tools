﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    internal class ModificationStoredProcedureConfiguration
    {
        private sealed class ParameterKey
        {
            private readonly PropertyPath _propertyPath;
            private readonly bool _rightKey;

            public ParameterKey(PropertyPath propertyPath, bool rightKey)
            {
                DebugCheck.NotNull(propertyPath);

                _propertyPath = propertyPath;
                _rightKey = rightKey;
            }

            public PropertyPath PropertyPath
            {
                get { return _propertyPath; }
            }

            public bool IsRightKey
            {
                get { return _rightKey; }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                var other = (ParameterKey)obj;

                return (_propertyPath.Equals(other._propertyPath)
                        && _rightKey.Equals(other._rightKey));
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_propertyPath.GetHashCode() * 397) ^ _rightKey.GetHashCode();
                }
            }
        }

        private readonly Dictionary<ParameterKey, Tuple<string, string>> _parameterNames
            = new Dictionary<ParameterKey, Tuple<string, string>>();

        private readonly Dictionary<PropertyInfo, string> _resultBindings
            = new Dictionary<PropertyInfo, string>();

        private string _name;
        private string _schema;
        private string _rowsAffectedParameter;

        private List<FunctionParameter> _configuredParameters;

        public ModificationStoredProcedureConfiguration()
        {
        }

        private ModificationStoredProcedureConfiguration(ModificationStoredProcedureConfiguration source)
        {
            DebugCheck.NotNull(source);

            _name = source._name;
            _schema = source._schema;
            _rowsAffectedParameter = source._rowsAffectedParameter;

            source._parameterNames.Each(
                c => _parameterNames.Add(c.Key, Tuple.Create(c.Value.Item1, c.Value.Item2)));

            source._resultBindings.Each(
                r => _resultBindings.Add(r.Key, r.Value));
        }

        public virtual ModificationStoredProcedureConfiguration Clone()
        {
            return new ModificationStoredProcedureConfiguration(this);
        }

        public void HasName(string name)
        {
            DebugCheck.NotEmpty(name);

            var databaseName = DatabaseName.Parse(name);

            _name = databaseName.Name;
            _schema = databaseName.Schema;
        }

        public void HasName(string name, string schema)
        {
            DebugCheck.NotEmpty(name);
            DebugCheck.NotEmpty(schema);

            _name = name;
            _schema = schema;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Schema
        {
            get { return _schema; }
        }

        public void RowsAffectedParameter(string name)
        {
            DebugCheck.NotEmpty(name);

            _rowsAffectedParameter = name;
        }

        public string RowsAffectedParameterName
        {
            get { return _rowsAffectedParameter; }
        }

        public IEnumerable<Tuple<string, string>> ParameterNames
        {
            get { return _parameterNames.Values; }
        }

        public void ClearParameterNames()
        {
            _parameterNames.Clear();
        }

        public Dictionary<PropertyInfo, string> ResultBindings
        {
            get { return _resultBindings; }
        }

        public void Parameter(
            PropertyPath propertyPath,
            string parameterName,
            string originalValueParameterName = null,
            bool rightKey = false)
        {
            DebugCheck.NotNull(propertyPath);
            DebugCheck.NotEmpty(parameterName);

            _parameterNames[new ParameterKey(propertyPath, rightKey)]
                = Tuple.Create(parameterName, originalValueParameterName);
        }

        public void Result(PropertyPath propertyPath, string columnName)
        {
            DebugCheck.NotNull(propertyPath);
            DebugCheck.NotEmpty(columnName);

            _resultBindings[propertyPath.Single()] = columnName;
        }

        public virtual void Configure(
            ModificationFunctionMapping modificationStoredProcedureMapping, DbProviderManifest providerManifest)
        {
            DebugCheck.NotNull(modificationStoredProcedureMapping);
            DebugCheck.NotNull(providerManifest);

            _configuredParameters = new List<FunctionParameter>();

            ConfigureName(modificationStoredProcedureMapping);
            ConfigureSchema(modificationStoredProcedureMapping);
            ConfigureRowsAffectedParameter(modificationStoredProcedureMapping, providerManifest);
            ConfigureParameters(modificationStoredProcedureMapping);
            ConfigureResultBindings(modificationStoredProcedureMapping);
        }

        private void ConfigureName(ModificationFunctionMapping modificationStoredProcedureMapping)
        {
            DebugCheck.NotNull(modificationStoredProcedureMapping);

            if (!string.IsNullOrWhiteSpace(_name))
            {
                modificationStoredProcedureMapping.Function.StoreFunctionNameAttribute = _name;
            }
        }

        private void ConfigureSchema(ModificationFunctionMapping modificationStoredProcedureMapping)
        {
            DebugCheck.NotNull(modificationStoredProcedureMapping);

            if (!string.IsNullOrWhiteSpace(_schema))
            {
                modificationStoredProcedureMapping.Function.Schema = _schema;
            }
        }

        private void ConfigureRowsAffectedParameter(
            ModificationFunctionMapping modificationStoredProcedureMapping, DbProviderManifest providerManifest)
        {
            DebugCheck.NotNull(modificationStoredProcedureMapping);
            DebugCheck.NotNull(providerManifest);

            if (!string.IsNullOrWhiteSpace(_rowsAffectedParameter))
            {
                if (modificationStoredProcedureMapping.RowsAffectedParameter == null)
                {
                    var rowsAffectedParameter
                        = new FunctionParameter(
                            "_RowsAffected_",
                            providerManifest.GetStoreType(
                                TypeUsage.CreateDefaultTypeUsage(
                                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))),
                            ParameterMode.Out);

                    modificationStoredProcedureMapping.Function.AddParameter(rowsAffectedParameter);
                    modificationStoredProcedureMapping.RowsAffectedParameter = rowsAffectedParameter;
                }

                modificationStoredProcedureMapping.RowsAffectedParameter.Name = _rowsAffectedParameter;

                _configuredParameters.Add(modificationStoredProcedureMapping.RowsAffectedParameter);
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void ConfigureParameters(ModificationFunctionMapping modificationStoredProcedureMapping)
        {
            foreach (var keyValue in _parameterNames)
            {
                var propertyPath = keyValue.Key.PropertyPath;
                var parameterName = keyValue.Value.Item1;
                var originalValueParameterName = keyValue.Value.Item2;

                var parameterBindings
                    = modificationStoredProcedureMapping
                        .ParameterBindings
                        .Where(
                            pb => // First, try and match scalar/complex/many-to-many binding 
                            (((pb.MemberPath.AssociationSetEnd == null)
                              || pb.MemberPath.AssociationSetEnd.ParentAssociationSet.ElementType.IsManyToMany())
                             && propertyPath.Equals(
                                 new PropertyPath(
                                    pb.MemberPath.Members.OfType<EdmProperty>().Select(m => m.GetClrPropertyInfo()))))
                            ||
                            // Otherwise, try and match IA FK bindings 
                            ((propertyPath.Count == 2)
                             && (pb.MemberPath.AssociationSetEnd != null)
                             && pb.MemberPath.Members.First().GetClrPropertyInfo().IsSameAs(propertyPath.Last())
                             && pb.MemberPath.AssociationSetEnd.ParentAssociationSet.AssociationSetEnds
                                    .Select(ae => ae.CorrespondingAssociationEndMember.GetClrPropertyInfo())
                                    .Where(pi => pi != null)
                                    .Any(pi => pi.IsSameAs(propertyPath.First()))))
                        .ToList();

                if (parameterBindings.Count == 1)
                {
                    var parameterBinding = parameterBindings.Single();

                    if (!string.IsNullOrWhiteSpace(originalValueParameterName))
                    {
                        if (parameterBinding.IsCurrent)
                        {
                            throw Error.ModificationFunctionParameterNotFoundOriginal(
                                propertyPath,
                                modificationStoredProcedureMapping.Function.FunctionName);
                        }
                    }

                    parameterBinding.Parameter.Name = parameterName;

                    _configuredParameters.Add(parameterBinding.Parameter);
                }
                else if (parameterBindings.Count == 2)
                {
                    var parameterBinding
                        = ((parameterBindings
                                .Select(pb => pb.IsCurrent)
                                .Distinct()
                                .Count() == 1) // same value for both
                           && parameterBindings
                                  .All(pb => pb.MemberPath.AssociationSetEnd != null))
                              ? !keyValue.Key.IsRightKey
                                    ? parameterBindings.First()
                                    : parameterBindings.Last()
                              : parameterBindings.Single(pb => pb.IsCurrent);

                    parameterBinding.Parameter.Name = parameterName;

                    _configuredParameters.Add(parameterBinding.Parameter);

                    if (!string.IsNullOrWhiteSpace(originalValueParameterName))
                    {
                        parameterBinding = parameterBindings.Single(pb => !pb.IsCurrent);

                        parameterBinding.Parameter.Name = originalValueParameterName;

                        _configuredParameters.Add(parameterBinding.Parameter);
                    }
                }
                else
                {
                    throw Error.ModificationFunctionParameterNotFound(
                        propertyPath,
                        modificationStoredProcedureMapping.Function.FunctionName);
                }
            }

            var unconfiguredParameters
                = modificationStoredProcedureMapping
                    .Function
                    .Parameters
                    .Except(_configuredParameters);

            foreach (var parameter in unconfiguredParameters)
            {
                parameter.Name
                    = modificationStoredProcedureMapping
                        .Function
                        .Parameters
                        .Except(new[] { parameter })
                        .UniquifyName(parameter.Name);
            }
        }

        private void ConfigureResultBindings(ModificationFunctionMapping modificationStoredProcedureMapping)
        {
            DebugCheck.NotNull(modificationStoredProcedureMapping);

            foreach (var keyValue in _resultBindings)
            {
                var propertyInfo = keyValue.Key;
                var columnName = keyValue.Value;

                var resultBinding
                    = (modificationStoredProcedureMapping
                           .ResultBindings ?? Enumerable.Empty<ModificationFunctionResultBinding>())
                        .SingleOrDefault(rb => propertyInfo.IsSameAs(rb.Property.GetClrPropertyInfo()));

                if (resultBinding == null)
                {
                    throw Error.ResultBindingNotFound(
                        propertyInfo.Name,
                        modificationStoredProcedureMapping.Function.FunctionName);
                }

                resultBinding.ColumnName = columnName;
            }
        }

        public bool IsCompatibleWith(ModificationStoredProcedureConfiguration other)
        {
            DebugCheck.NotNull(other);

            if ((_name != null)
                && (other._name != null)
                && !string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if ((_schema != null)
                && (other._schema != null)
                && !string.Equals(_schema, other._schema, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !_parameterNames
                        .Join(
                            other._parameterNames,
                            kv1 => kv1.Key,
                            kv2 => kv2.Key,
                            (kv1, kv2) => !Equals(kv1.Value, kv2.Value))
                        .Any(j => j);
        }

        public void Merge(ModificationStoredProcedureConfiguration modificationStoredProcedureConfiguration, bool allowOverride)
        {
            DebugCheck.NotNull(modificationStoredProcedureConfiguration);

            if (allowOverride || string.IsNullOrWhiteSpace(_name))
            {
                _name = modificationStoredProcedureConfiguration.Name ?? _name;
            }

            if (allowOverride || string.IsNullOrWhiteSpace(_schema))
            {
                _schema = modificationStoredProcedureConfiguration.Schema ?? _schema;
            }

            if (allowOverride || string.IsNullOrWhiteSpace(_rowsAffectedParameter))
            {
                _rowsAffectedParameter
                    = modificationStoredProcedureConfiguration.RowsAffectedParameterName ?? _rowsAffectedParameter;
            }

            foreach (var parameterName in modificationStoredProcedureConfiguration._parameterNames
                .Where(parameterName => allowOverride || !_parameterNames.ContainsKey(parameterName.Key)))
            {
                _parameterNames[parameterName.Key] = parameterName.Value;
            }

            foreach (var resultBinding in modificationStoredProcedureConfiguration.ResultBindings
                .Where(resultBinding => allowOverride || !_resultBindings.ContainsKey(resultBinding.Key)))
            {
                _resultBindings[resultBinding.Key] = resultBinding.Value;
            }
        }
    }
}
