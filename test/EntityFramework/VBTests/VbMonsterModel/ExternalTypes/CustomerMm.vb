﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
Imports System
Imports System.Collections.Generic

Namespace Another.Place
    Partial Public Class CustomerMm
        Public Property CustomerId As Integer
        Public Property Name As String

        Public Property ContactInfo As ContactDetailsMm = New ContactDetailsMm
        Public Property Auditing As AuditInfoMm = New AuditInfoMm

        Public Overridable Property Orders As ICollection(Of OrderMm) = New HashSet(Of OrderMm)
        Public Overridable Property Logins As ICollection(Of LoginMm) = New HashSet(Of LoginMm)
        Public Overridable Property Husband As CustomerMm
        Public Overridable Property Wife As CustomerMm
        Public Overridable Property Info As CustomerInfoMm

    End Class
End Namespace
