// Copyright 2024 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;

using JetBrains.Annotations;

namespace PPWCode.Server.Core.NServiceBus
{
    /// <inheritdoc cref="IMessageContextAccessor" />
    [UsedImplicitly]
    public class MessageContextAccessor : IMessageContextAccessor
    {
        private static readonly AsyncLocal<MessageContextHolder> _requestMessageContextCurrent =
            new AsyncLocal<MessageContextHolder>();

        public RequestMessageContext MessageContext
        {
            get => _requestMessageContextCurrent.Value?.Context;
            set
            {
                MessageContextHolder holder = _requestMessageContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current HttpContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _requestMessageContextCurrent.Value = new MessageContextHolder { Context = value };
                }
            }
        }

        private class MessageContextHolder
        {
            public RequestMessageContext Context { get; set; }
        }
    }
}
