' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.
Namespace AdvancedPatternsVB

    Partial Friend Class SiteInfoMf

        Public Sub New()

        End Sub

        Public Sub New(ByVal zone As Integer, ByVal environment As String)
            Me.Zone = zone
            Me.Environment = environment
        End Sub

    End Class

End Namespace
