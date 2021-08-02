﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation.  All rights reserved.")]
[assembly: AssemblyProduct("Microsoft Entity Framework")]
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-US")]

#if !BUILD_GENERATED_VERSION
#if VS11
[assembly: AssemblyVersion("11.2.0.0")]
[assembly: AssemblyFileVersion("11.2.0.0")]
#elif VS12
[assembly: AssemblyVersion("12.0.0.0")]
[assembly: AssemblyFileVersion("12.0.0.0")]
#elif VS14
[assembly: AssemblyVersion("14.0.0.0")]
[assembly: AssemblyFileVersion("14.0.0.0")]
#elif VS15
[assembly: AssemblyVersion("15.0.0.0")]
[assembly: AssemblyFileVersion("15.0.0.0")]
#elif VS16
[assembly: AssemblyVersion("16.0.0.0")]
[assembly: AssemblyFileVersion("16.0.0.0")]
#endif
#endif
