﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
Imports System

Namespace Another.Place
    Partial Public Class AuditInfoMm
        Public Property ModifiedDate As Date
        Public Property ModifiedBy As String

        Public Property Concurrency As ConcurrencyInfoMm = New ConcurrencyInfoMm

    End Class
End Namespace
