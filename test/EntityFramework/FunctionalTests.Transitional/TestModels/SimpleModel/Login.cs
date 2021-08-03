// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace SimpleModel
{
    using System;

    public class Login
    {
        public Login()
        {
        }

        public Login(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public string Username { get; set; }
    }
}
