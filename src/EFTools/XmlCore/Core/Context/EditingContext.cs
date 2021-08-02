// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Core.Context
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     The EditingContext class contains contextual state about a designer.  This includes permanent
    ///     state such as list of services running in the designer.
    ///     It also includes transient state consisting of context items.  Examples of transient
    ///     context item state include the set of currently selected objects as well as the editing tool
    ///     being used to manipulate objects on the design surface.
    ///     The editing context is designed to be a concrete class for ease of use.  It does have a protected
    ///     API that can be used to replace its implementation.
    /// </summary>
    public class EditingContext : IDisposable
    {
        private ContextItemCollection _contextItems;
        private ServiceCollection _services;

        /// <summary>
        ///     The Disposing event gets fired just before the context gets disposed.
        /// </summary>
        public event EventHandler Disposing;

        /// <summary>
        ///     Finalizer that implements the IDisposable pattern.
        /// </summary>
        ~EditingContext()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Returns the local collection of context items offered by this editing context.
        /// </summary>
        /// <value></value>
        public ContextItemCollection Items
        {
            get
            {
                if (_contextItems == null)
                {
                    _contextItems = CreateContextItemCollection();
                    if (_contextItems == null)
                    {
                        throw new InvalidOperationException();
                    }
                }

                return _contextItems;
            }
        }

        /// <summary>
        ///     Returns the service collection for this editing context.
        /// </summary>
        /// <value></value>
        public ServiceCollection Services
        {
            get
            {
                if (_services == null)
                {
                    _services = CreateServiceCollection();
                    if (_services == null)
                    {
                        throw new InvalidOperationException();
                    }
                }

                return _services;
            }
        }

        /// <summary>
        ///     Creates an instance of the context item collection to be returned from
        ///     the ContextItems property.  The default implementation creates a
        ///     ContextItemCollection that supports delayed activation of design editor
        ///     collections through the declaration of a SubscribeContext attribute on
        ///     the design editor manager.
        /// </summary>
        /// <returns>Returns an implementation of the ContextItemCollection class.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected virtual ContextItemCollection CreateContextItemCollection()
        {
            return new DefaultContextItemCollection(this);
        }

        /// <summary>
        ///     Creates an instance of the service collection to be returned from the
        ///     Services property. The default implementation creates a ServiceCollection
        ///     that supports delayed activation of design editor managers through the
        ///     declaration of a SubscribeService attribute on the design editor manager.
        /// </summary>
        /// <returns>Returns an implemetation of the ServiceCollection class.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected virtual ServiceCollection CreateServiceCollection()
        {
            return new DefaultServiceCollection();
        }

        /// <summary>
        ///     Disposes this editing context.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disposes this editing context.
        /// </summary>
        /// <param name="disposing">True if this object is being disposed, or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Let any interested parties know the context is being disposed
                if (Disposing != null)
                {
                    Disposing(this, EventArgs.Empty);
                }

                var d = _services as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }

                d = _contextItems as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
        }

        /// <summary>
        ///     This is the default context item collection for our editing context.
        /// </summary>
        private sealed class DefaultContextItemCollection : ContextItemCollection, IDisposable
        {
            private readonly EditingContext _context;
            private DefaultContextLayer _currentLayer;
            private Dictionary<Type, SubscribeContextCallback> _subscriptions;

            internal DefaultContextItemCollection(EditingContext context)
            {
                _context = context;
                _currentLayer = new DefaultContextLayer(this, null);
            }

            public void Dispose()
            {
                if (_currentLayer != null)
                {
                    _currentLayer.Dispose();
                }
            }

            /// <summary>
            ///     This changes a context item to the given value.  It is illegal to pass
            ///     null here.  If you want to set a context item to its empty value create
            ///     an instance of the item using a default constructor.
            /// </summary>
            /// <param name="value"></param>
            public override void SetValue(ContextItem value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // The rule for change is that we store the new value,
                // raise a change on the item, and then raise a change
                // to everyone else.  If changing the item fails, we recover
                // the previous item.
                ContextItem existing = GetValueNull(value.ItemType);

                if (existing != null
                    && !existing.CanBeReplaced)
                {
                    throw new InvalidOperationException();
                }

                var success = false;

                try
                {
                    _currentLayer.Items[value.ItemType] = value;
                    NotifyItemChanged(_context, value, existing);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        var d = existing as IDisposable;
                        if (d != null)
                        {
                            d.Dispose();
                        }
                        OnItemChanged(value);
                    }
                    else
                    {
                        // The item threw during its transition to 
                        // becoming active.  Put the old one back.
                        // We must put the old one back by re-activating
                        // it.  This could throw a second time, so we
                        // cover this case by removing the value first.
                        // Should it throw again, we won't recurse because
                        // the existing raw value would be null.

                        _currentLayer.Items.Remove(value.ItemType);
                        if (existing != null)
                        {
                            SetValue(existing);
                        }
                    }
                }
            }

            /// <summary>
            ///     Returns true if the item collection contains an item of the given type.
            ///     This only looks in the current layer.
            /// </summary>
            /// <param name="itemType"></param>
            /// <returns></returns>
            public override bool Contains(Type itemType)
            {
                if (itemType == null)
                {
                    throw new ArgumentNullException("itemType");
                }
                if (!typeof(ContextItem).IsAssignableFrom(itemType))
                {
                    throw new ArgumentException("Incorrect Argument Type", "itemType");
                    /*
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, 
                        Resources.Error_ArgIncorrectType, 
                        "itemType", typeof(ContextItem).FullName));
                    */
                }

                return _currentLayer.Items.ContainsKey(itemType);
            }

            /// <summary>
            ///     This helper function returns the childLayer for the layer that is passed in.
            ///     This function is used in the OnLayerRemoved to link the layers when
            ///     a layer (in the middle) is removed.
            /// </summary>
            /// <param name="layer"></param>
            /// <returns></returns>
            private DefaultContextLayer FindChildLayer(DefaultContextLayer layer)
            {
                var startLayer = _currentLayer;
                while (startLayer != null
                       && startLayer.ParentLayer != layer)
                {
                    startLayer = startLayer.ParentLayer;
                }
                return startLayer;
            }

            /// <summary>
            ///     Returns an instance of the requested item type.  If there is no context
            ///     item with the given type, an empty item will be created.
            /// </summary>
            /// <param name="itemType"></param>
            /// <returns></returns>
            public override ContextItem GetValue(Type itemType)
            {
                var item = GetValueNull(itemType);

                if (item == null)
                {
                    item = (ContextItem)Activator.CreateInstance(itemType);

                    // Verify that the resulting item has the correct item type
                    // If it doesn't, it means that the user provided a derived
                    // item type
                    if (item.ItemType != itemType)
                    {
                        throw new ArgumentException("Error in DerivedContextItem", itemType.FullName);
                        /*
                        throw new ArgumentException(string.Format(
                            CultureInfo.CurrentCulture, 
                            Resources.Error_DerivedContextItem,
                            itemType.FullName,
                            item.ItemType.FullName));
                        */
                    }

                    // Now push the item in the context so we have
                    // a consistent reference
                    SetValue(item);
                }

                return item;
            }

            /// <summary>
            ///     Similar to GetValue, but returns NULL if the item isn't found instead of
            ///     creating an empty item.
            /// </summary>
            /// <param name="itemType"></param>
            /// <returns></returns>
            private ContextItem GetValueNull(Type itemType)
            {
                if (itemType == null)
                {
                    throw new ArgumentNullException("itemType");
                }
                if (!typeof(ContextItem).IsAssignableFrom(itemType))
                {
                    throw new ArgumentException("Incorrect Type", "itemType");
                    /*                
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, 
                        Resources.Error_ArgIncorrectType, 
                        "itemType", typeof(ContextItem).FullName));
                    */
                }

                ContextItem item = null;
                var layer = _currentLayer;
                while (layer != null
                       && !layer.Items.TryGetValue(itemType, out item))
                {
                    layer = layer.ParentLayer;
                }

                return item;
            }

            /// <summary>
            ///     Creates a new editing context layer.  Editing context layers can be used to
            ///     create editing modes.  For example, you may create a layer before starting a
            ///     drag operation on the designer.  Any new context items you add to the layer
            ///     hide context items underneath it.  When the layer is removed, all context
            ///     items under the layer are re-surfaced.  This allows you to create a layer
            ///     and set overrides for context items during operations such as drag and drop.
            /// </summary>
            /// <returns></returns>
            public override ContextLayer CreateLayer()
            {
                _currentLayer = new DefaultContextLayer(this, _currentLayer);
                return _currentLayer;
            }

            /// <summary>
            ///     Enumerates the context items in the editing context.  This enumeration
            ///     includes prior layers unless the enumerator hits an isolated layer.
            ///     Enumeration is typically not useful in most scenarios but it is provided so
            ///     that developers can search in the context and learn what is placed in it.
            /// </summary>
            /// <returns></returns>
            public override IEnumerator<ContextItem> GetEnumerator()
            {
                return _currentLayer.Items.Values.GetEnumerator();
            }

            /// <summary>
            ///     Called when an item changes value.  This happens in one of two ways:
            ///     either the user has called Change, or the user has removed a layer.
            /// </summary>
            /// <param name="item"></param>
            private void OnItemChanged(ContextItem item)
            {
                SubscribeContextCallback callback;

                Debug.Assert(item != null, "You cannot pass a null item here.");

                if (_subscriptions != null
                    && _subscriptions.TryGetValue(item.ItemType, out callback))
                {
                    callback(item);
                }
            }

            /// <summary>
            ///     Called when the user removes a layer.
            /// </summary>
            /// <param name="layer"></param>
            private void OnLayerRemoved(DefaultContextLayer layer)
            {
                if (_currentLayer == layer)
                {
                    _currentLayer = layer.ParentLayer;
                }
                else
                {
                    var childLayer = FindChildLayer(layer);
                    if (childLayer != null)
                    {
                        childLayer.ParentLayer = layer.ParentLayer;
                    }
                }
            }

            /// <summary>
            ///     Adds an event callback that will be invoked with a context item of the given item type changes.
            /// </summary>
            /// <param name="contextItemType"></param>
            /// <param name="callback"></param>
            public override void Subscribe(Type contextItemType, SubscribeContextCallback callback)
            {
                if (contextItemType == null)
                {
                    throw new ArgumentNullException("contextItemType");
                }
                if (callback == null)
                {
                    throw new ArgumentNullException("callback");
                }
                if (!typeof(ContextItem).IsAssignableFrom(contextItemType))
                {
                    throw new ArgumentException("Argument Incorrect Type", "contextItemType");
                    /*
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture,
                        Resources.Error_ArgIncorrectType,
                        "contextItemType", typeof(ContextItem).FullName));
                    */
                }

                if (_subscriptions == null)
                {
                    _subscriptions = new Dictionary<Type, SubscribeContextCallback>();
                }

                SubscribeContextCallback existing = null;

                _subscriptions.TryGetValue(contextItemType, out existing);

                existing = (SubscribeContextCallback)Delegate.Combine(existing, callback);
                _subscriptions[contextItemType] = existing;

                // If the context is already present, invoke the callback.
                var item = GetValueNull(contextItemType);

                if (item != null)
                {
                    callback(item);
                }
            }

            /// <summary>
            ///     Removes a subscription.
            /// </summary>
            public override void Unsubscribe(Type contextItemType, SubscribeContextCallback callback)
            {
                if (contextItemType == null)
                {
                    throw new ArgumentNullException("contextItemType");
                }
                if (callback == null)
                {
                    throw new ArgumentNullException("callback");
                }
                if (!typeof(ContextItem).IsAssignableFrom(contextItemType))
                {
                    throw new ArgumentException("Argument incorrect type.", "contextItemType");
                    /*
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture,
                        Resources.Error_ArgIncorrectType,
                        "contextItemType", typeof(ContextItem).FullName));
                    */
                }
                if (_subscriptions != null)
                {
                    SubscribeContextCallback existing;
                    if (_subscriptions.TryGetValue(contextItemType, out existing))
                    {
                        existing = (SubscribeContextCallback)RemoveCallback(existing, callback);
                        if (existing == null)
                        {
                            _subscriptions.Remove(contextItemType);
                        }
                        else
                        {
                            _subscriptions[contextItemType] = existing;
                        }
                    }
                }
            }

            /// <summary>
            ///     This context layer contains our context items.
            /// </summary>
            private class DefaultContextLayer : ContextLayer
            {
                private readonly DefaultContextItemCollection _collection;
                private DefaultContextLayer _parentLayer;
                private Dictionary<Type, ContextItem> _items;

                internal DefaultContextLayer(DefaultContextItemCollection collection, DefaultContextLayer parentLayer)
                {
                    _collection = collection;
                    _parentLayer = parentLayer; // can be null
                }

                internal Dictionary<Type, ContextItem> Items
                {
                    get
                    {
                        if (_items == null)
                        {
                            _items = new Dictionary<Type, ContextItem>();
                        }
                        return _items;
                    }
                }

                internal DefaultContextLayer ParentLayer
                {
                    get { return _parentLayer; }
                    set { _parentLayer = value; }
                }

                public override void Remove(bool disposing = false)
                {
                    // Only remove the layer if we have a parent layer or if being disposed.
                    // Also, once we remove the layer make sure we don't
                    // try to remove it if someone else calls Remove.
                    if (_parentLayer != null || disposing)
                    {
                        foreach (var item in _items.Values)
                        {
                            var d = item as IDisposable;
                            if (d != null)
                            {
                                d.Dispose();
                            }
                        }
                        _items.Clear();
                        _collection.OnLayerRemoved(this);
                        _parentLayer = null;
                    }
                }
            }
        }

        /// <summary>
        ///     This is the default service collection for our editing context.
        /// </summary>
        private sealed class DefaultServiceCollection : ServiceCollection, IDisposable
        {
            private Dictionary<Type, object> _services;
            private Dictionary<Type, SubscribeServiceCallback> _subscriptions;
            private static readonly object _recursionSentinel = new object();

            /// <summary>
            ///     Returns true if the service collection contains a service of the given type.
            /// </summary>
            /// <param name="serviceType"></param>
            /// <returns></returns>
            public override bool Contains(Type serviceType)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }
                return (_services != null && _services.ContainsKey(serviceType));
            }

            /// <summary>
            ///     Retrieves the requested service.  This method returns null if the service could not be located.
            /// </summary>
            /// <param name="serviceType"></param>
            /// <returns></returns>
            public override object GetService(Type serviceType)
            {
                object service = null;

                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }

                if (_services != null
                    && _services.TryGetValue(serviceType, out service))
                {
                    // If this service is our recursion sentinel, it means that someone is recursing
                    // while resolving a service callback.  Throw to break out of the recursion
                    // cycle.
                    if (service == _recursionSentinel)
                    {
                        throw new InvalidOperationException("Recursion Error" + serviceType.FullName);
                        // throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_RecursionResolvingService, serviceType.FullName));
                    }

                    // See if this service is a callback.  If it is, invoke it and store
                    // the resulting service back in the dictionary.
                    var callback = service as PublishServiceCallback;
                    if (callback != null)
                    {
                        // Store a recursion sentinel in the dictionary so we can easily
                        // tell if someone is recursing
                        _services[serviceType] = _recursionSentinel;
                        try
                        {
                            service = callback(serviceType);
                            if (service == null)
                            {
                                throw new InvalidOperationException(
                                    "Null Service: " +
                                    callback.Method.DeclaringType.FullName + " " +
                                    serviceType.FullName);
                                /*                                    
                                throw new InvalidOperationException(
                                    string.Format(CultureInfo.CurrentCulture, 
                                    Resources.Error_NullService,
                                    callback.Method.DeclaringType.FullName,
                                    serviceType.FullName));
                                */
                            }

                            if (!serviceType.IsInstanceOfType(service))
                            {
                                throw new InvalidOperationException(
                                    "Incorrect Service Type: " +
                                    callback.Method.DeclaringType.FullName + " " +
                                    serviceType.FullName + " " +
                                    service.GetType().FullName);
                                /*
                                throw new InvalidOperationException(
                                    string.Format(CultureInfo.CurrentCulture, 
                                    Resources.Error_IncorrectServiceType,
                                    callback.Method.DeclaringType.FullName,
                                    serviceType.FullName,
                                    service.GetType().FullName));
                                */
                            }
                        }
                        finally
                        {
                            // Note, this puts the callback back in place if it threw.
                            _services[serviceType] = service;
                        }
                    }
                }

                // If the service is not found locally, do not walk up the parent chain.  
                // This was a major source of unreliability with the component model
                // design.  For a service to be accessible from the editing context, it
                // must be added.

                return service;
            }

            /// <summary>
            ///     Retrieves an enumerator that can be used to enumerate all of the services that this
            ///     service collection publishes.
            /// </summary>
            /// <returns></returns>
            public override IEnumerator<Type> GetEnumerator()
            {
                if (_services == null)
                {
                    _services = new Dictionary<Type, object>();
                }

                return _services.Keys.GetEnumerator();
            }

            /// <summary>
            ///     Calls back on the provided callback when someone has published the requested service.
            ///     If the service was already available, this method invokes the callback immediately.
            ///     A generic version of this method is provided for convience, and calls the non-generic
            ///     method with appropriate casts.
            /// </summary>
            /// <param name="serviceType"></param>
            /// <param name="callback"></param>
            public override void Subscribe(Type serviceType, SubscribeServiceCallback callback)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }
                if (callback == null)
                {
                    throw new ArgumentNullException("callback");
                }

                var service = GetService(serviceType);
                if (service != null)
                {
                    // If the service is already available, callback immediately
                    callback(serviceType, service);
                }
                else
                {
                    // Otherwise, store this for later
                    if (_subscriptions == null)
                    {
                        _subscriptions = new Dictionary<Type, SubscribeServiceCallback>();
                    }
                    SubscribeServiceCallback existing = null;
                    _subscriptions.TryGetValue(serviceType, out existing);
                    existing = (SubscribeServiceCallback)Delegate.Combine(existing, callback);
                    _subscriptions[serviceType] = existing;
                }
            }

            /// <summary>
            ///     Calls back on the provided callback when someone has published the requested service.
            ///     If the service was already available, this method invokes the callback immediately.
            ///     A generic version of this method is provided for convience, and calls the non-generic
            ///     method with appropriate casts.
            /// </summary>
            /// <param name="serviceType"></param>
            /// <param name="callback"></param>
            public override void Publish(Type serviceType, PublishServiceCallback callback)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }
                if (callback == null)
                {
                    throw new ArgumentNullException("callback");
                }

                Publish(serviceType, (object)callback);
            }

            /// <summary>
            ///     If you already have an instance to a service, you can publish it here.
            /// </summary>
            /// <param name="serviceType"></param>
            /// <param name="serviceInstance"></param>
            public override void Publish(Type serviceType, object serviceInstance)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }
                if (serviceInstance == null)
                {
                    throw new ArgumentNullException("serviceInstance");
                }

                if (!(serviceInstance is PublishServiceCallback)
                    && !serviceType.IsInstanceOfType(serviceInstance))
                {
                    throw new ArgumentException(
                        "Incorrect Service Type" +
                        typeof(ServiceCollection).Name + " " +
                        serviceType.FullName + " " +
                        serviceInstance.GetType().FullName);
                    /*
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture,
                        Resources.Error_IncorrectServiceType,
                        typeof(ServiceCollection).Name,
                        serviceType.FullName,
                        serviceInstance.GetType().FullName));
                    */
                }

                if (_services == null)
                {
                    _services = new Dictionary<Type, object>();
                }

                try
                {
                    _services.Add(serviceType, serviceInstance);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException("Duplicate Service: " + serviceType.FullName, e);
                    //throw new ArgumentException(string.Format(
                    //CultureInfo.CurrentCulture, 
                    //Resources.Error_DuplicateService, serviceType.FullName), e);
                }

                // Now see if there were any subscriptions that required this service
                SubscribeServiceCallback subscribeCallback;
                if (_subscriptions != null
                    && _subscriptions.TryGetValue(serviceType, out subscribeCallback))
                {
                    subscribeCallback(serviceType, GetService(serviceType));
                    _subscriptions.Remove(serviceType);
                }
            }

            /// <summary>
            ///     Removes a subscription.
            /// </summary>
            public override void Unsubscribe(Type serviceType, SubscribeServiceCallback callback)
            {
                if (serviceType == null)
                {
                    throw new ArgumentNullException("serviceType");
                }
                if (callback == null)
                {
                    throw new ArgumentNullException("callback");
                }

                if (_subscriptions != null)
                {
                    SubscribeServiceCallback existing;
                    if (_subscriptions.TryGetValue(serviceType, out existing))
                    {
                        existing = (SubscribeServiceCallback)RemoveCallback(existing, callback);
                        if (existing == null)
                        {
                            _subscriptions.Remove(serviceType);
                        }
                        else
                        {
                            _subscriptions[serviceType] = existing;
                        }
                    }
                }
            }

            /// <summary>
            ///     We implement IDisposable so that the editing context can destroy us when it
            ///     shuts down.
            /// </summary>
            void IDisposable.Dispose()
            {
                if (_services != null)
                {
                    var services = _services;

                    try
                    {
                        foreach (var value in services.Values)
                        {
                            var d = value as IDisposable;
                            if (d != null)
                            {
                                d.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        _services = null;
                    }
                }
            }
        }
    }
}
