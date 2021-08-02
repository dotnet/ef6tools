// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using System.Linq.Expressions;

    internal class DynamicTypeDescriptionProvider<T> : TypeDescriptionProvider
    {
        private readonly DynamicTypeDescriptionConfiguration<T> _configuration;

        public DynamicTypeDescriptionProvider(TypeDescriptionProvider parent, DynamicTypeDescriptionConfiguration<T> configuration)
            : base(parent)
        {
            _configuration = configuration;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            var defaultDescriptor = base.GetTypeDescriptor(objectType, instance);

            return new DynamicTypeDescriptor<T>(defaultDescriptor, _configuration);
        }
    }

    internal class DynamicTypeDescriptor<T> : CustomTypeDescriptor
    {
        private readonly DynamicTypeDescriptionConfiguration<T> _configuration;

        public DynamicTypeDescriptor(ICustomTypeDescriptor parent, DynamicTypeDescriptionConfiguration<T> configuration)
            : base(parent)
        {
            _configuration = configuration;
        }

        public override AttributeCollection GetAttributes()
        {
            var newAttributes = new List<Attribute>(_configuration.TypeAttributes);

            if (!_configuration.IgnoreBase)
            {
                newAttributes.AddRange(base.GetAttributes().Cast<Attribute>());
            }

            return new AttributeCollection(newAttributes.ToArray());
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return AddAttributes(base.GetProperties());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return AddAttributes(base.GetProperties(attributes));
        }

        private PropertyDescriptorCollection AddAttributes(PropertyDescriptorCollection originalProperties)
        {
            var newProperties = new PropertyDescriptor[originalProperties.Count];
            for (var i = 0; i < originalProperties.Count; i++)
            {
                Attribute[] attributes;
                if (_configuration.PropertyAttributes.TryGetValue(originalProperties[i].Name, out attributes))
                {
                    var newAttributes = new List<Attribute>(attributes);

                    if (!_configuration.IgnoreBase)
                    {
                        newAttributes.AddRange(originalProperties[i].Attributes.Cast<Attribute>());
                    }
                    newProperties[i] = TypeDescriptor.CreateProperty(
                        originalProperties[i].ComponentType, originalProperties[i], newAttributes.ToArray());
                }
                else
                {
                    newProperties[i] = originalProperties[i];
                }
            }
            return new PropertyDescriptorCollection(newProperties);
        }
    }

    /// <summary>
    /// Allows to add or remove attributes on a type and its properties at runtime
    /// </summary>
    public class DynamicTypeDescriptionConfiguration<T> : IDisposable
    {
        private readonly DynamicTypeDescriptionProvider<T> _dynamicTypeDescriptionProvider;
        private readonly Dictionary<string, Attribute[]> _propertyAttributes;

        public DynamicTypeDescriptionConfiguration()
        {
            _propertyAttributes = new Dictionary<string, Attribute[]>();
            TypeAttributes = new Attribute[0];

            var provider = TypeDescriptor.GetProvider(typeof(T));
            if (!(provider is DynamicTypeDescriptionProvider<T>))
            {
                _dynamicTypeDescriptionProvider = new DynamicTypeDescriptionProvider<T>(provider, this);
                TypeDescriptor.AddProvider(_dynamicTypeDescriptionProvider, typeof(T));
            }
        }

        public Attribute[] TypeAttributes { get; set; }

        public Dictionary<string, Attribute[]> PropertyAttributes
        {
            get { return _propertyAttributes; }
        }

        /// <summary>
        /// If set to true will not return attributes defined on the type at compile time
        /// </summary>
        public bool IgnoreBase { get; set; }

        public void Dispose()
        {
            DbConfiguration.DependencyResolver.GetService<AttributeProvider>().ClearCache();
            TypeDescriptor.RemoveProvider(_dynamicTypeDescriptionProvider, typeof(T));
        }

        public void SetPropertyAttributes<TProperty>(Expression<Func<T, TProperty>> property, params Attribute[] attributes)
        {
            DebugCheck.NotNull(property);

            PropertyAttributes[property.GetSimplePropertyAccess().Single().Name] = attributes;
        }

        public void SetPropertyAttributes(string propertyName, params Attribute[] attributes)
        {
            DebugCheck.NotEmpty(propertyName);

            PropertyAttributes[propertyName] = attributes;
        }
    }
}