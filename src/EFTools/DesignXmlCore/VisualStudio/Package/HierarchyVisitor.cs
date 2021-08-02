// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using VSErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace Microsoft.Data.Entity.Design.VisualStudio.Package
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    // <summary>
    //     This is just wrapper for project item path
    // </summary>
    internal struct VsProjectItemPath
    {
        internal Url BaseUrl;
        internal string RelativePath;

        internal VsProjectItemPath(Url baseUrl, string relativePath)
        {
            BaseUrl = baseUrl;
            RelativePath = relativePath;
        }

        internal string Path
        {
            get
            {
                if (BaseUrl != null
                    && !string.IsNullOrEmpty(RelativePath))
                {
                    var url = new Url(BaseUrl, RelativePath);
                    return url.AbsoluteUrl;
                }
                return RelativePath;
            }
        }
    }

    internal delegate void HierarchyHandler(IVsHierarchy item, uint id, VsProjectItemPath path);

    // <summary>
    //     HierarchyVisitor walks IVsHierarchy and calls a given delegate for each item
    //     found together with a string containing the full SaveName (or moniker) for each item.
    // </summary>
    internal class HierarchyVisitor
    {
        private readonly HierarchyHandler _handler;

        // <summary>
        //     Construct a new hierarchy visitor providing the delegate you want called during each visit.
        // </summary>
        internal HierarchyVisitor(HierarchyHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            _handler = handler;
        }

        internal void VisitHierarchy(IVsHierarchy root)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            object pvar;
            Url baseUri = null;
            var hr = root.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out pvar);
            var projectDir = pvar as string;
            if (VSErrorHandler.Succeeded(hr)
                && projectDir != null)
            {
                baseUri = new Url(projectDir + Path.DirectorySeparatorChar);
            }
            VisitHierarchyItems(root, VSConstants.VSITEMID_ROOT, baseUri, _handler, true);
        }

        internal static bool IsSearchable(IVsHierarchy hierarchy, uint itemid)
        {
            object pvar;
            var hr = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_IsNonSearchable, out pvar);
            var isNonSearchable = pvar as bool?;
            if (VSErrorHandler.Succeeded(hr)
                && isNonSearchable != null)
            {
                return !isNonSearchable.Value;
            }

            object pvar2;
            hr = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_HasEnumerationSideEffects, out pvar2);
            var hasEnumerationSideEffects = pvar2 as bool?;
            if (VSErrorHandler.Succeeded(hr)
                && hasEnumerationSideEffects != null)
            {
                return !hasEnumerationSideEffects.Value;
            }
            return true;
        }

        private void VisitHierarchyItems(IVsHierarchy hierarchy, uint id, Url baseUrl, HierarchyHandler handler, bool isRootItem)
        {
            // Note: some root items (e.g. for a C# .NET Fwk project as of VS2019) say they are not searchable but they are
            if (!isRootItem && !IsSearchable(hierarchy, id))
            {
                return;
            }

            object pvar;
            var hr = hierarchy.GetProperty(id, (int)__VSHPROPID.VSHPROPID_SaveName, out pvar);
            var path = pvar as string;
            if (VSErrorHandler.Succeeded(hr)
                && path != null)
            {
                // Dev10 Bug 653879: Retrieving project item absolute URL is expensive so retrieve when we actually need it.
                handler(hierarchy, id, new VsProjectItemPath(baseUrl, path));
            }

            hr = hierarchy.GetProperty(id, (int)__VSHPROPID.VSHPROPID_FirstChild, out pvar);
            if (VSErrorHandler.Succeeded(hr))
            {
                var childId = GetItemId(pvar);
                while (childId != VSConstants.VSITEMID_NIL)
                {
                    VisitHierarchyItems(hierarchy, childId, baseUrl, handler, false);
                    hr = hierarchy.GetProperty(childId, (int)__VSHPROPID.VSHPROPID_NextSibling, out pvar);
                    if (VSErrorHandler.Succeeded(hr))
                    {
                        childId = GetItemId(pvar);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static uint GetItemId(object pvar)
        {
            if (pvar == null)
            {
                return VSConstants.VSITEMID_NIL;
            }
            if (pvar is int)
            {
                return (uint)(int)pvar;
            }
            if (pvar is uint)
            {
                return (uint)pvar;
            }
            if (pvar is short)
            {
                return (uint)(short)pvar;
            }
            if (pvar is ushort)
            {
                return (ushort)pvar;
            }
            if (pvar is long)
            {
                return (uint)(long)pvar;
            }
            return VSConstants.VSITEMID_NIL;
        }
    }
}
