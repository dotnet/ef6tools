﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.Data.Entity.Design.Model;
    using Microsoft.Data.Entity.Design.Model.Entity;

    internal class MaxLengthConverter : StringConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var v = value as StringOrPrimitive<UInt32>;
                if (v != null)
                {
                    return StringOrPrimitiveConverter<UInt32>.StringConverter(v);
                }
            }
            return null;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = value as string;
            if (stringValue != null)
            {
                try
                {
                    return DefaultableValueMaxLength.MaxLengthConverter.ValueConverter(stringValue);
                }
                catch (ConversionException)
                {
                    // if the Converter throws a ConversionException then user has put in a incorrect
                    // value e.g. text into an uint field - so throw a new exception with a better
                    // error message
                    var attributeName = context.PropertyDescriptor.DisplayName;
                    var message = string.Format(
                        CultureInfo.CurrentCulture, Tools.XmlDesignerBase.Resources.ConverterIncorrectValueForAttribute, stringValue, attributeName);
                    throw new ConversionException(message);
                }
            }
            return null;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StringOrPrimitive<UInt32>[] standardValues =
                {
                    Property.MaxLengthMaxValueObject,
                    DefaultableValueUIntOrNone.NoneValue,
                };
            return new StandardValuesCollection(standardValues);
        }
    }
}
