﻿////////////////////////////////////////////////////////////////////////////////
//
//   Copyright 2023 Eppie(https://eppie.io)
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Tuvi.Proton.Primitive.Exceptions;

namespace Tuvi.Proton.Client.Exceptions
{
    public class ProtonSessionException : ProtonException
    {
        internal ProtonSessionException(string message) : base(message)
        { }

        internal ProtonSessionException(string message, Exception innerException)
            : base(message, innerException)
        { }

        internal ProtonSessionException()
        { }
    }
}
