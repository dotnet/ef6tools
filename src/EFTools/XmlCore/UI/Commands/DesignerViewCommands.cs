// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.Commands
{
    using System.Windows.Input;
    using Microsoft.Data.Tools.XmlDesignerBase;

    internal class DesignerViewCommands
    {
        public static readonly RoutedUICommand ChangeCenter =
            new RoutedUICommand(Resources.DesignerViewCommandsText, "ChangeCenter", typeof(DesignerViewCommands));
    }
}
