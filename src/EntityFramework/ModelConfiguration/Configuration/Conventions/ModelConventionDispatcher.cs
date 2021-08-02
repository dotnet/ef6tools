// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;

    public partial class ConventionsConfiguration
    {
        private class ModelConventionDispatcher : EdmModelVisitor
        {
            private readonly IConvention _convention;
            private readonly DbModel _model;
            private readonly DataSpace _dataSpace;

            public ModelConventionDispatcher(IConvention convention, DbModel model, DataSpace dataSpace)
            {
                Check.NotNull(convention, "convention");
                Check.NotNull(model, "model");
                Debug.Assert(dataSpace == DataSpace.CSpace || dataSpace == DataSpace.SSpace);

                _convention = convention;
                _model = model;
                _dataSpace = dataSpace;
            }

            public void Dispatch()
            {
                VisitEdmModel(
                    _dataSpace == DataSpace.CSpace 
                        ? _model.ConceptualModel
                        : _model.StoreModel);
            }

            private void Dispatch<T>(T item)
                where T : MetadataItem
            {
                if (_dataSpace == DataSpace.CSpace)
                {
                    var convention = _convention as IConceptualModelConvention<T>;
                    if (convention != null)
                    {
                        convention.Apply(item, _model);
                    }                    
                }
                else
                {
                    var convention = _convention as IStoreModelConvention<T>;
                    if (convention != null)
                    {
                        convention.Apply(item, _model);
                    }
                }
            }

            protected internal override void VisitEdmModel(EdmModel item)
            {
                Dispatch(item);

                base.VisitEdmModel(item);
            }

            protected override void VisitEdmNavigationProperty(NavigationProperty item)
            {
                Dispatch(item);

                base.VisitEdmNavigationProperty(item);
            }

            protected override void VisitEdmAssociationConstraint(ReferentialConstraint item)
            {
                Dispatch(item);

                if (item != null)
                {
                    VisitMetadataItem(item);
                }
            }

            protected override void VisitEdmAssociationEnd(RelationshipEndMember item)
            {
                Dispatch(item);

                base.VisitEdmAssociationEnd(item);
            }

            protected internal override void VisitEdmProperty(EdmProperty item)
            {
                Dispatch(item);

                base.VisitEdmProperty(item);
            }

            protected internal override void VisitMetadataItem(MetadataItem item)
            {
                Dispatch(item);

                base.VisitMetadataItem(item);
            }

            protected override void VisitEdmEntityContainer(EntityContainer item)
            {
                Dispatch(item);

                base.VisitEdmEntityContainer(item);
            }

            protected internal override void VisitEdmEntitySet(EntitySet item)
            {
                Dispatch(item);

                base.VisitEdmEntitySet(item);
            }

            protected override void VisitEdmAssociationSet(AssociationSet item)
            {
                Dispatch(item);

                base.VisitEdmAssociationSet(item);
            }

            protected override void VisitEdmAssociationSetEnd(EntitySet item)
            {
                Dispatch(item);

                base.VisitEdmAssociationSetEnd(item);
            }

            protected override void VisitComplexType(ComplexType item)
            {
                Dispatch(item);

                base.VisitComplexType(item);
            }

            protected internal override void VisitEdmEntityType(EntityType item)
            {
                Dispatch(item);

                VisitMetadataItem(item);

                if (item != null)
                {
                    VisitDeclaredProperties(item, item.DeclaredProperties);
                    VisitDeclaredNavigationProperties(item, item.DeclaredNavigationProperties);
                }
            }

            protected internal override void VisitEdmAssociationType(AssociationType item)
            {
                Dispatch(item);

                base.VisitEdmAssociationType(item);
            }
        }
    }
}
